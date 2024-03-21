using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Rendering;

using ImGuiNET;

namespace Dalamud.Interface.Spannables;

/// <summary>Arguments for use with <see cref="ISpannable.InteractWith"/>.</summary>
public readonly struct SpannableInteractionArgs
{
    /// <summary>The state obtained from <see cref="ISpannable.RentState"/>.</summary>
    public readonly ISpannableState State;

    private const int CImGuiSetActiveIdOffset = 0x483f0;
    private const int CImGuiSetHoverIdOffset = 0x48e80;
    private const int CImGuiContextCurrentWindowOffset = 0x3ff0;
    private const int CImGuiContextHoveredWindowOffset = 0x3ff8;
    private const int CImGuiContextHoveredIdOffset = 0x4030;
    private const int CImGuiContextHoveredIdUsingMouseWheelOffset = 0x4039;
    private const int CImGuiContextActiveIdOffset = 0x4044;
    private const int CImGuiContextActiveIdUsingMouseWheelOffset = 0x4088;

    private static readonly unsafe delegate* unmanaged<uint, nint, void> ImGuiSetActiveId;
    private static readonly unsafe delegate* unmanaged<uint, void> ImGuiSetHoveredId;

    static unsafe SpannableInteractionArgs()
    {
        _ = ImGui.GetCurrentContext();

        var cimgui = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                            .First(x => x.ModuleName == "cimgui.dll")
                            .BaseAddress;
        ImGuiSetActiveId = (delegate* unmanaged<uint, IntPtr, void>)(cimgui + CImGuiSetActiveIdOffset);
        ImGuiSetHoveredId = (delegate* unmanaged<uint, void>)(cimgui + CImGuiSetHoverIdOffset);
    }

    /// <summary>Initializes a new instance of the <see cref="SpannableInteractionArgs"/> struct.</summary>
    /// <param name="state">The state for the spannable.</param>
    public SpannableInteractionArgs(ISpannableState state) => this.State = state;

    /// <summary>Gets the mouse coordinates, w.r.t <see cref="RenderState.StartScreenOffset"/>, un-transformed according
    /// to <see cref="RenderState.Transformation"/>.</summary>
    /// <returns>The relative mouse coordinates.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetRelativeMouseCoord() =>
        this.State.RenderState.TransformInverse(ImGui.GetMousePos() - this.State.RenderState.StartScreenOffset);

    /// <summary>Marks the specified inner ID as hovered.</summary>
    /// <param name="innerId">The inner ID.</param>
    /// <param name="useWheel">Whether to take wheel inputs, preventing window from handling wheel events.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetHovered(int innerId, bool useWheel = false)
    {
        ImGuiSetHoveredId(this.State.RenderState.GetGlobalIdFromInnerId(innerId));
        if (useWheel)
        {
            *(byte*)(ImGui.GetCurrentContext() + CImGuiContextHoveredIdUsingMouseWheelOffset) = 1;
        }
    }

    /// <summary>Marks the specified inner ID as active (pressed).</summary>
    /// <param name="innerId">The inner ID.</param>
    /// <param name="useWheel">Whether to take wheel inputs, preventing window from handling wheel events.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetActive(int innerId, bool useWheel = false)
    {
        ImGuiSetActiveId(
            this.State.RenderState.GetGlobalIdFromInnerId(innerId),
            *(nint*)(ImGui.GetCurrentContext() + CImGuiContextCurrentWindowOffset));
        if (useWheel)
        {
            *(byte*)(ImGui.GetCurrentContext() + CImGuiContextActiveIdUsingMouseWheelOffset) = 1;
        }
    }

    /// <summary>Clears the active item ID.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void ClearActive() => ImGuiSetActiveId(0, 0);

    /// <summary>Determines if some other item is active.</summary>
    /// <param name="innerId">The inner ID.</param>
    /// <returns><c>true</c> if something else is active.</returns>
    public unsafe bool IsItemHoverable(int innerId)
    {
        var currentWindow = *(nint*)(ImGui.GetCurrentContext() + CImGuiContextCurrentWindowOffset);
        var hoveredWindow = *(nint*)(ImGui.GetCurrentContext() + CImGuiContextHoveredWindowOffset);
        if (currentWindow != hoveredWindow)
            return false;

        var innerIdGlobal = this.State.RenderState.GetGlobalIdFromInnerId(innerId);
        var hoveredId = *(uint*)(ImGui.GetCurrentContext() + CImGuiContextHoveredIdOffset);
        if (hoveredId != 0 && hoveredId != innerIdGlobal)
            return false;

        var activeId = *(uint*)(ImGui.GetCurrentContext() + CImGuiContextActiveIdOffset);
        if (activeId != 0 && activeId != innerIdGlobal)
            return false;

        return true;
    }
}
