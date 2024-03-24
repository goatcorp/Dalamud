using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Helpers;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.RenderPassMethodArgs;

/// <summary>Arguments for use with <see cref="ISpannableRenderPass.HandleSpannableInteraction"/>.</summary>
public struct SpannableHandleInteractionArgs
{
    /// <summary>The associated spannable.</summary>
    public ISpannable Sender;

    /// <summary>The state obtained from <see cref="ISpannable.RentRenderPass"/>.</summary>
    public ISpannableRenderPass RenderPass;

    /// <summary>Each bit indicates whether a mouse button is held down.</summary>
    /// <remarks>Use the helper method <see cref="IsMouseButtonDown"/>.</remarks>
    public int MouseButtonStateFlags;

    /// <summary>Location of the mouse in screen coordinates.</summary>
    public Vector2 MouseScreenLocation;

    /// <summary>Location of the mouse in local coordinates.</summary>
    public Vector2 MouseLocalLocation;

    /// <summary>Number of detents the mouse wheel has rotated, <b>without</b> WHEEL_DELTA multiplied.</summary>
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
    /// <param name="sender">The associated spannable.</param>
    /// <param name="renderPass">The state for the spannable.</param>
    public SpannableHandleInteractionArgs(ISpannable sender, ISpannableRenderPass renderPass)
    {
        this.Sender = sender;
        this.RenderPass = renderPass;
    }

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
        if (innerId != -1)
            ImGuiSetHoveredId(this.RenderPass.GetGlobalIdFromInnerId(innerId));
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
        if (innerId != -1)
        {
            ImGuiSetActiveId(
                this.RenderPass.GetGlobalIdFromInnerId(innerId),
                *(nint*)(ImGui.GetCurrentContext() + CImGuiContextCurrentWindowOffset));
        }

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

        var innerIdGlobal = this.RenderPass.GetGlobalIdFromInnerId(innerId);
        var hoveredId = *(uint*)(ImGui.GetCurrentContext() + CImGuiContextHoveredIdOffset);
        if (hoveredId != 0 && hoveredId != innerIdGlobal)
            return false;

        var activeId = *(uint*)(ImGui.GetCurrentContext() + CImGuiContextActiveIdOffset);
        if (activeId != 0 && activeId != innerIdGlobal)
            return false;

        return true;
    }

    /// <summary>Notifies a child <see cref="ISpannable"/> with transformed arguments.</summary>
    /// <param name="child">A child to notify the event.</param>
    /// <param name="childRenderPass">The child state.</param>
    /// <param name="link">The interacted link, if the child processed the event.</param>
    public readonly void NotifyChild(
        ISpannable child,
        ISpannableRenderPass childRenderPass,
        out SpannableLinkInteracted link) =>
        childRenderPass.HandleSpannableInteraction(
            this with
            {
                Sender = child,
                RenderPass = childRenderPass,
                MouseLocalLocation = Vector2.Transform(
                    this.MouseLocalLocation,
                    Matrix4x4.Invert(childRenderPass.Transformation, out var inverted)
                        ? inverted
                        : Matrix4x4.Identity),
            },
            out link);
}
