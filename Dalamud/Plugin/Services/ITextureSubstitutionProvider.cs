namespace Dalamud.Plugin.Services;

/// <summary>
/// Service that grants you the ability to replace texture data that is to be loaded by Dalamud.
/// </summary>
public interface ITextureSubstitutionProvider
{
    /// <summary>
    /// Delegate describing a function that may be used to intercept and replace texture data.
    /// The path assigned may point to another texture inside the game's dats, or a .tex file or image on the disk.
    /// </summary>
    /// <param name="path">The path to the texture that is to be loaded.</param>
    /// <param name="replacementPath">The path that should be loaded instead.</param>
    public delegate void TextureDataInterceptorDelegate(string path, ref string? replacementPath);

    /// <summary>
    /// Event that will be called once Dalamud wants to load texture data.
    /// </summary>
    public event TextureDataInterceptorDelegate? InterceptTexDataLoad;
}
