namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImGuiInputTextCallbackData
{
    public readonly Span<byte> BufSpan => new(this.Buf, this.BufSize);

    public readonly Span<byte> BufTextSpan => new(this.Buf, this.BufTextLen);

    public void InsertChars(int pos, ImU8String text)
    {
        fixed (ImGuiInputTextCallbackData* thisPtr = &this)
            ImGui.InsertChars(thisPtr, pos, text);
    }
}

public unsafe partial struct ImGuiInputTextCallbackDataPtr
{
    public readonly Span<byte> BufSpan => this.Handle->BufSpan;

    public readonly Span<byte> BufTextSpan => this.Handle->BufTextSpan;

    public void InsertChars(int pos, ImU8String text) => ImGui.InsertChars(this, pos, text);
}
