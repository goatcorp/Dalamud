using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Data;
using Dalamud.Interface.Fonts.Internal.GameFonts.Formats;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Utility;

using Lumina.Data.Files;

using Serilog;

namespace Dalamud.Interface.Fonts.Internal.GameFonts;

/// <summary>Fonts of the game's font families, drawn from its .fdt and .tex files.</summary>
internal sealed class GameFontSources
{
    private readonly ImmutableDictionary<GameFontFamilyAndSize, Task<byte[]>> fdtFiles;
    private readonly ImmutableDictionary<string, Task<GameFontFamilyFont.Texture?[]>> textures;
    private readonly ConcurrentDictionary<GameFontFamily, Lazy<GameFontFamilyFont>> gameFontFamilies = new();

    /// <summary>Initializes a new instance of the <see cref="GameFontSources"/> class. Starts loading the files.
    /// </summary>
    public GameFontSources()
    {
        var dataManager = Service<DataManager>.Get();
        var gffasInfo = Enum.GetValues<GameFontFamilyAndSize>()
                            .Select(x => (Font: x, Attr: x.GetAttribute<GameFontFamilyAndSizeAttribute>()))
                            .Where(x => x.Attr is not null)
                            .ToArray();
        var texPaths = gffasInfo.Select(x => x.Attr.TexPathFormat).Distinct().ToArray();
        this.fdtFiles = gffasInfo.ToImmutableDictionary(
            x => x.Font,
            x => Task.Run(
                () => dataManager.GetFile(x.Attr.Path)?.Data
                      ?? throw new FileNotFoundException("The game font file is missing.", x.Attr.Path)));
        var textureCounts = texPaths.ToImmutableDictionary(
            x => x,
            x => CountTexturesAsync(
                gffasInfo.Where(y => y.Attr.TexPathFormat == x)
                         .Select(y => (y.Attr.Path, this.fdtFiles[y.Font]))
                         .ToArray()));
        this.textures = textureCounts.ToImmutableDictionary(
            x => x.Key,
            x => LoadTexturesAsync(dataManager, x.Key, x.Value));
    }

    /// <summary>Gets the font of a font id.</summary>
    /// <param name="id">The font id.</param>
    /// <returns>The font, or null for an id of another source. The game's fonts have no locale-specific forms.
    /// </returns>
    public Font? GetFont(IFontFaceId id) =>
        id is GameFontId g ? this.GetGameFontFamily(g.GameFontFamily) : null;

    // The number of texture files the .fdt files that loaded use. A failed file is logged and skipped, so that it takes
    // away only its own size.
    private static async Task<int> CountTexturesAsync((string Path, Task<byte[]> Data)[] files)
    {
        var count = 0;
        foreach (var (path, data) in files)
        {
            try
            {
                count = Math.Max(count, new FdtFileView(await data.ConfigureAwait(false)).TextureFileCount);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to load the game font file {Path}", path);
            }
        }

        return count;
    }

    // The textures of a path format, by file index. Index 0 is font1.tex. Failures are logged and leave null.
    private static async Task<GameFontFamilyFont.Texture?[]> LoadTexturesAsync(
        DataManager dataManager, string pathFormat, Task<int> textureCount)
    {
        var count = await textureCount.ConfigureAwait(false);
        if (count == 0)
            return [];
        return await Task.WhenAll(
                             Enumerable.Range(1, count)
                                       .Select(i => LoadTextureAsync(dataManager, string.Format(pathFormat, i))))
                         .ConfigureAwait(false);
    }

    // A font texture, or null if it failed to load. The pixels stay in the file's data, which is not copied.
    private static async Task<GameFontFamilyFont.Texture?> LoadTextureAsync(DataManager dataManager, string path)
    {
        try
        {
            return ToTexture(
                await dataManager.GetFileAsync<TexFile>(path, CancellationToken.None).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to load the font texture {Path}", path);
            return null;
        }
    }

    // The first image of a texture file.
    private static unsafe GameFontFamilyFont.Texture ToTexture(TexFile tex)
    {
        var (w, h, format) = (tex.Header.Width, tex.Header.Height, tex.Header.Format);
        if (format is not (TexFile.TextureFormat.B4G4R4A4 or TexFile.TextureFormat.B8G8R8A8))
            return new(w, h, false, tex.ImageData);
        var length = tex.SliceSpan(0, 0, out _, out _, out _).Length;
        return new(
            w,
            h,
            format == TexFile.TextureFormat.B4G4R4A4,
            tex.Data.AsMemory((int)tex.Header.OffsetToSurface[0], length));
    }

    // All sizes of a game font family, as one font.
    private GameFontFamilyFont GetGameFontFamily(GameFontFamily family) =>
        this.gameFontFamilies.GetOrAdd(
            family,
            key => new(() =>
            {
                var sizes = Enum.GetValues<GameFontFamilyAndSize>()
                                .Where(x => x != GameFontFamilyAndSize.Undefined && x.Family == key)
                                .Select(x => (Size: x, Attr: x.GetAttribute<GameFontFamilyAndSizeAttribute>()!))
                                .ToArray();
                if (sizes.Length == 0)
                    throw new ArgumentOutOfRangeException(nameof(family), family, null);

                // A size whose .fdt failed is left out. CountTexturesAsync has logged it. If all failed, the first
                // one throws its error below.
                var loaded = sizes.Where(x => this.WaitForFdt(x.Size)).ToArray();
                if (loaded.Length == 0)
                    loaded = sizes[..1];
                return new(
                    key.ToString(),
                    loaded.Select(x => (this.GetFdtView(x.Size), x.Attr.HorizontalOffset)),
                    this.textures[sizes[0].Attr.TexPathFormat].GetAwaiter().GetResult());
            })).Value;

    // A game font file (.fdt) reader. Blocks until loaded.
    private FdtFileView GetFdtView(GameFontFamilyAndSize gffas) => new(this.fdtFiles[gffas].GetAwaiter().GetResult());

    // Waits for a game font file (.fdt) and returns whether it loaded.
    private bool WaitForFdt(GameFontFamilyAndSize gffas)
    {
        Task task = this.fdtFiles[gffas];
        task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing).GetAwaiter().GetResult();
        return task.IsCompletedSuccessfully;
    }
}
