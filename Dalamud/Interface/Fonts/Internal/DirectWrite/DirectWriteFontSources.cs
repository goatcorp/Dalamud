using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;

using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Storage.Assets;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>Fonts of OpenType files, and the system font fallback, drawn with DirectWrite.</summary>
/// <remarks><ul>
/// <li>Faces are cached by file, face, simulations and axis values, so that the fonts of a face share its glyph images.
/// Installed faces are mapped from their files by DirectWrite. Other files are copied into memory.</li>
/// <li>The caches keep fonts and faces only while a font uses them. The system fallbacks are kept.</li>
/// </ul></remarks>
internal sealed unsafe class DirectWriteFontSources : IDisposable
{
    // Fonts of faces that are not installed, by face and locale.
    private readonly WeakCache<(DirectWriteFontFace.FaceKey Face, string Locale), Font> fonts = new();

    // By id, as the face is known only once DirectWrite opened it.
    private readonly WeakCache<(SystemFontFaceId Id, string Locale), Font> installedFonts = new();

    // By locale. Kept across default chains, so that making the chain again does not rasterize its glyphs again.
    private readonly ConcurrentDictionary<string, Lazy<Font>> systemFallbacks = new();

    // Shared by the fonts of all locales, the styled fonts and the system fallbacks.
    private readonly WeakCache<DirectWriteFontFace.FaceKey, DirectWriteFontFace> faces = new();

    private readonly ColorGlyphRasterizer colorGlyphs = new();

    // Shared by the faces made here, so that their finished glyphs are added within one budget per frame.
    private readonly ConcurrentGlyphQueue<GlyphRasterRequest> deferral = DirectWriteFontFace.CreateDeferral();

    /// <summary>Initializes a new instance of the <see cref="DirectWriteFontSources"/> class. Makes Windows enumerate
    /// the installed fonts in the background.</summary>
    public DirectWriteFontSources() => SystemFallbackFont.Preload();

    // Taken when an asset is asked for, so that fonts of files need no services.
    private static DalamudAssetManager Assets => Service<DalamudAssetManager>.Get();

    /// <summary>Gets the font of a font id.</summary>
    /// <param name="id">The font id.</param>
    /// <param name="locale">The locale to draw text in, such as "ja-JP".</param>
    /// <returns>The font, or null if declined.</returns>
    /// <remarks><ul>
    /// <li>A font that is not there is declined. A font that is there but fails to load throws.</li>
    /// <li>The game symbol asset is declined while its file is missing. AXIS stands in for it then.</li>
    /// <li>A file or a manifest resource that does not exist is declined, and so is a resource of an unloaded assembly
    /// and a family DirectWrite does not have installed. Raster fonts (.fon), such as Fixedsys, are never installed
    /// for DirectWrite.</li>
    /// </ul></remarks>
    public Font? GetFont(IFontFaceId id, string locale) =>
        id switch
        {
            DalamudAssetFontId { Asset: DalamudAsset.LodestoneGameSymbol }
                when !Assets.IsAvailableLocally(DalamudAsset.LodestoneGameSymbol) => null,
            DalamudAssetFontId a => this.GetAssetFont(a.Asset, a.FontNo, locale),
            FileFontId f when !File.Exists(f.Path) => null,
            FileFontId f => this.GetFileFont(f.Path, f.FontNo, locale),
            StreamFontId s => this.GetStreamFont(s, locale),
            ManifestResourceFontId { Assembly: { } asm } r
                when asm.GetManifestResourceInfo(r.ResourceName) is not null =>
                this.GetResourceFont(asm, r.ResourceName, r.FontNo, locale),
            ManifestResourceFontId => null,
            SystemFontFaceId s when !DirectWriteUtilities.IsFamilyInstalled(s.Family.Name.English) => null,
            SystemFontFaceId s => this.GetSystemFont(s, locale),
            _ => null,
        };

    /// <summary>Gets Windows' system font fallback for a locale.</summary>
    /// <param name="locale">The locale, such as "ja-JP". It decides between Han glyph variants.</param>
    /// <returns>The font, kept for good.</returns>
    public Font GetSystemFallback(string locale) =>
        GetOrCreate(this.systemFallbacks, locale, () => new SystemFallbackFont(locale, this));

