using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Plugin.Services;

namespace Dalamud.Interface.Spannables;

/// <summary>Arguments for use with <see cref="ISpannable.RentState"/>.</summary>
public struct SpannableRentStateArgs
{
    /// <summary>The associated renderer.</summary>
    public ISpannableRenderer Renderer;
    
    /// <summary>The allocated ImGui global ID. <c>0</c> to disable interaction.</summary>
    public uint ImGuiGlobalId;
    
    /// <summary>The render scale to use.</summary>
    public float Scale;
    
    /// <summary>The initial text state.</summary>
    public TextState TextState;

    /// <summary>Initializes a new instance of the <see cref="SpannableRentStateArgs"/> struct.</summary>
    /// <param name="renderer">The associated renderer.</param>
    /// <param name="imGuiGlobalId">The allocated ImGui global ID. <c>0</c> to disable interaction.</param>
    /// <param name="scale">The render scale to use.</param>
    /// <param name="textState">The initial text state.</param>
    public SpannableRentStateArgs(ISpannableRenderer renderer, uint imGuiGlobalId, float scale, TextState textState)
    {
        this.Renderer = renderer;
        this.ImGuiGlobalId = imGuiGlobalId;
        this.Scale = scale;
        this.TextState = textState;
    }
}
