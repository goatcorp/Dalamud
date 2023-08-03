using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

using ImGuiScene;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Excel;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class provides data for Dalamud-internal features, but can also be used by plugins if needed.
/// </summary>
public interface IDataManager
{
    /// <summary>
    /// Gets the current game client language.
    /// </summary>
    public ClientLanguage Language { get; }

    /// <summary>
    /// Gets the OpCodes sent by the server to the client.
    /// </summary>
    public ReadOnlyDictionary<string, ushort> ServerOpCodes { get; }

    /// <summary>
    /// Gets the OpCodes sent by the client to the server.
    /// </summary>
    public ReadOnlyDictionary<string, ushort> ClientOpCodes { get; }

    /// <summary>
    /// Gets a <see cref="Lumina"/> object which gives access to any excel/game data.
    /// </summary>
    public GameData GameData { get; }

    /// <summary>
    /// Gets an <see cref="ExcelModule"/> object which gives access to any of the game's sheet data.
    /// </summary>
    public ExcelModule Excel { get; }

    /// <summary>
    /// Gets a value indicating whether Game Data is ready to be read.
    /// </summary>
    public bool IsDataReady { get; }

    /// <summary>
    /// Gets a value indicating whether the game data files have been modified by another third-party tool.
    /// </summary>
    public bool HasModifiedGameDataFiles { get; }

    /// <summary>
    /// Get an <see cref="ExcelSheet{T}"/> with the given Excel sheet row type.
    /// </summary>
    /// <typeparam name="T">The excel sheet type to get.</typeparam>
    /// <returns>The <see cref="ExcelSheet{T}"/>, giving access to game rows.</returns>
    public ExcelSheet<T>? GetExcelSheet<T>() where T : ExcelRow;

    /// <summary>
    /// Get an <see cref="ExcelSheet{T}"/> with the given Excel sheet row type with a specified language.
    /// </summary>
    /// <param name="language">Language of the sheet to get.</param>
    /// <typeparam name="T">The excel sheet type to get.</typeparam>
    /// <returns>The <see cref="ExcelSheet{T}"/>, giving access to game rows.</returns>
    public ExcelSheet<T>? GetExcelSheet<T>(ClientLanguage language) where T : ExcelRow;

    /// <summary>
    /// Get a <see cref="FileResource"/> with the given path.
    /// </summary>
    /// <param name="path">The path inside of the game files.</param>
    /// <returns>The <see cref="FileResource"/> of the file.</returns>
    public FileResource? GetFile(string path);

    /// <summary>
    /// Get a <see cref="FileResource"/> with the given path, of the given type.
    /// </summary>
    /// <typeparam name="T">The type of resource.</typeparam>
    /// <param name="path">The path inside of the game files.</param>
    /// <returns>The <see cref="FileResource"/> of the file.</returns>
    public T? GetFile<T>(string path) where T : FileResource;

    /// <summary>
    /// Check if the file with the given path exists within the game's index files.
    /// </summary>
    /// <param name="path">The path inside of the game files.</param>
    /// <returns>True if the file exists.</returns>
    public bool FileExists(string path);

    /// <summary>
    /// Get a <see cref="TexFile"/> containing the icon with the given ID.
    /// </summary>
    /// <param name="iconId">The icon ID.</param>
    /// <param name="highResolution">Return high resolution version.</param>
    /// <returns>The <see cref="TexFile"/> containing the icon.</returns>
    [Obsolete("Use ITextureProvider instead")]
    public TexFile? GetIcon(uint iconId, bool highResolution = false);

    /// <summary>
    /// Get a <see cref="TexFile"/> containing the icon with the given ID, of the given language.
    /// </summary>
    /// <param name="iconLanguage">The requested language.</param>
    /// <param name="iconId">The icon ID.</param>
    /// <param name="highResolution">Return high resolution version.</param>
    /// <returns>The <see cref="TexFile"/> containing the icon.</returns>
    [Obsolete("Use ITextureProvider instead")]
    public TexFile? GetIcon(ClientLanguage iconLanguage, uint iconId, bool highResolution = false);

    /// <summary>
    /// Get a <see cref="TexFile"/> containing the icon with the given ID, of the given type.
    /// </summary>
    /// <param name="type">The type of the icon (e.g. 'hq' to get the HQ variant of an item icon).</param>
    /// <param name="iconId">The icon ID.</param>
    /// <param name="highResolution">Return high resolution version.</param>
    /// <returns>The <see cref="TexFile"/> containing the icon.</returns>
    [Obsolete("Use ITextureProvider instead")]
    public TexFile? GetIcon(string? type, uint iconId, bool highResolution = false);
    
