using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Threading;

using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Logging.Internal;
using Dalamud.Storage.Assets;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>Loads and caches DirectWrite fonts from files, memory, resources, installed families, and system fallback.
/// </summary>
/// <remarks><ul>
/// <li>Faces are weakly cached by file, face index, simulations, and variable axes so fonts sharing a face share glyph
/// images. Installed faces are file-mapped by DirectWrite, while other sources are copied into memory.</li>
/// <li>Font and face caches hold weak references, while system fallbacks are retained until
/// <see cref="DropSystemFallbacks"/>.</li>
/// </ul></remarks>
internal sealed unsafe class DirectWriteFontSources : IDisposable
{
    private static readonly ModuleLog Log = ModuleLog.Create<DirectWriteFontSources>();

    // Keyed by DiagnosticKey to avoid rooting plugin assemblies and to warn only once per missing source.
    private readonly ConcurrentDictionary<string, byte> reportedMissing = new();

    // Non-installed fonts by face key, requested axis values, and locale.
    private readonly WeakCache<(DirectWriteFontFace.FaceKey Face, EquatableArray<FontAxis> Axes, string Locale), Font>
        fonts = new();

    // Keyed by SystemFontFaceId because the underlying file and face index are not known until DirectWrite opens it.
    private readonly WeakCache<(SystemFontFaceId Id, string Locale), Font> installedFonts = new();

    // Strong cache by locale so rebuilding default font chains does not re-rasterize fallback glyphs.
    private readonly ConcurrentDictionary<string, Lazy<Font>> systemFallbacks = new();

    // Shared across locales, styled fonts, and system fallbacks.
    private readonly WeakCache<DirectWriteFontFace.FaceKey, DirectWriteFontFace> faces = new();

    private readonly ColorGlyphRasterizer colorGlyphs = new();

    // Shared across faces created here so completed background glyphs drain under a single per-frame budget.
    private readonly ConcurrentGlyphQueue deferral = new();

    /// <summary>Initializes a new instance of the <see cref="DirectWriteFontSources"/> class. Starts background
    /// enumeration of installed system fonts.</summary>
    public DirectWriteFontSources() => SystemFallbackFont.Preload(this);

    // Resolved lazily so file and memory fonts do not require DalamudAssetManager.
    private static DalamudAssetManager Assets => Service<DalamudAssetManager>.Get();

    /// <summary>Gets the font for a font ID.</summary>
    /// <param name="id">The font ID.</param>
    /// <param name="locale">The locale to shape text in, such as <c>"ja-JP"</c>.</param>
    /// <returns>The font, or <see langword="null"/> if the source is unavailable.</returns>
    /// <remarks><ul>
    /// <li>Returns <see langword="null"/> when a source is missing (unavailable local asset, missing file or resource,
    /// unloaded assembly, or family not installed in DirectWrite such as legacy <c>.fon</c> raster fonts). Throws if an
    /// existing source fails to load.</li>
    /// <li><see cref="DalamudAsset.LodestoneGameSymbol"/> returns <see langword="null"/> while not downloaded locally so
    /// AXIS stands in for it.</li>
    /// </ul></remarks>
    public Font? GetFont(IFontFaceId id, string locale) =>
        id switch
        {
            DalamudAssetFontId { Asset: DalamudAsset.LodestoneGameSymbol }
                when !Assets.IsAvailableLocally(DalamudAsset.LodestoneGameSymbol) => null,
            DalamudAssetFontId a => this.GetFont(
                $"asset:{a.Asset}", $"{a.Asset}", a.FontNo, locale, () => Assets.CreateStream(a.Asset)),
            FontRegistration.Face f => this.GetRegisteredFont(f, locale),
            SystemFontFaceId s when !DirectWriteUtilities.IsFamilyInstalled(s.Family.Name.English) =>
                this.Missing(s, "The font family is not installed"),
            SystemFontFaceId s => this.GetSystemFont(s, locale),
            _ => null,
        };

    /// <summary>Gets Windows' system font fallback for a locale.</summary>
    /// <param name="locale">The locale, such as <c>"ja-JP"</c>, used to select regional glyph variants.</param>
    /// <returns>The cached fallback font, retained until <see cref="DropSystemFallbacks"/>.</returns>
    public Font GetSystemFallback(string locale)
    {
        var lazy = this.systemFallbacks.GetOrAdd(
            locale,
            static (l, self) => new(() => new SystemFallbackFont(l, self)),
            this);
        return this.systemFallbacks.ValueOrRemove(locale, lazy);
    }

    /// <summary>Clears cached system fallback fonts so subsequent requests pick up newly installed fonts.</summary>
    /// <remarks>Existing font chains continue using their already-created fallback instances.</remarks>
    public void DropSystemFallbacks() => this.systemFallbacks.Clear();

