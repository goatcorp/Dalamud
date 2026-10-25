using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Utilities using DirectWrite.</summary>
internal static unsafe class DirectWriteUtilities
{
    // Maximum cached family-name lookups, including negative misses for uninstalled fonts.
    private const int MaxCachedFamilyNames = 512;

    private static readonly Lazy<ComPtr<IDWriteFactory5>> FactoryLazy =
        new(CreateFactory, LazyThreadSafetyMode.ExecutionAndPublication);

    // The result of TryGetFamilyName by name. An entry of an older generation is not valid.
    private static readonly ConcurrentDictionary<string, FamilyNameEntry> FamilyNames =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Lazy<ComPtr<IDWriteRenderingParams>> RenderingParamsLazy =
        new(
            static () =>
            {
                var p = default(ComPtr<IDWriteRenderingParams>);
                Factory->CreateRenderingParams(p.GetAddressOf()).ThrowOnError();
                return p;
            },
            LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<ComPtr<IDWriteTextAnalyzer>> TextAnalyzerLazy =
        new(
            static () =>
            {
                var analyzer = default(ComPtr<IDWriteTextAnalyzer>);
                Factory->CreateTextAnalyzer(analyzer.GetAddressOf()).ThrowOnError();
                return analyzer;
            },
            LazyThreadSafetyMode.ExecutionAndPublication);

    // Counts refreshes of the installed fonts, so that a lookup that began before one is not used later.
    private static int familyNameGeneration;

    /// <summary>Gets the isolated DirectWrite factory that all fonts are made, shaped and rasterized with.</summary>
    public static IDWriteFactory5* Factory => FactoryLazy.Value.Get();

    /// <summary>Gets the text analyzer of <see cref="Factory"/>, kept for the process.</summary>
    /// <remarks>It is free-threaded.</remarks>
    public static IDWriteTextAnalyzer* TextAnalyzer => TextAnalyzerLazy.Value.Get();

    /// <summary>Gets the rendering parameters of the primary monitor, to pick rendering modes with.</summary>
    public static IDWriteRenderingParams* RenderingParams => RenderingParamsLazy.Value.Get();

    /// <summary>Lists the installed font families.</summary>
    /// <param name="refresh">Whether to look for changes to the installed fonts.</param>
    /// <returns>The families. Those that fail to load are left out.</returns>
    /// <remarks>Refreshing clears the family-name cache and triggers a font rebuild so system fallbacks pick up newly
    /// installed fonts.</remarks>
    public static List<IFontFamilyId> ListFamilies(bool refresh)
    {
        using var collection = default(ComPtr<IDWriteFontCollection>);
        Factory->GetSystemFontCollection(collection.GetAddressOf(), refresh).ThrowOnError();
        if (refresh)
        {
            Interlocked.Increment(ref familyNameGeneration);
            FamilyNames.Clear();
            Service<FontFactory>.GetNullable()?.OnSystemFontsRefreshed();
        }

        var count = (int)collection.Get()->GetFontFamilyCount();
        var result = new List<IFontFamilyId>(count);
        for (var i = 0; i < count; i++)
        {
            using var family = default(ComPtr<IDWriteFontFamily>);
            if (collection.Get()->GetFontFamily((uint)i, family.GetAddressOf()).FAILED)
                continue;

            try
            {
                using var names = default(ComPtr<IDWriteLocalizedStrings>);
                family.Get()->GetFamilyNames(names.GetAddressOf()).ThrowOnError();
                result.Add(new SystemFontFamilyId(GetLocalizedName(names)));
            }
            catch
            {
            }
        }

        return result;
    }

    /// <summary>Lists the faces of an installed font family.</summary>
    /// <param name="familyId">The family.</param>
    /// <returns>The faces that are not simulated, by weight, stretch and then slant.</returns>
    /// <exception cref="FileNotFoundException">If the family is not installed.</exception>
    /// <remarks><see cref="SystemFontFaceId"/> identifies faces by family and style, so faces sharing a style are
    /// deduplicated to whichever face DirectWrite selects for that style.</remarks>
    public static List<IFontFaceId> ListFaces(SystemFontFamilyId familyId)
    {
        using var family = FindFamily(familyId.Name.English);
        var fontCount = (int)family.Get()->GetFontCount();
        var fonts = new List<IFontFaceId>(fontCount);
        var styles = new HashSet<FontStyle>(fontCount);
        for (var i = 0; i < fontCount; i++)
        {
            using var font = default(ComPtr<IDWriteFont>);
            if (family.Get()->GetFont((uint)i, font.GetAddressOf()).FAILED ||
                font.Get()->GetSimulations() != DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_NONE)
            {
                continue;
            }

            var weight = font.Get()->GetWeight();
            var stretch = font.Get()->GetStretch();
            var slant = font.Get()->GetStyle();
            FontStyle style = new((int)weight, slant != DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL, (int)stretch);
            if (styles.Contains(style))
                continue;

            // Windows' pick for the style is expected not to be simulated, as this face has the style. The request
            // matches CreateFace, which asks for italic for any slant, so that the listed face is the one the id loads.
            using var picked = default(ComPtr<IDWriteFont>);
            var requested = style.Italicized ? DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_ITALIC : slant;
            var listed = family.Get()->GetFirstMatchingFont(weight, stretch, requested, picked.GetAddressOf())
                             .SUCCEEDED &&
                         picked.Get()->GetSimulations() == DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_NONE
                             ? picked.Get()
                             : font.Get();
            using var names = default(ComPtr<IDWriteLocalizedStrings>);
            if (listed->GetFaceNames(names.GetAddressOf()).FAILED)
                continue;
            var face = new SystemFontFaceId(familyId, style, listed->IsSymbolFont(), GetLocalizedName(names));
            fonts.Add(face);
            styles.Add(face.Style);
        }

        fonts.Sort(CompareFaces);
        return fonts;

        static int CompareFaces(IFontFaceId a, IFontFaceId b)
        {
            var comp = a.Style.Weight.CompareTo(b.Style.Weight);
            if (comp == 0)
                comp = a.Style.Stretch.CompareTo(b.Style.Stretch);
            return comp != 0 ? comp : a.Style.Italicized.CompareTo(b.Style.Italicized);
        }
    }

    /// <summary>Finds the file of the installed face closest to a font id.</summary>
    /// <param name="id">The font id.</param>
    /// <returns>The path of the file, the index of the face in it, and the axis values of a named instance of a
    /// variable font that differ from its defaults.</returns>
    /// <exception cref="FileNotFoundException">If the family is not installed.</exception>
    /// <exception cref="NotSupportedException">If the face is not backed by a single file on disk.</exception>
    public static (string Path, int Index, ImmutableArray<FontAxis> Axes) GetFileAndIndex(SystemFontFaceId id)
    {
        using var face = CreateFace(id);
        return (GetFilePath(face.Get()) ?? throw new NotSupportedException(),
                (int)face.Get()->GetIndex(),
                DirectWrite.DirectWriteFontFace.GetAxes(face.Get()));
    }

    /// <summary>Makes the DirectWrite face of the installed face closest to a font id.</summary>
    /// <param name="id">The font id.</param>
    /// <returns>The face, which DirectWrite maps from the file and shares. A named instance of a variable font keeps
    /// its axis values, and a style the family lacks is simulated.</returns>
    /// <exception cref="FileNotFoundException">If the family is not installed.</exception>
    public static ComPtr<IDWriteFontFace> CreateFace(SystemFontFaceId id)
    {
        using var family = FindFamily(id.Family.Name.English);
        using var font = default(ComPtr<IDWriteFont>);
        family.Get()->GetFirstMatchingFont(
            (DWRITE_FONT_WEIGHT)Math.Min(id.Style.Weight, 999),
            (DWRITE_FONT_STRETCH)id.Style.Stretch,
            id.Style.Italicized
                ? DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_ITALIC
                : DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL,
            font.GetAddressOf()).ThrowOnError();

        var face = default(ComPtr<IDWriteFontFace>);
        font.Get()->CreateFontFace(face.GetAddressOf()).ThrowOnError();
        return face;
    }

    /// <summary>Gets the path of the file of a font face.</summary>
    /// <param name="face">The face.</param>
    /// <returns>The path, or null if the face is not backed by a single file on disk.</returns>
    public static string? GetFilePath(IDWriteFontFace* face)
    {
        var fileCount = 0u;
        if (face->GetFiles(&fileCount, null).FAILED || fileCount != 1)
            return null;

        using var file = default(ComPtr<IDWriteFontFile>);
        using var loader = default(ComPtr<IDWriteFontFileLoader>);
        using var localLoader = default(ComPtr<IDWriteLocalFontFileLoader>);
        void* refKey;
        var refKeySize = 0u;
        var pathSize = 0u;
        if (face->GetFiles(&fileCount, file.GetAddressOf()).FAILED ||
            file.Get()->GetReferenceKey(&refKey, &refKeySize).FAILED ||
            file.Get()->GetLoader(loader.GetAddressOf()).FAILED ||
            loader.As(&localLoader).FAILED ||
            localLoader.Get()->GetFilePathLengthFromKey(refKey, refKeySize, &pathSize).FAILED)
        {
            return null;
        }

        // Paths may be long, up to 32767 characters.
        var buffer = pathSize < 260 ? stackalloc char[(int)pathSize + 1] : new char[pathSize + 1];
        fixed (char* path = buffer)
        {
            return localLoader.Get()->GetFilePathFromKey(refKey, refKeySize, path, pathSize + 1).SUCCEEDED
                       ? new(path, 0, (int)pathSize)
                       : null;
        }
    }

    /// <summary>Reads ClearType, contrast, and subpixel geometry settings for a monitor.</summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="orientation">Its <see cref="DEVMODEW.dmDisplayOrientation"/>.</param>
    /// <returns>The settings.</returns>
    /// <seealso cref="MonitorCache"/>
    public static SubpixelSettings.MonitorSettings ReadMonitorSettings(HMONITOR monitor, uint orientation)
    {
        BOOL smoothing = false;
        uint type = 0;
        var clearType = SystemParametersInfoW(SPI.SPI_GETFONTSMOOTHING, 0, &smoothing, 0) && smoothing &&
                        SystemParametersInfoW(SPI.SPI_GETFONTSMOOTHINGTYPE, 0, &type, 0) &&
                        type == FE_FONTSMOOTHINGCLEARTYPE;

        using var rendering = default(ComPtr<IDWriteRenderingParams>);
        using var rendering1 = default(ComPtr<IDWriteRenderingParams1>);
        Factory->CreateMonitorRenderingParams(monitor, rendering.GetAddressOf()).ThrowOnError();
        var p = rendering.Get();
        var contrast = p->GetEnhancedContrast();
        var grayscaleContrast = rendering.As(&rendering1).SUCCEEDED
                                    ? rendering1.Get()->GetGrayscaleEnhancedContrast()
                                    : contrast;
        var level = p->GetClearTypeLevel();
        var geometry = p->GetPixelGeometry();
        var bgr = geometry == DWRITE_PIXEL_GEOMETRY.DWRITE_PIXEL_GEOMETRY_BGR;

        // ClearType reads the geometry as if the monitor were not rotated. A quarter turn makes the stripes
        // vertical, which it cannot draw for, and half a turn reverses them.
        var subpixel = clearType && level > 0 && geometry != DWRITE_PIXEL_GEOMETRY.DWRITE_PIXEL_GEOMETRY_FLAT &&
                       orientation is DMDO_DEFAULT or DMDO_180;
        return new(
            SubpixelSettings.GetGammaRatios(p->GetGamma()),
            contrast,
            grayscaleContrast,
            level,
            !subpixel ? SubpixelSettings.SubpixelGeometry.None
            : bgr != (orientation == DMDO_180) ? SubpixelSettings.SubpixelGeometry.Bgr
            : SubpixelSettings.SubpixelGeometry.Rgb);
    }

    /// <summary>Tests whether a font family is installed.</summary>
    /// <param name="name">The family name, in any language the font has it in.</param>
    /// <returns>Whether it is. False if DirectWrite fails to tell.</returns>
    public static bool IsFamilyInstalled(string name)
    {
        if (!TryFindFamily(name, out var family))
            return false;

        family.Dispose();
        return true;
    }

    /// <summary>Gets the localized names of an installed font family.</summary>
    /// <param name="name">The family name, in any language the font has it in.</param>
    /// <param name="localized">Receives the canonical family names, when found.</param>
    /// <returns>Whether an installed family with that name was found.</returns>
    /// <remarks>Results are kept until the installed fonts are refreshed, as plugins may ask for the same name every
    /// frame.</remarks>
    public static bool TryGetFamilyName(string name, [NotNullWhen(true)] out LocalizedName? localized)
    {
        localized = null;
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var generation = Volatile.Read(ref familyNameGeneration);
        if (!FamilyNames.TryGetValue(name, out var cached) || cached.Generation != generation)
        {
            cached = new(generation, LookUpFamilyName(name));
            if (FamilyNames.Count >= MaxCachedFamilyNames)
                FamilyNames.Clear();
            FamilyNames[name] = cached;
        }

        localized = cached.Name;
        return localized is not null;
    }

    /// <summary>Gets the English name from a DirectWrite localized string table.</summary>
    /// <param name="names">The localized strings.</param>
    /// <returns>The <c>en-us</c> entry, falling back to <c>en</c>, the first entry, or empty.</returns>
    public static string GetEnglishName(IDWriteLocalizedStrings* names)
    {
        var index = 0u;
        BOOL exists = false;
        fixed (char* enUs = "en-us")
        {
            fixed (char* en = "en")
            {
                if ((names->FindLocaleName(enUs, &index, &exists).FAILED || !exists) &&
                    (names->FindLocaleName(en, &index, &exists).FAILED || !exists))
                {
                    index = 0;
                }
            }
        }

        uint length;
        if (names->GetStringLength(index, &length).FAILED)
            return string.Empty;
        var buffer = new char[length + 1];
        fixed (char* p = buffer)
            return names->GetString(index, p, length + 1).SUCCEEDED ? new(p, 0, (int)length) : string.Empty;
    }

    /// <summary>Finds a family of the system font collection.</summary>
    /// <param name="name">The family name, in any language the font has it in.</param>
    /// <param name="family">Receives the family, which the caller owns, when found.</param>
    /// <returns>Whether it was found.</returns>
    public static bool TryFindFamily(string name, out ComPtr<IDWriteFontFamily> family)
    {
        family = default;
        using var collection = default(ComPtr<IDWriteFontCollection>);
        if (Factory->GetSystemFontCollection(collection.GetAddressOf(), false).FAILED)
            return false;

        var index = 0u;
        BOOL exists = false;
        fixed (char* p = name)
        {
            if (collection.Get()->FindFamilyName(p, &index, &exists).FAILED || !exists)
                return false;
        }

        return collection.Get()->GetFontFamily(index, family.GetAddressOf()).SUCCEEDED;
    }

    private static LocalizedName? LookUpFamilyName(string name)
    {
        if (!TryFindFamily(name, out var found))
            return null;

        using var family = found;
        using var names = default(ComPtr<IDWriteLocalizedStrings>);
        if (family.Get()->GetFamilyNames(names.GetAddressOf()).FAILED)
            return null;

        var candidate = GetLocalizedName(names.Get());
        return string.IsNullOrEmpty(candidate.English) ? null : candidate;
    }

    // Localized names by lowercase locale code, and the English one.
    private static LocalizedName GetLocalizedName(IDWriteLocalizedStrings* names)
    {
        var count = names->GetCount();
        var maxStrLen = 0u;
        for (var i = 0u; i < count; i++)
        {
            var length = 0u;
            names->GetStringLength(i, &length).ThrowOnError();
            maxStrLen = Math.Max(maxStrLen, length);
            names->GetLocaleNameLength(i, &length).ThrowOnError();
            maxStrLen = Math.Max(maxStrLen, length);
        }

        // Guard stackalloc against large string lengths declared in untrusted font files.
        maxStrLen++;
        var buffer = maxStrLen <= 256 ? stackalloc char[(int)maxStrLen] : new char[maxStrLen];
        var byLocale = new Dictionary<string, string>((int)count);
        string? enUs = null, en = null, first = null;
        fixed (char* buf = buffer)
        {
            for (var i = 0u; i < count; i++)
            {
                names->GetLocaleName(i, buf, maxStrLen).ThrowOnError();
                var key = new string(buf).ToLowerInvariant();
                names->GetString(i, buf, maxStrLen).ThrowOnError();
                var name = new string(buf);
                byLocale[key] = name;
                first ??= name;
                if (key == "en-us")
                    enUs ??= name;
                else if (key == "en")
                    en ??= name;
            }
        }

        return new(enUs ?? en ?? first ?? string.Empty, byLocale);
    }

    private static ComPtr<IDWriteFontFamily> FindFamily(string name) =>
        TryFindFamily(name, out var family) ? family : throw new FileNotFoundException($"Font \"{name}\" not found.");

    // Isolated, as a shared factory remaps the cache of the Windows font cache service as new sizes are shaped, which
    // stalled the calling thread about 35 ms per remap in the game. Font files, faces and collections given to it must
    // be made by it too.
    private static ComPtr<IDWriteFactory5> CreateFactory()
    {
        var factory = default(ComPtr<IDWriteFactory5>);
        DirectX.DWriteCreateFactory(
            DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_ISOLATED,
            __uuidof<IDWriteFactory5>(),
            (IUnknown**)factory.GetAddressOf()).ThrowOnError();

        return factory;
    }

    // Cached lookup result, null if uninstalled, tagged with the refresh generation.
    private readonly record struct FamilyNameEntry(int Generation, LocalizedName? Name);
}