    /// <summary>
    /// Get a <see cref="TextureWrap"/> containing the icon with the given ID.
    /// </summary>
    /// <param name="iconId">The icon ID.</param>
    /// <param name="highResolution">Return the high resolution version.</param>
    /// <returns>The <see cref="TextureWrap"/> containing the icon.</returns>
    [Obsolete("Use ITextureProvider instead")]
    public TextureWrap? GetImGuiTextureIcon(uint iconId, bool highResolution = false);

    /// <summary>
    /// Get a <see cref="TexFile"/> containing the icon with the given ID, of the given quality.
    /// </summary>
    /// <param name="isHq">A value indicating whether the icon should be HQ.</param>
    /// <param name="iconId">The icon ID.</param>
    /// <returns>The <see cref="TexFile"/> containing the icon.</returns>
    [Obsolete("Use ITextureProvider instead")]
    public TexFile? GetIcon(bool isHq, uint iconId);
    
    /// <summary>
    /// Get a <see cref="TexFile"/> containing the HQ icon with the given ID.
    /// </summary>
    /// <param name="iconId">The icon ID.</param>
    /// <returns>The <see cref="TexFile"/> containing the icon.</returns>
    [Obsolete("Use ITextureProvider instead")]
    public TexFile? GetHqIcon(uint iconId);

    /// <summary>
    /// Get the passed <see cref="TexFile"/> as a drawable ImGui TextureWrap.
    /// </summary>
    /// <param name="tex">The Lumina <see cref="TexFile"/>.</param>
    /// <returns>A <see cref="TextureWrap"/> that can be used to draw the texture.</returns>
    [Obsolete("Use ITextureProvider instead")]
    [return: NotNullIfNotNull(nameof(tex))]
    public TextureWrap? GetImGuiTexture(TexFile? tex);

    /// <summary>
    /// Get the passed texture path as a drawable ImGui TextureWrap.
    /// </summary>
    /// <param name="path">The internal path to the texture.</param>
    /// <returns>A <see cref="TextureWrap"/> that can be used to draw the texture.</returns>
    [Obsolete("Use ITextureProvider instead")]
    public TextureWrap? GetImGuiTexture(string path);

    /// <summary>
    /// Get a <see cref="TextureWrap"/> containing the icon with the given ID, of the given quality.
    /// </summary>
    /// <param name="isHq">A value indicating whether the icon should be HQ.</param>
    /// <param name="iconId">The icon ID.</param>
    /// <returns>The <see cref="TextureWrap"/> containing the icon.</returns>
    [Obsolete("Use ITextureProvider instead")]
    public TextureWrap? GetImGuiTextureIcon(bool isHq, uint iconId);

    /// <summary>
    /// Get a <see cref="TextureWrap"/> containing the icon with the given ID, of the given language.
    /// </summary>
    /// <param name="iconLanguage">The requested language.</param>
    /// <param name="iconId">The icon ID.</param>
    /// <returns>The <see cref="TextureWrap"/> containing the icon.</returns>
    [Obsolete("Use ITextureProvider instead")]
    public TextureWrap? GetImGuiTextureIcon(ClientLanguage iconLanguage, uint iconId);

    /// <summary>
    /// Get a <see cref="TextureWrap"/> containing the icon with the given ID, of the given type.
    /// </summary>
    /// <param name="type">The type of the icon (e.g. 'hq' to get the HQ variant of an item icon).</param>
    /// <param name="iconId">The icon ID.</param>
    /// <returns>The <see cref="TextureWrap"/> containing the icon.</returns>
    [Obsolete("Use ITextureProvider instead")]
    public TextureWrap? GetImGuiTextureIcon(string type, uint iconId);

    /// <summary>
    /// Get a <see cref="TextureWrap"/> containing the HQ icon with the given ID.
    /// </summary>
    /// <param name="iconId">The icon ID.</param>
    /// <returns>The <see cref="TextureWrap"/> containing the icon.</returns>
    [Obsolete("Use ITextureProvider instead")]
    public TextureWrap? GetImGuiTextureHqIcon(uint iconId);
}
