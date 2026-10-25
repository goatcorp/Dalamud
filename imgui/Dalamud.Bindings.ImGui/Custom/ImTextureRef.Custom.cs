namespace Dalamud.Bindings.ImGui;

public partial struct ImTextureRef
{
    /// <summary>Initializes a new instance of the <see cref="ImTextureRef"/> struct from a texture ID.</summary>
    /// <param name="texId">The texture ID.</param>
    public ImTextureRef(ImTextureID texId)
    {
        this.TexID = texId;
    }

    public static implicit operator ImTextureRef(ImTextureID texId) => new(texId);
}
