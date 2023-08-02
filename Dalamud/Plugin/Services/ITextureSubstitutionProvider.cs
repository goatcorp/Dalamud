namespace Dalamud.Plugin.Services;

/// <summary>
/// Service that grants you the ability to replace texture data that is to be loaded by Dalamud.
/// </summary>
public interface ITextureSubstitutionProvider
{
    /// <summary>
    /// Delegate describing a function that may be used to intercept and replace texture data.
    /// </summary>
    /// <param name="path">The path to the texture that is to be loaded.</param>
    /// <param name="data">The texture data. Null by default, assign something if you wish to replace the data from the game dats.</param>
    public delegate void TextureDataInterceptorDelegate(string path, ref byte[]? data);
    
    /// <summary>
    /// Event that will be called once Dalamud wants to load texture data.
    /// If you have data that should replace the data from the game dats, assign it to the
    /// data argument.
    /// </summary>
    public event TextureDataInterceptorDelegate? InterceptTexDataLoad;
}
