namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImTextureData
{
    /// <summary>Gets a texture reference that points to this texture data.</summary>
    /// <returns>The texture reference.</returns>
    public ImTextureRef GetTexRef()
    {
        fixed (ImTextureData* thisPtr = &this)
            return ImGuiNative.GetTexRef(thisPtr);
    }
}

public unsafe partial struct ImTextureDataPtr
{
    /// <summary>Gets a texture reference that points to this texture data.</summary>
    /// <returns>The texture reference.</returns>
    public ImTextureRef GetTexRef() => ImGuiNative.GetTexRef(this.Handle);
}
