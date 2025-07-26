namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImFontGlyphRangesBuilder
{
    public void AddText(ImU8String text)
    {
        fixed (ImFontGlyphRangesBuilder* thisPtr = &this) ImGui.AddText(thisPtr, text);
    }
}

public partial struct ImFontGlyphRangesBuilderPtr
{
    public void AddText(ImU8String text) => ImGui.AddText(this, text);
}
