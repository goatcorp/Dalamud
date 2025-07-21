namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImFontGlyphRangesBuilder
{
    public void AddText(AutoUtf8Buffer text)
    {
        fixed (ImFontGlyphRangesBuilder* thisPtr = &this)
            ImGui.AddText(thisPtr, text);
    }
}

public partial struct ImFontGlyphRangesBuilderPtr
{
    public void AddText(AutoUtf8Buffer text) => ImGui.AddText(this, text);
}
