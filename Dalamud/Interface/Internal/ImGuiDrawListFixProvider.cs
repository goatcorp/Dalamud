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
    private const int CImGuiImDrawListSharedDataTexIdCommonOffset = 0;

    private readonly Hook<ImDrawListAddPolyLine> hookImDrawListAddPolyline;
    private readonly Hook<ImDrawListAddRectFilled> hookImDrawListAddRectFilled;

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
        this.hookImDrawListAddPolyline.Enable();
        this.hookImDrawListAddRectFilled.Enable();
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

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.hookImDrawListAddPolyline.Dispose();
        this.hookImDrawListAddRectFilled.Dispose();
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
}
