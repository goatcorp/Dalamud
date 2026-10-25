using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Fonts;

/// <summary>A reference to a text layout.</summary>
/// <remarks>If keeping this across frames is desired, use <see cref="ImTextLayoutPtr.AddRef"/> and
/// <see cref="ImTextLayoutPtr.Release"/> on <see cref="Ptr"/> on the main thread.</remarks>
/// <example><code>
/// using var layout = ImGui.CreateTextLayout(line, highlights, tabSize: 4);
/// layout.Ptr.Render(ImGui.GetWindowDrawList(), pos, ImGui.GetColorU32(ImGuiCol.Text));
/// var caret = layout.Ptr.HitTest(ImGui.GetMousePos() - pos);
/// </code></example>
public ref struct ImTextLayoutRef
{
    /// <summary>Initializes a new instance of the <see cref="ImTextLayoutRef"/> struct.</summary>
    /// <param name="ptr">The layout, whose reference this takes over.</param>
    internal ImTextLayoutRef(ImTextLayoutPtr ptr) => this.Ptr = ptr;

    /// <summary>Gets the layout.</summary>
    public ImTextLayoutPtr Ptr { get; private set; }

    /// <summary>Gets a value indicating whether this is a null layout.</summary>
    public readonly bool IsNull => this.Ptr.IsNull;

    public static implicit operator ImTextLayoutPtr(ImTextLayoutRef layout) => layout.Ptr;

    /// <summary>Releases the reference, and makes this a null layout.</summary>
    public void Dispose()
    {
        _ = this.Ptr.Release();
        this.Ptr = default;
    }
}
