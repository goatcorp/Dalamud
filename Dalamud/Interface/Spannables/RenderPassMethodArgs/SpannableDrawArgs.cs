using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Rendering;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.RenderPassMethodArgs;

/// <summary>Arguments for use with <see cref="ISpannableRenderPass.DrawSpannable"/>.</summary>
public struct SpannableDrawArgs
{
    /// <summary>The associated spannable.</summary>
    public ISpannable Sender;

    /// <summary>The state obtained from <see cref="ISpannable.RentRenderPass"/>.</summary>
    public ISpannableRenderPass RenderPass;

    /// <summary>The splitter.</summary>
    public ImDrawListSplitterPtr SplitterPtr;

    /// <summary>The draw list.</summary>
    public ImDrawListPtr DrawListPtr;

    /// <summary>Initializes a new instance of the <see cref="SpannableDrawArgs"/> struct.</summary>
    /// <param name="sender">The associated spannable.</param>
    /// <param name="renderPass">The state for the spannable.</param>
    /// <param name="splitterPtr">The splitter to use.</param>
    /// <param name="drawListPtr">The darw list to use.</param>
    public SpannableDrawArgs(
        ISpannable sender,
        ISpannableRenderPass renderPass,
        ImDrawListSplitterPtr splitterPtr,
        ImDrawListPtr drawListPtr)
    {
        this.Sender = sender;
        this.RenderPass = renderPass;
        this.SplitterPtr = splitterPtr;
        this.DrawListPtr = drawListPtr;
    }

    /// <summary>Gets a value indicating whether there is no target available for drawing.</summary>
    public readonly unsafe bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.SplitterPtr.NativePtr is null || this.DrawListPtr.NativePtr is null;
    }

    /// <summary>Switches to a specified channel (layer).</summary>
    /// <param name="channel">The channel.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe void SwitchToChannel(RenderChannel channel)
    {
        if (this.IsEmpty)
            return;
        ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
            this.SplitterPtr,
            this.DrawListPtr,
            (int)channel);
    }

    /// <summary>Notifies a child <see cref="ISpannable"/> with transformed arguments.</summary>
    /// <param name="child">A child to notify the event.</param>
    /// <param name="childRenderPass">The child state.</param>
    public readonly void NotifyChild(ISpannable child, ISpannableRenderPass childRenderPass) =>
        childRenderPass.DrawSpannable(this with { Sender = child, RenderPass = childRenderPass });
}
