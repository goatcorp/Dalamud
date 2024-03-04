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
    string? ITextureProvider.GetIconPath(uint iconId, ITextureProvider.IconFlags flags, ClientLanguage? language)
        => this.TryGetIconPath(
               new(
                   iconId,
                   (flags & ITextureProvider.IconFlags.ItemHighQuality) != 0,
                   (flags & ITextureProvider.IconFlags.HiRes) != 0,
                   language),
               out var path)
               ? path
               : null;

    /// <inheritdoc/>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    [Obsolete("See interface definition.")]
    IDalamudTextureWrap? ITextureProvider.GetIcon(
        uint iconId,
        ITextureProvider.IconFlags flags,
        ClientLanguage? language,
        bool keepAlive) =>
        this.ManagerOrThrow.Shared.GetFromGameIcon(
                new(
                    iconId,
                    (flags & ITextureProvider.IconFlags.ItemHighQuality) != 0,
                    (flags & ITextureProvider.IconFlags.HiRes) != 0,
                    language))
            .GetAvailableOnAccessWrapForApi9();

    /// <inheritdoc/>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    [Obsolete("See interface definition.")]
    IDalamudTextureWrap? ITextureProvider.GetTextureFromGame(string path, bool keepAlive) =>
        this.ManagerOrThrow.Shared.GetFromGame(path).GetAvailableOnAccessWrapForApi9();

    /// <inheritdoc/>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    [Obsolete("See interface definition.")]
    IDalamudTextureWrap? ITextureProvider.GetTextureFromFile(FileInfo file, bool keepAlive) =>
        this.ManagerOrThrow.Shared.GetFromFile(file.FullName).GetAvailableOnAccessWrapForApi9();
}
