using System.Diagnostics;
using System.Linq;
using System.Numerics;

using Dalamud.Hooking;

using ImGuiNET;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Fixes ImDrawList not correctly dealing with the current texture for that draw list not in tune with the global
/// state. Currently, ImDrawList::AddPolyLine and ImDrawList::AddRectFilled are affected.
///
/// * The implementation for AddRectFilled is entirely replaced with the hook below.
/// * The implementation for AddPolyLine is wrapped with Push/PopTextureID.
/// 
/// TODO:
/// * imgui_draw.cpp:1433 ImDrawList::AddRectFilled
///   The if block needs a PushTextureID(_Data->TexIdCommon)/PopTextureID() block,
///   if _Data->TexIdCommon != _CmdHeader.TextureId.
/// * imgui_draw.cpp:729 ImDrawList::AddPolyLine
///   The if block always needs to call PushTextureID if the abovementioned condition is not met.
///   Change push_texture_id to only have one condition.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class ImGuiDrawListFixProvider : IInternalDisposableService
{
    private const int CImGuiImDrawListAddPolyLineOffset = 0x589B0;
    private const int CImGuiImDrawListAddRectFilled = 0x59FD0;
    private const int CImGuiImDrawListAddImageRounded = 0x58390;
    private const int CImGuiImDrawListSharedDataTexIdCommonOffset = 0;

    private readonly Hook<ImDrawListAddPolyLine> hookImDrawListAddPolyline;
    private readonly Hook<ImDrawListAddRectFilled> hookImDrawListAddRectFilled;
    private readonly Hook<ImDrawListAddImageRounded> hookImDrawListAddImageRounded;

    [ServiceManager.ServiceConstructor]
    private ImGuiDrawListFixProvider(InterfaceManager.InterfaceManagerWithScene imws)
    {
        // Force cimgui.dll to be loaded.
        _ = ImGui.GetCurrentContext();
        var cimgui = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                            .First(x => x.ModuleName == "cimgui.dll")
                            .BaseAddress;

        this.hookImDrawListAddPolyline = Hook<ImDrawListAddPolyLine>.FromAddress(
            cimgui + CImGuiImDrawListAddPolyLineOffset,
            this.ImDrawListAddPolylineDetour);
        this.hookImDrawListAddRectFilled = Hook<ImDrawListAddRectFilled>.FromAddress(
            cimgui + CImGuiImDrawListAddRectFilled,
            this.ImDrawListAddRectFilledDetour);
        this.hookImDrawListAddImageRounded = Hook<ImDrawListAddImageRounded>.FromAddress(
            cimgui + CImGuiImDrawListAddImageRounded,
            this.ImDrawListAddImageRoundedDetour);
        this.hookImDrawListAddPolyline.Enable();
        this.hookImDrawListAddRectFilled.Enable();
        this.hookImDrawListAddImageRounded.Enable();
    }

    private delegate void ImDrawListAddPolyLine(
        ImDrawListPtr drawListPtr,
        ref Vector2 points,
        int pointsCount,
        uint color,
        ImDrawFlags flags,
        float thickness);

    private delegate void ImDrawListAddRectFilled(
        ImDrawListPtr drawListPtr,
        ref Vector2 min,
        ref Vector2 max,
        uint col,
        float rounding,
        ImDrawFlags flags);

    private delegate void ImDrawListAddImageRounded(
        ImDrawListPtr drawListPtr,
        nint userTextureId,        ref Vector2 xy0,
        ref Vector2 xy1,
        ref Vector2 uv0,
        ref Vector2 uv1,
        uint col,
        float rounding,
        ImDrawFlags flags);

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.hookImDrawListAddPolyline.Dispose();
        this.hookImDrawListAddRectFilled.Dispose();
        this.hookImDrawListAddImageRounded.Dispose();
    }

    private static ImDrawFlags FixRectCornerFlags(ImDrawFlags flags)
    {
#if !IMGUI_DISABLE_OBSOLETE_FUNCTIONS
        // Legacy Support for hard coded ~0 (used to be a suggested equivalent to ImDrawCornerFlags_All)
        //   ~0   --> ImDrawFlags_RoundCornersAll or 0
        if ((int)flags == ~0)
            return ImDrawFlags.RoundCornersAll;

        // Legacy Support for hard coded 0x01 to 0x0F (matching 15 out of 16 old flags combinations)
        //   0x01 --> ImDrawFlags_RoundCornersTopLeft (VALUE 0x01 OVERLAPS ImDrawFlags_Closed but ImDrawFlags_Closed is never valid in this path!)
        //   0x02 --> ImDrawFlags_RoundCornersTopRight
        //   0x03 --> ImDrawFlags_RoundCornersTopLeft | ImDrawFlags_RoundCornersTopRight
        //   0x04 --> ImDrawFlags_RoundCornersBotLeft
        //   0x05 --> ImDrawFlags_RoundCornersTopLeft | ImDrawFlags_RoundCornersBotLeft
        //   ...
        //   0x0F --> ImDrawFlags_RoundCornersAll or 0
        // (See all values in ImDrawCornerFlags_)
        if ((int)flags >= 0x01 && (int)flags <= 0x0F)
            return (ImDrawFlags)((int)flags << 4);

        // We cannot support hard coded 0x00 with 'float rounding > 0.0f' --> replace with ImDrawFlags_RoundCornersNone or use 'float rounding = 0.0f'
#endif

        // If this triggers, please update your code replacing hardcoded values with new ImDrawFlags_RoundCorners* values.
        // Note that ImDrawFlags_Closed (== 0x01) is an invalid flag for AddRect(), AddRectFilled(), PathRect() etc...
        if (((int)flags & 0x0F) != 0)
            throw new ArgumentException("Misuse of legacy hardcoded ImDrawCornerFlags values!");

        if ((flags & ImDrawFlags.RoundCornersMask) == 0)
            flags |= ImDrawFlags.RoundCornersAll;

        return flags;
    }

    private void ImDrawListAddRectFilledDetour(
        ImDrawListPtr drawListPtr,
        ref Vector2 min,
        ref Vector2 max,
        uint col,
        float rounding,
        ImDrawFlags flags)
    {
        // Skip drawing if we're drawing something with alpha value of 0.
        if ((col & 0xFF000000) == 0)
            return;

        if (rounding < 0.5f || (flags & ImDrawFlags.RoundCornersMask) == ImDrawFlags.RoundCornersMask)
        {
            // Take the fast path of drawing two triangles if no rounded corners are required.

            var texIdCommon = *(nint*)(drawListPtr._Data + CImGuiImDrawListSharedDataTexIdCommonOffset);
            var pushTextureId = texIdCommon != drawListPtr._CmdHeader.TextureId;
            if (pushTextureId)
                drawListPtr.PushTextureID(texIdCommon);

            drawListPtr.PrimReserve(6, 4);
            drawListPtr.PrimRect(min, max, col);

            if (pushTextureId)
                drawListPtr.PopTextureID();
        }
        else
        {
            // Defer drawing rectangle with rounded corners to path drawing operations.
            // Note that this may have a slightly different extent behaviors from the above if case.
            // This is how it is in imgui_draw.cpp.
            drawListPtr.PathRect(min, max, rounding, flags);
            drawListPtr.PathFillConvex(col);
        }
    }

    private void ImDrawListAddPolylineDetour(
        ImDrawListPtr drawListPtr,
        ref Vector2 points,
        int pointsCount,
        uint color,
        ImDrawFlags flags,
        float thickness)
    {
        var texIdCommon = *(nint*)(drawListPtr._Data + CImGuiImDrawListSharedDataTexIdCommonOffset);
        var pushTextureId = texIdCommon != drawListPtr._CmdHeader.TextureId;
        if (pushTextureId)
            drawListPtr.PushTextureID(texIdCommon);
        
        this.hookImDrawListAddPolyline.Original(drawListPtr, ref points, pointsCount, color, flags, thickness);

        if (pushTextureId)
            drawListPtr.PopTextureID();
    }

    private void ImDrawListAddImageRoundedDetour(ImDrawListPtr drawListPtr, nint userTextureId, ref Vector2 xy0, ref Vector2 xy1, ref Vector2 uv0, ref Vector2 uv1, uint col, float rounding, ImDrawFlags flags)
    {
        // Skip drawing if we're drawing something with alpha value of 0.
        if ((col & 0xFF000000) == 0)
            return;

        // Handle non-rounded cases.
        flags = FixRectCornerFlags(flags);
        if (rounding < 0.5f || (flags & ImDrawFlags.RoundCornersMask) == ImDrawFlags.RoundCornersNone)
        {
            drawListPtr.AddImage(userTextureId, xy0, xy1, uv0, uv1, col);
            return;
        }

        // Temporary provide the requested image as the common texture ID, so that the underlying
        // ImDrawList::AddConvexPolyFilled does not create a separate draw command and then revert back.
        // ImDrawList::AddImageRounded will temporarily push the texture ID provided by the user if the latest draw
        // command does not point to the texture we're trying to draw. Once pushed, ImDrawList::AddConvexPolyFilled
        // will leave the list of draw commands alone, so that ImGui::ShadeVertsLinearUV can safely work on the latest
        // draw command.
        ref var texIdCommon = ref *(nint*)(drawListPtr._Data + CImGuiImDrawListSharedDataTexIdCommonOffset);
        var texIdCommonPrev = texIdCommon;
        texIdCommon = userTextureId;

        this.hookImDrawListAddImageRounded.Original(
            drawListPtr,
            texIdCommon,
            ref xy0,
            ref xy1,
            ref uv0,
            ref uv1,
            col,
            rounding,
            flags);

        texIdCommon = texIdCommonPrev;
    }
}
