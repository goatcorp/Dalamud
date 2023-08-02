using System;

using Dalamud.Interface.Internal;
using Lumina.Data.Files;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Service that grants you access to textures you may render via ImGui.
/// </summary>
public interface ITextureProvider
{
    /// <summary>
    /// Flags describing the icon you wish to receive.
    /// </summary>
    [Flags]
    public enum IconFlags
    {
        /// <summary>
        /// Low-resolution, standard quality icon.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// If this icon is an item icon, and it has a high-quality variant, receive the high-quality version.
        /// Null if the item does not have a high-quality variant.
        /// </summary>
        ItemHighQuality = 1 << 0,
        
        /// <summary>
        /// Get the hi-resolution version of the icon, if it exists.
        /// </summary>
        HiRes = 1 << 1,
    }
    
    /// <summary>
    /// Get a texture handle for a specific icon.
    /// </summary>
    /// <param name="iconId">The ID of the icon to load.</param>
    /// <param name="flags">Options to be considered when loading the icon.</param>
    /// <param name="language">
    /// The language to be considered when loading the icon, if the icon has versions for multiple languages.
    /// If null, default to the game's current language.
    /// </param>
    /// <param name="keepAlive">
    /// Prevent Dalamud from automatically unloading this icon to save memory. Usually does not need to be set.
    /// </param>
    /// <returns>
    /// Null, if the icon does not exist in the specified configuration, or a texture wrap that can be used
    /// to render the icon.
    /// </returns>
    public IDalamudTextureWrap? GetIcon(uint iconId, IconFlags flags = IconFlags.HiRes, ClientLanguage? language = null, bool keepAlive = false);

    /// <summary>
    /// Get a texture handle for the texture at the specified path.
    /// You may only specify paths in the game's VFS.
    /// </summary>
    /// <param name="path">The path to the texture in the game's VFS.</param>
    /// <param name="keepAlive">Prevent Dalamud from automatically unloading this texture to save memory. Usually does not need to be set.</param>
    /// <returns>Null, if the icon does not exist, or a texture wrap that can be used to render the texture.</returns>
    public IDalamudTextureWrap? GetTextureFromGamePath(string path, bool keepAlive = false);
    
    /// <summary>
    /// Get a texture handle for the specified Lumina TexFile.
    /// </summary>
    /// <param name="file">The texture to obtain a handle to.</param>
    /// <returns>A texture wrap that can be used to render the texture.</returns>
    public IDalamudTextureWrap GetTexture(TexFile file);
}
