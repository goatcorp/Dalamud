using System.IO;

using Dalamud.Interface.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

namespace Dalamud.Interface.Textures.Internal;

#pragma warning disable CS0618 // Type or member is obsolete

/// <summary>Plugin-scoped version of <see cref="TextureManager"/>.</summary>
internal sealed partial class TextureManagerPluginScoped
{
    /// <inheritdoc/>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    [Obsolete("See interface definition.")]
    public IDalamudTextureWrap? GetIcon(
        uint iconId,
        ITextureProvider.IconFlags flags = ITextureProvider.IconFlags.HiRes,
        ClientLanguage? language = null,
        bool keepAlive = false)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    [Obsolete("See interface definition.")]
    public string? GetIconPath(
        uint iconId,
        ITextureProvider.IconFlags flags = ITextureProvider.IconFlags.HiRes,
        ClientLanguage? language = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    [Obsolete("See interface definition.")]
    public IDalamudTextureWrap? GetTextureFromGame(
        string path,
        bool keepAlive = false)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    [Obsolete("See interface definition.")]
    public IDalamudTextureWrap? GetTextureFromFile(
        FileInfo file,
        bool keepAlive = false)
    {
        throw new NotImplementedException();
    }
}
