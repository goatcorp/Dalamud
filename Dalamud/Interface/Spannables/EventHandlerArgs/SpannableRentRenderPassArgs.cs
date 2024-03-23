using Dalamud.Plugin.Services;

namespace Dalamud.Interface.Spannables.EventHandlerArgs;

/// <summary>Arguments for use with <see cref="ISpannable.RentRenderPass"/>.</summary>
public ref struct SpannableRentRenderPassArgs
{
    /// <summary>The associated renderer.</summary>
    public ISpannableRenderer Renderer;

    /// <summary>Initializes a new instance of the <see cref="SpannableRentRenderPassArgs"/> struct.</summary>
    /// <param name="renderer">The associated renderer.</param>
    public SpannableRentRenderPassArgs(ISpannableRenderer renderer) => this.Renderer = renderer;
}
