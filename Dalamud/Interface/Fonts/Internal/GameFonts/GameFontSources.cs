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

/// <summary>Fonts of the game's font families, drawn from its <c>.fdt</c> and <c>.tex</c> files.</summary>
internal sealed class GameFontSources
{
    private const string TexPathFormat = "common/font/font{0}.tex";

    private readonly ImmutableDictionary<GameFontFamilyAndSize, Task<FdtFileView>> fdtFiles;
    private readonly Task<GameFontFamilyFont.Texture?[]> textures;
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
        this.fdtFiles = gffasInfo.ToImmutableDictionary(
            x => x.Font,
            x => Task.Run(
                () =>
                {
                    var data = dataManager.GetFile(x.Attr.Path)?.Data
                               ?? throw new FileNotFoundException("The game font file is missing.", x.Attr.Path);

                    return new FdtFileView(data);
                }));
        this.textures = LoadTexturesAsync(
            dataManager,
            CountTexturesAsync([.. gffasInfo.Select(x => (x.Attr.Path, this.fdtFiles[x.Font]))]));
    }

    /// <summary>Gets the font for a font ID.</summary>
    /// <param name="id">The font ID.</param>
    /// <returns>The game font, or <see langword="null"/> if <paramref name="id"/> is not a <see cref="GameFontId"/>.
    /// </returns>
    public Font? GetFont(IFontFaceId id) =>
        id is GameFontId g ? this.GetGameFontFamily(g.GameFontFamily) : null;

    // Returns the maximum texture count across loaded .fdt files, logging and skipping failed loads.
    private static async Task<int> CountTexturesAsync((string Path, Task<FdtFileView> Fdt)[] files)
    {
        var count = 0;
        foreach (var (path, fdt) in files)
        {
            try
            {
                count = Math.Max(count, (await fdt.ConfigureAwait(false)).TextureFileCount);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to load the game font file {Path}", path);
            }
        }

        return count;
    }

    // Loads font1.tex through fontN.tex into a 0-based array, logging failures as null.
    private static async Task<GameFontFamilyFont.Texture?[]> LoadTexturesAsync(
        DataManager dataManager, Task<int> textureCount)
    {
        var count = await textureCount.ConfigureAwait(false);
        if (count == 0)
            return [];
        return await Task.WhenAll(
                             Enumerable.Range(1, count)
                                       .Select(i => LoadTextureAsync(dataManager, string.Format(TexPathFormat, i))))
                         .ConfigureAwait(false);
    }

    // Loads one font texture, logging and returning null on failure.
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

    // Wraps surface 0 directly for B4G4R4A4 and B8G8R8A8 without copying, and decodes other formats to B8G8R8A8 via ImageData.
    private static unsafe GameFontFamilyFont.Texture ToTexture(TexFile tex)
    {
        var (w, h, format) = (tex.Header.Width, tex.Header.Height, tex.Header.Format);
        if (format is not (TexFile.TextureFormat.B4G4R4A4 or TexFile.TextureFormat.B8G8R8A8))
            return new(w, h, TexFile.TextureFormat.B8G8R8A8, tex.ImageData);
        var length = tex.SliceSpan(0, 0, out _, out _, out _).Length;
        return new(w, h, format, tex.Data.AsMemory((int)tex.Header.OffsetToSurface[0], length));
    }

    private GameFontFamilyFont GetGameFontFamily(GameFontFamily family)
    {
        var lazy = this.gameFontFamilies.GetOrAdd(
            family,
            key => new(() =>
            {
                var sizes = Enum.GetValues<GameFontFamilyAndSize>()
                                .Where(x => x != GameFontFamilyAndSize.Undefined && x.Family == key)
                                .Select(x => (Size: x, Attr: x.GetAttribute<GameFontFamilyAndSizeAttribute>()!))
                                .ToArray();
                if (sizes.Length == 0)
                    throw new ArgumentOutOfRangeException(nameof(family), family, null);

                // Omit strikes whose .fdt failed to load (already logged by CountTexturesAsync). If all failed, rethrow the first below.
                var loaded = sizes.Where(x => this.WaitForFdt(x.Size)).ToArray();
                if (loaded.Length == 0)
                    loaded = sizes[..1];
                return new(
                    key.ToString(),
                    loaded.Select(x => (this.fdtFiles[x.Size].GetAwaiter().GetResult(), x.Attr.HorizontalOffset)),
                    this.textures.GetAwaiter().GetResult());
            }));

        return this.gameFontFamilies.ValueOrRemove(family, lazy);
    }

    private bool WaitForFdt(GameFontFamilyAndSize gffas)
    {
        Task task = this.fdtFiles[gffas];
        task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing).GetAwaiter().GetResult();
        return task.IsCompletedSuccessfully;
    }
}