    /// <summary>Gets a face from the face cache, making it if needed.</summary>
    /// <param name="name">Name for debugging, if the face is made.</param>
    /// <param name="from">A face of the file.</param>
    /// <param name="simulations">What DirectWrite is to simulate.</param>
    /// <param name="key">The key of the face, or null to take it from the file of <paramref name="from"/>.</param>
    /// <returns>The face. Without <paramref name="key"/>, a face of a file that is not on disk is made anew.
    /// </returns>
    /// <remarks>Called on any thread.</remarks>
    public DirectWriteFontFace GetFace(
        string name, IDWriteFontFace* from, DWRITE_FONT_SIMULATIONS simulations,
        DirectWriteFontFace.FaceKey? key = null)
    {
        key ??= DirectWriteFontFace.FaceKey.Of(from, simulations);
        if (key is not { } k)
            return this.CreateFace(name, from, simulations, null);
        var pFrom = (nint)from;
        return this.faces.GetOrCreate(k, () => this.CreateFace(name, (IDWriteFontFace*)pFrom, simulations, k));
    }

    /// <inheritdoc/>
    /// <remarks>Fonts made here keep working, but draw the outlines of color glyphs from then on. They rasterize every
    /// glyph in the frame it is asked for. Called on the main thread.</remarks>
    public void Dispose()
    {
        this.deferral.Dispose();
        this.colorGlyphs.Dispose();
    }

    // Gets a font from a cache, making it if needed. One that failed to be made is not kept, so that it is tried again.
    private static T GetOrCreate<TKey, T>(ConcurrentDictionary<TKey, Lazy<T>> fonts, TKey key, Func<T> create)
        where TKey : notnull
    {
        var lazy = fonts.GetOrAdd(key, static (_, c) => new(c), create);
        try
        {
            return lazy.Value;
        }
        catch
        {
            fonts.TryRemove(new(key, lazy));
            throw;
        }
    }

    private DirectWriteFontFace CreateFace(
        string name, IDWriteFontFace* from, DWRITE_FONT_SIMULATIONS simulations, DirectWriteFontFace.FaceKey? key) =>
        new(name, from, simulations, this.colorGlyphs, this.deferral, this, key);

    private DirectWriteFontFace CreateFace(string name, ReadOnlySpan<byte> data, DirectWriteFontFace.FaceKey key) =>
        new(name, data, key.FontNo, this.colorGlyphs, this.deferral, this, key);

    // DirectWrite copies the data. Mapped resources of an assembly loaded from a file are copied without a managed copy.
    private DirectWriteFontFace CreateFace(string name, Stream stream, DirectWriteFontFace.FaceKey key)
    {
        using (stream)
        {
            if (stream is UnmanagedMemoryStream mapped)
            {
                var data = new ReadOnlySpan<byte>(mapped.PositionPointer, checked((int)mapped.Length));
                return this.CreateFace(name, data, key);
            }

            if (stream is MemoryStream m && m.TryGetBuffer(out var buffer))
                return this.CreateFace(name, buffer, key);

            using var ms = new MemoryStream(stream.CanSeek ? checked((int)stream.Length) : 0);
            stream.CopyTo(ms);
            return this.CreateFace(name, ms.GetBuffer().AsSpan(0, (int)ms.Length), key);
        }
    }

    // Gets a font of a face that is not installed. file is unique across files and kinds of ids, and name is for
    // debugging. open is called only if the face is not cached.
    private Font GetFont(string file, string name, int fontNo, string locale, Func<Stream> open)
    {
        var key = new DirectWriteFontFace.FaceKey(file, fontNo, DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_NONE);
        return this.fonts.GetOrCreate(
            (key, locale),
            () =>
            {
                var fullName = $"{name}#{fontNo}";
                var face = this.faces.GetOrCreate(key, () => this.CreateFace(fullName, open(), key));
                return new DirectWriteFont(fullName, face, locale);
            });
    }

