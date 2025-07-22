namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImFontGlyphRangesBuilder
{
    public void AddText(Utf8Buffer text)
    {
        fixed (ImFontGlyphRangesBuilder* thisPtr = &this) ImGui.AddText(thisPtr, text);
    }
}

public partial struct ImFontGlyphRangesBuilderPtr
{
    public void AddText(Utf8Buffer text) => ImGui.AddText(this, text);
}
