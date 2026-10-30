using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Fonts;

/// <summary>Layout options beyond the current font and ImGui state.</summary>
/// <example><code>
/// using var layout = ImGui.CreateTextLayout(line, highlights, new() { TabSize = 8 });
/// </code></example>
/// <seealso cref="ImGuiTextExtensions.extension(ImGui).CreateTextLayout(ReadOnlySpan{byte}, ReadOnlySpan{TextStyleRange}, TextLayoutOptions)"/>
public readonly record struct TextLayoutOptions
{
    /// <summary>Gets the width to wrap at, or 0 for no wrapping.</summary>
    public float WrapWidth { get; init; }

    /// <summary>Gets the spaces between tab stops, counted from the start of each line, or 0 for 4.</summary>
    public int TabSize { get; init; }

    /// <summary>Gets the layout flags.</summary>
    public ImTextLayoutFlags Flags { get; init; }

    /// <summary>Gets the maximum number of lines, or 0 for unlimited.</summary>
    /// <remarks>Truncated text ends with an ellipsis on the last line.</remarks>
    public int MaxLines { get; init; }

    /// <summary>Gets horizontal line alignment within <see cref="WrapWidth"/>, or within the widest line when unwrapped.</summary>
    /// <value>0 is left, 0.5 is centered, and 1 is right.</value>
    public float Align { get; init; }
}