    private Font GetStreamFont(StreamFontId id, string locale) =>
        this.GetFont(
            $"stream:{id.Key}",
            id.Name.English,
            id.FontNo,
            locale,
            () => new MemoryStream(id.Data, 0, id.Data.Length, false, true));

    // The key holds the module version id, not the assembly, so that the cache does not keep a plugin's load context
    // alive. A plugin loaded again from the same build shares the face, which outlives an unloaded plugin.
    private Font GetResourceFont(Assembly assembly, string resourceName, int fontNo, string locale) =>
        this.GetFont(
            $"resource:{assembly.ManifestModule.ModuleVersionId:N}:{resourceName}",
            resourceName,
            fontNo,
            locale,
            () => assembly.GetManifestResourceStream(resourceName)
                  ?? throw new FileNotFoundException("The resource is gone.", resourceName));

    private Font GetAssetFont(DalamudAsset asset, int fontNo, string locale) =>
        this.GetFont($"asset:{asset}", $"{asset}", fontNo, locale, () => Assets.CreateStream(asset));

    // The file is copied, so that it can be changed or deleted while the font lives. An installed face that another
    // font took first is shared.
    private Font GetFileFont(string path, int fontNo, string locale)
    {
        var file = DirectWriteFontFace.FaceKey.OfFile(path, fontNo).File;
        return this.GetFont(file, Path.GetFileName(file), fontNo, locale, () => File.OpenRead(path));
    }

    // DirectWrite maps the file. The face is shared with the fonts of other ids and locales that come to it.
    private Font GetSystemFont(SystemFontFaceId id, string locale) =>
        this.installedFonts.GetOrCreate(
            (id, locale),
            () =>
            {
                using var face = DirectWriteUtilities.CreateFace(id);
                var sims = face.Get()->GetSimulations();
                var key = DirectWriteFontFace.FaceKey.Of(face.Get(), sims);
                var name = key is { } k
                               ? $"{Path.GetFileName(k.File)}#{k.FontNo}{(k.Axes.Length > 0 ? " " : null)}{k.Axes}"
                               : $"{id.Family.Name.English} {id.Style}";
                return new DirectWriteFont(name, this.GetFace(name, face.Get(), sims, key), locale);
            });

    // A cache that keeps its items only while something else uses them. Made on font loads, not per frame. One item is
    // made per key at a time.
    private sealed class WeakCache<TKey, T>
        where TKey : notnull
        where T : class
    {
        // The count from which collected items are looked for.
        private const int MinPruneCount = 64;

        private readonly ConcurrentDictionary<TKey, Slot> slots = new();
        private int pruneCount = MinPruneCount;

        // Gets an item, making it if it is not cached or was collected.
        public T GetOrCreate(TKey key, Func<T> create)
        {
            while (true)
            {
                var slot = this.slots.GetOrAdd(key, static _ => new());
                using (slot.Lock.EnterScope())
                {
                    if (slot.Removed)
                        continue;
                    if (slot.Item?.TryGetTarget(out var item) is not true)
                    {
                        item = create();
                        if (slot.Item is null)
                            slot.Item = new(item);
                        else
                            slot.Item.SetTarget(item);
                    }

                    if (this.slots.Count >= Volatile.Read(ref this.pruneCount))
                        this.Prune();
                    return item;
                }
            }
        }

        // Drops the slots of collected items. A slot whose item is being made has no item yet, and is kept.
        private void Prune()
        {
            foreach (var (key, slot) in this.slots)
            {
                if (!slot.Lock.TryEnter())
                    continue;
                if (slot.Item?.TryGetTarget(out _) is false)
                {
                    slot.Removed = true;
                    this.slots.TryRemove(new(key, slot));
                }

                slot.Lock.Exit();
            }

            Volatile.Write(ref this.pruneCount, Math.Max(MinPruneCount, this.slots.Count * 2));
        }

        private sealed class Slot
        {
            public Lock Lock { get; } = new();

            public WeakReference<T>? Item { get; set; }

            public bool Removed { get; set; }
        }
    }
}
