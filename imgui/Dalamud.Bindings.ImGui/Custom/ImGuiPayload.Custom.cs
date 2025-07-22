namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImGuiPayload
{
    public readonly Span<byte> DataSpan => new(this.Data, this.DataSize);

    public readonly bool IsDataType(Utf8Buffer type)
    {
        fixed (ImGuiPayload* ptr = &this)
            return ImGui.IsDataType(ptr, type);
    }
}

public partial struct ImGuiPayloadPtr
{
    public readonly bool IsDataType(Utf8Buffer type) => ImGui.IsDataType(this, type);
}
