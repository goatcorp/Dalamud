using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.EventHandlerArgs;

/// <summary>Arguments for use with <see cref="ISpannable.HandleInteraction"/>.</summary>
public struct SpannableHandleInteractionArgs
{
    /// <summary>The state obtained from <see cref="ISpannable.RentState"/>.</summary>
    public ISpannableState State;

    /// <summary>Each bit indicates whether a mouse button is held down.</summary>
    /// <remarks>Use the helper method <see cref="IsMouseButtonDown"/>.</remarks>
    public int MouseButtonStateFlags;

    /// <summary>The location of the mouse in screen coordinates.</summary>
    public Vector2 MouseScreenLocation;

    /// <summary>The number of detents the mouse wheel has rotated, <b>without</b> WHEEL_DELTA multiplied.</summary>
    public Vector2 WheelDelta;

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

    static unsafe SpannableHandleInteractionArgs()
    {
        _ = ImGui.GetCurrentContext();

        var cimgui = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                            .First(x => x.ModuleName == "cimgui.dll")
                            .BaseAddress;
        ImGuiSetActiveId = (delegate* unmanaged<uint, IntPtr, void>)(cimgui + CImGuiSetActiveIdOffset);
        ImGuiSetHoveredId = (delegate* unmanaged<uint, void>)(cimgui + CImGuiSetHoverIdOffset);
    }

    /// <summary>Initializes a new instance of the <see cref="SpannableHandleInteractionArgs"/> struct.</summary>
    /// <param name="state">The state for the spannable.</param>
    public SpannableHandleInteractionArgs(ISpannableState state)
    {
        this.State = state;
    }
    
    /// <summary>Gets the location of the mouse, relative to the left top of the control, without having
    /// <see cref="ISpannableState.Transformation"/> or <see cref="ISpannableState.ScreenOffset"/> applied.</summary>
    public readonly Vector2 MouseLocalLocation => this.State.TransformToLocal(this.MouseScreenLocation);

    /// <summary>Gets a value indicating whether the specified mouse button is down, at the point of the beginning of
    /// handling this spannable.</summary>
    /// <param name="button">The button to test.</param>
    /// <returns><c>true</c> if it is down.</returns>
    public readonly bool IsMouseButtonDown(ImGuiMouseButton button) =>
        (this.MouseButtonStateFlags & (1 << (int)button)) != 0;

    /// <summary>Gets any held mouse button.</summary>
    /// <param name="button">The held button, if any.</param>
    /// <returns><c>true</c> if any button is held.</returns>
    public readonly bool TryGetAnyHeldMouseButton(out ImGuiMouseButton button)
    {
        if (this.IsMouseButtonDown(ImGuiMouseButton.Left))
        {
            button = ImGuiMouseButton.Left;
            return true;
        }
        
        if (this.IsMouseButtonDown(ImGuiMouseButton.Right))
        {
            button = ImGuiMouseButton.Right;
            return true;
        }

        if (this.IsMouseButtonDown(ImGuiMouseButton.Middle))
        {
            button = ImGuiMouseButton.Middle;
            return true;
        }

        button = (ImGuiMouseButton)(-1);
        return false;
    }

    /// <summary>Marks the specified inner ID as hovered.</summary>
    /// <param name="innerId">The inner ID.</param>
    /// <param name="useWheel">Whether to take wheel inputs, preventing window from handling wheel events.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe void SetHovered(int innerId, bool useWheel = false)
    {
        ImGuiSetHoveredId(this.State.GetGlobalIdFromInnerId(innerId));
        if (useWheel)
        {
            *(byte*)(ImGui.GetCurrentContext() + CImGuiContextHoveredIdUsingMouseWheelOffset) = 1;
        }
    }

    /// <summary>Marks the specified inner ID as active (pressed).</summary>
    /// <param name="innerId">The inner ID.</param>
    /// <param name="useWheel">Whether to take wheel inputs, preventing window from handling wheel events.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe void SetActive(int innerId, bool useWheel = false)
    {
        ImGuiSetActiveId(
            this.State.GetGlobalIdFromInnerId(innerId),
            *(nint*)(ImGui.GetCurrentContext() + CImGuiContextCurrentWindowOffset));
        if (useWheel)
        {
            *(byte*)(ImGui.GetCurrentContext() + CImGuiContextActiveIdUsingMouseWheelOffset) = 1;
        }
    }

    /// <summary>Clears the active item ID.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe void ClearActive() => ImGuiSetActiveId(0, 0);

    /// <summary>Determines if some other item is active.</summary>
    /// <param name="innerId">The inner ID.</param>
    /// <returns><c>true</c> if something else is active.</returns>
    public readonly unsafe bool IsItemHoverable(int innerId)
    {
        var currentWindow = *(nint*)(ImGui.GetCurrentContext() + CImGuiContextCurrentWindowOffset);
        var hoveredWindow = *(nint*)(ImGui.GetCurrentContext() + CImGuiContextHoveredWindowOffset);
        if (currentWindow != hoveredWindow)
            return false;

        var innerIdGlobal = this.State.GetGlobalIdFromInnerId(innerId);
        var hoveredId = *(uint*)(ImGui.GetCurrentContext() + CImGuiContextHoveredIdOffset);
        if (hoveredId != 0 && hoveredId != innerIdGlobal)
            return false;

        var activeId = *(uint*)(ImGui.GetCurrentContext() + CImGuiContextActiveIdOffset);
        if (activeId != 0 && activeId != innerIdGlobal)
            return false;

        return true;
    }
}
