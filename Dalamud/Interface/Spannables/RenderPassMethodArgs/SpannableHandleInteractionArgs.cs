using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.RenderPassMethodArgs;

/// <summary>Arguments for use with <see cref="ISpannableRenderPass.HandleSpannableInteraction"/>.</summary>
public struct SpannableHandleInteractionArgs
{
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

    /// <summary>Initializes a new instance of the <see cref="SpannableHandleInteractionArgs"/> struct.</summary>
    /// <param name="renderPass">The state for the spannable.</param>
    public SpannableHandleInteractionArgs(ISpannableRenderPass renderPass)
    {
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

    /// <summary>Declare item bounding box for clipping and interaction.
    /// Note that the size can be different than the one provided to ItemSize(). Typically, widgets that spread over
    /// available surface declare their minimum size requirement to ItemSize() and provide a larger region to ItemAdd()
    /// which is used drawing/interaction.</summary>
    /// <param name="innerId">The inner ID.</param>
    /// <param name="rcHover">The hit-test boundary.</param>
    /// <param name="rcNav">The nav-test boundary.</param>
    /// <param name="rcDisplay">The display boundary.</param>
    /// <param name="hovered">Whether the control is hovered.</param>
    /// <param name="noNav">Whether to disable navigation/focus.</param>
    /// <param name="noNavDefaultFocus">Whether to not apply default focus.</param>
    /// <param name="disabled">Whether the item is disabled.</param>
    public readonly unsafe void ItemAdd(
        int innerId,
        RectVector4 rcHover,
        RectVector4 rcNav,
        RectVector4 rcDisplay,
        bool hovered,
        bool noNav,
        bool noNavDefaultFocus,
        bool disabled)
    {
        if (innerId == -1)
            return;
        ref var context = ref ImGuiInternals.ImGuiContext.Instance;
        rcHover = RectVector4.TransformLossy(rcHover, this.RenderPass.TransformationFromAncestors);
        rcNav = RectVector4.TransformLossy(rcNav, this.RenderPass.TransformationFromAncestors);
        rcDisplay = RectVector4.TransformLossy(rcDisplay, this.RenderPass.TransformationFromAncestors);
        ImGuiInternals.ImGuiItemAdd(
            &rcHover,
            this.RenderPass.GetGlobalIdFromInnerId(innerId),
            &rcNav,
            ImGuiInternals.ImGuiItemFlags.Inputable
            | (noNav
                   ? ImGuiInternals.ImGuiItemFlags.NoNav | ImGuiInternals.ImGuiItemFlags.NoTabStop
                   : ImGuiInternals.ImGuiItemFlags.None)
            | (noNavDefaultFocus ? ImGuiInternals.ImGuiItemFlags.NoNavDefaultFocus : ImGuiInternals.ImGuiItemFlags.None)
            | (disabled ? ImGuiInternals.ImGuiItemFlags.Disabled : ImGuiInternals.ImGuiItemFlags.None));
        context.LastItemData.DisplayRect = rcDisplay;
        if (hovered)
            context.LastItemData.StatusFlags &= ImGuiInternals.ImGuiItemStatusFlags.HoveredRect;
        else
            context.LastItemData.StatusFlags &= ~ImGuiInternals.ImGuiItemStatusFlags.HoveredRect;
    }

    /// <summary>Marks the specified inner ID as active (pressed).</summary>
    /// <param name="innerId">The inner ID.</param>
    /// <param name="useWheel">Whether to take wheel inputs, preventing window from handling wheel events.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe void SetActive(int innerId, bool useWheel = false)
    {
        if (innerId != -1)
        {
            ImGuiInternals.ImGuiSetActiveId(
                this.RenderPass.GetGlobalIdFromInnerId(innerId),
                ImGuiInternals.ImGuiContext.Instance.CurrentWindow);
        }

        if (useWheel)
            ImGuiInternals.ImGuiContext.Instance.ActiveIdUsingMouseWheel = 1;
    }

    /// <summary>Marks the specified inner ID as focused.</summary>
    /// <param name="innerId">The inner ID.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe void SetFocused(int innerId)
    {
        if (innerId != -1)
        {
            ImGuiInternals.ImGuiSetFocusedId(
                this.RenderPass.GetGlobalIdFromInnerId(innerId),
                ImGuiInternals.ImGuiContext.Instance.CurrentWindow);
        }
    }

    /// <summary>Marks the specified inner ID as hovered.</summary>
    /// <param name="innerId">The inner ID.</param>
    /// <param name="useWheel">Whether to take wheel inputs, preventing window from handling wheel events.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe void SetHovered(int innerId, bool useWheel = false)
    {
        if (innerId != -1)
            ImGuiInternals.ImGuiSetHoveredId(this.RenderPass.GetGlobalIdFromInnerId(innerId));
        if (useWheel)
            ImGuiInternals.ImGuiContext.Instance.HoveredIdUsingMouseWheel = 1;
    }

    /// <summary>Clears the active item ID.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe void ClearActive() =>
        ImGuiInternals.ImGuiSetActiveId(0, ImGuiInternals.ImGuiContext.Instance.CurrentWindow);

    /// <summary>Determines if some other item is active.</summary>
    /// <param name="rc">The rect in local coordinates.</param>
    /// <param name="innerId">The inner ID.</param>
    /// <returns><c>true</c> if something else is active.</returns>
    public readonly unsafe bool IsItemHoverable(in RectVector4 rc, int innerId)
    {
        ref var g = ref ImGuiInternals.ImGuiContext.Instance;
        var innerIdGlobal = innerId == -1 ? 0 : this.RenderPass.GetGlobalIdFromInnerId(innerId);
        var rcGlobal = RectVector4.TransformLossy(rc, this.RenderPass.TransformationFromAncestors);
        var prevHover = g.HoveredId;
        var prevHoverDisabled = g.HoveredIdDisabled;
        if (ImGuiInternals.ImGuiItemHoverable(&rcGlobal, innerIdGlobal) == 0)
            return false;
        if (!rc.Contains(this.MouseLocalLocation))
        {
            ImGuiInternals.ImGuiSetHoveredId(prevHover);
            g.HoveredIdDisabled = prevHoverDisabled;
            return false;
        }

        return true;
    }
}