    /// <summary>Gets a face from the face cache, creating it on a miss.</summary>
    /// <param name="name">Name for debugging if created.</param>
    /// <param name="from">A face of the file.</param>
    /// <param name="simulations">DirectWrite font simulations.</param>
    /// <param name="key">The cache key, or <see langword="null"/> to derive it from <paramref name="from"/>'s disk file.
    /// </param>
    /// <returns>The cached face, or an uncached new instance when <paramref name="key"/> is <see langword="null"/> and
    /// <paramref name="from"/> has no single disk path.</returns>
    /// <remarks>Thread-safe.</remarks>
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
    /// <remarks><ul>
    /// <li>Existing fonts keep working after disposal, but rasterize synchronously and fall back to monochrome outlines
    /// for color glyphs.</li>
    /// <li>Main thread only.</li>
    /// </ul></remarks>
    public void Dispose()
    {
        this.deferral.Dispose();
        this.colorGlyphs.Dispose();
    }

    private Font? Missing(IFontFaceId id, string reason)
    {
        if (this.reportedMissing.TryAdd(id.Family.DiagnosticKey, 0))
            Log.Warning("{Reason}. {Id} is left out", reason, id);
        return null;
    }

    private DirectWriteFontFace CreateFace(
        string name, IDWriteFontFace* from, DWRITE_FONT_SIMULATIONS simulations, DirectWriteFontFace.FaceKey? key) =>
        new(name, from, simulations, this.colorGlyphs, this.deferral, this, key);

    // Loads or retrieves a non-installed font. file uniquely identifies the source across all source kinds, and open is called only on a face cache miss.
    private Font GetFont(
        string file, string name, int fontNo, string locale, Func<Stream> open, ImmutableArray<FontAxis> axes = default)
    {
        var key = new DirectWriteFontFace.FaceKey(file, fontNo, DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_NONE);
        return this.fonts.GetOrCreate(
            (key, new(axes), locale),
            () =>
            {
                var fullName = $"{name}#{fontNo}";
                var face = this.faces.GetOrCreate(
                                   key,
                                   () => new(fullName, open, key, this.colorGlyphs, this.deferral, this))
                               .WithAxes(axes);
                return new DirectWriteFont(fullName, face, locale);
            });
    }

    // Resources are keyed by ModuleVersionId rather than Assembly so the cache never roots a plugin load context, allowing reloaded builds to share faces.
    // Disk files are copied into memory for the lifetime of the cached face, and share an existing face if already opened as an installed font.
    private Font? GetRegisteredFont(FontRegistration.Face face, string locale)
    {
        var source = face.Registration.Source;
        var axes = source.Axes;
        switch (source.Kind)
        {
            case FontSource.Kinds.File when !File.Exists(source.Path):
                return this.Missing(face, "The font file does not exist");
            case FontSource.Kinds.File:
                var file = DirectWriteFontFace.FaceKey.OfFile(source.Path!, source.FontNo).File;
                return this.GetFont(
                    file,
                    Path.GetFileName(file),
                    source.FontNo,
                    locale,
                    () => File.OpenRead(source.Path!),
                    axes);
            case FontSource.Kinds.Memory:
                return this.GetFont(source.Key, source.Name, source.FontNo, locale, source.Open, axes);
            case FontSource.Kinds.ManifestResource:
                if (source.Assembly is not { } assembly)
                    return this.Missing(face, "The assembly of the font is not loaded");
                if (assembly.GetManifestResourceInfo(source.Name) is null)
                    return this.Missing(face, "The font resource does not exist");
                return this.GetFont(
                    $"resource:{assembly.ManifestModule.ModuleVersionId:N}:{source.Name}",
                    source.Name,
                    source.FontNo,
                    locale,
                    source.Open,
                    axes);
            default:
                return null;
        }
    }

    private Font GetSystemFont(SystemFontFaceId id, string locale) =>
        this.installedFonts.GetOrCreate(
            (id, locale),
            () =>
            {
                using var face = DirectWriteUtilities.CreateFace(id);
                var sims = face.Get()->GetSimulations();
                var slant = DirectWriteFontFace.TakeOblique(ref sims) ? SyntheticStyleFont.Slant : 0;
                var key = DirectWriteFontFace.FaceKey.Of(face.Get(), sims);
                var name = key is { } k
                               ? $"{Path.GetFileName(k.File)}#{k.FontNo}{(k.Axes.Length > 0 ? " " : null)}{k.Axes}"
                               : $"{id.Family.Name.English} {id.Style}";
                return new DirectWriteFont(name, this.GetFace(name, face.Get(), sims, key), locale, slant);
            });

    // Thread-safe weak-reference cache with per-key creation locking.
    private sealed class WeakCache<TKey, T>
        where TKey : notnull
        where T : class
    {
        private const int MinPruneCount = 64;

        private readonly ConcurrentDictionary<TKey, Slot> slots = new();
        private int pruneCount = MinPruneCount;

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

        // Removes unlocked slots whose weak reference is dead or failed to initialize.
        // Marking slot.Removed makes any concurrent caller that fetched the slot before TryRemove retry with a fresh slot.
        private void Prune()
        {
            foreach (var (key, slot) in this.slots)
            {
                if (!slot.Lock.TryEnter())
                    continue;
                if (slot.Item?.TryGetTarget(out _) is not true)
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
