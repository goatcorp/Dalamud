using System.Runtime.CompilerServices;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.RenderPassMethodArgs;

/// <summary>Arguments for use with <see cref="ISpannableRenderPass.DrawSpannable"/>.</summary>
public struct SpannableDrawArgs
{
    /// <summary>The state obtained from <see cref="ISpannable.RentRenderPass"/>.</summary>
    public ISpannableRenderPass RenderPass;

    /// <summary>The draw list.</summary>
    public ImDrawListPtr DrawListPtr;

    /// <summary>Initializes a new instance of the <see cref="SpannableDrawArgs"/> struct.</summary>
    /// <param name="renderPass">The state for the spannable.</param>
    /// <param name="drawListPtr">The darw list to use.</param>
    public SpannableDrawArgs(ISpannableRenderPass renderPass, ImDrawListPtr drawListPtr)
    {
        this.RenderPass = renderPass;
        this.DrawListPtr = drawListPtr;
    }

    /// <summary>Gets a value indicating whether there is no target available for drawing.</summary>
    public readonly unsafe bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.DrawListPtr.NativePtr is null;
    }
}
