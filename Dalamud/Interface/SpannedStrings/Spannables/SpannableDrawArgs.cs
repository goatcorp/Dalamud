using System.Runtime.CompilerServices;

using Dalamud.Interface.SpannedStrings.Rendering;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings.Spannables;

/// <summary>Arguments for use with <see cref="ISpannable.Draw"/>.</summary>
public readonly struct SpannableDrawArgs
{
    /// <summary>The state obtained from <see cref="ISpannable.RentState"/>.</summary>
    public readonly ISpannableState State;

    private readonly unsafe ImDrawListSplitter* splitterPtr;

    /// <summary>Initializes a new instance of the <see cref="SpannableDrawArgs"/> struct.</summary>
    /// <param name="state">The state for the spannable.</param>
    /// <param name="splitterPtr">The splitter to use.</param>
    public unsafe SpannableDrawArgs(ISpannableState state, ImDrawListSplitterPtr splitterPtr)
    {
        this.State = state;
        this.splitterPtr = splitterPtr;
    }

    /// <summary>Creates a new instance of the <see cref="SpannableDrawArgs"/> struct.</summary>
    /// <param name="state">The state to use instead.</param>
    /// <returns>The new instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SpannableDrawArgs WithState(ISpannableState state) => new(state, this.splitterPtr);

    /// <summary>Switches to a specified channel (layer).</summary>
    /// <param name="channel">The channel.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SwitchToChannel(RenderChannel channel)
    {
        if (this.splitterPtr is null || this.State.RenderState.DrawListPtr.NativePtr is null)
            return;
        ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
            this.splitterPtr,
            this.State.RenderState.DrawListPtr,
            (int)channel);
    }
}
