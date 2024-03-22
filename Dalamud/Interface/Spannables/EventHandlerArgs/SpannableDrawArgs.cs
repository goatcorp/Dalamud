using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Rendering;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.EventHandlerArgs;

/// <summary>Arguments for use with <see cref="ISpannable.Draw"/>.</summary>
public struct SpannableDrawArgs
{
    /// <summary>The state obtained from <see cref="ISpannable.RentState"/>.</summary>
    public ISpannableState State;

    /// <summary>The splitter.</summary>
    public ImDrawListSplitterPtr SplitterPtr;

    /// <summary>The draw list.</summary>
    public ImDrawListPtr DrawListPtr;

    /// <summary>Initializes a new instance of the <see cref="SpannableDrawArgs"/> struct.</summary>
    /// <param name="state">The state for the spannable.</param>
    /// <param name="splitterPtr">The splitter to use.</param>
    /// <param name="drawListPtr">The darw list to use.</param>
    public SpannableDrawArgs(ISpannableState state, ImDrawListSplitterPtr splitterPtr, ImDrawListPtr drawListPtr)
    {
        this.State = state;
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
}
