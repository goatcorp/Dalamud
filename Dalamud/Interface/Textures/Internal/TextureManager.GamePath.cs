using System.Collections.Generic;
using System.IO;

using Dalamud.Game;
using Dalamud.Plugin.Services;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
internal sealed partial class TextureManager
{
    private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}.tex";
    private const string HighResolutionIconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}_hr1.tex";

    /// <inheritdoc/>
    public event ITextureSubstitutionProvider.TextureDataInterceptorDelegate? InterceptTexDataLoad;

    /// <inheritdoc/>
    public bool TryGetIconPath(in GameIconLookup lookup, out string path)
    {
        // 1. Item
        path = FormatIconPath(
            lookup.IconId,
            lookup.ItemHq ? "hq/" : string.Empty,
            lookup.HiRes);
        if (this.dataManager.FileExists(path))
            return true;

        var languageFolder = (lookup.Language ?? (ClientLanguage)(int)this.dalamud.StartInfo.Language) switch
        {
            ClientLanguage.Japanese => "ja/",
            ClientLanguage.English => "en/",
            ClientLanguage.German => "de/",
            ClientLanguage.French => "fr/",
            _ => null,
        };

        if (languageFolder is not null)
        {
            // 2. Regular icon, with language, hi-res
            path = FormatIconPath(
                lookup.IconId,
                languageFolder,
                lookup.HiRes);
            if (this.dataManager.FileExists(path))
                return true;

            if (lookup.HiRes)
            {
                // 3. Regular icon, with language, no hi-res
                path = FormatIconPath(
                    lookup.IconId,
                    languageFolder,
                    false);
                if (this.dataManager.FileExists(path))
                    return true;
            }
        }

        // 4. Regular icon, without language, hi-res
        path = FormatIconPath(
            lookup.IconId,
            null,
            lookup.HiRes);
        if (this.dataManager.FileExists(path))
            return true;

        // 4. Regular icon, without language, no hi-res
        if (lookup.HiRes)
        {
            path = FormatIconPath(
                lookup.IconId,
                null,
                false);
            if (this.dataManager.FileExists(path))
                return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public string GetIconPath(in GameIconLookup lookup) =>
        this.TryGetIconPath(lookup, out var path) ? path : throw new FileNotFoundException();

    /// <inheritdoc/>
    public string GetSubstitutedPath(string originalPath)
    {
        if (this.InterceptTexDataLoad == null)
            return originalPath;

        string? interceptPath = null;
        this.InterceptTexDataLoad.Invoke(originalPath, ref interceptPath);

        if (interceptPath != null)
        {
            Log.Verbose("Intercept: {OriginalPath} => {ReplacePath}", originalPath, interceptPath);
            return interceptPath;
        }

        return originalPath;
    }

    /// <inheritdoc/>
    public void InvalidatePaths(IEnumerable<string> paths)
    {
        foreach (var path in paths)
            this.Shared.FlushFromGameCache(path);
    }

    private static string FormatIconPath(uint iconId, string? type, bool highResolution)
    {
        var format = highResolution ? HighResolutionIconFileFormat : IconFileFormat;

        type ??= string.Empty;
        if (type.Length > 0 && !type.EndsWith('/'))
            type += '/';

        return string.Format(format, iconId / 1000, type, iconId);
    }
}
