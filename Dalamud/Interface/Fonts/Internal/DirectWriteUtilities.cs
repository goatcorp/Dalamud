using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private static readonly Lazy<ComPtr<IDWriteFactory5>> FactoryLazy =
        new(CreateFactory, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<ComPtr<IDWriteRenderingParams>> RenderingParamsLazy =
        new(
            static () =>
            {
                var p = default(ComPtr<IDWriteRenderingParams>);
                Factory->CreateRenderingParams(p.GetAddressOf()).ThrowOnError();
                return p;
            },
            LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Gets the shared DirectWrite factory.</summary>
    public static IDWriteFactory5* Factory => FactoryLazy.Value.Get();

    /// <summary>Gets the rendering parameters of the primary monitor, to pick rendering modes with.</summary>
    public static IDWriteRenderingParams* RenderingParams => RenderingParamsLazy.Value.Get();

    /// <summary>Lists the installed font families.</summary>
    /// <param name="refresh">Whether to look for changes to the installed fonts.</param>
    /// <returns>The families. Those that fail to load are left out.</returns>
    public static List<IFontFamilyId> ListFamilies(bool refresh)
    {
        using var collection = default(ComPtr<IDWriteFontCollection>);
        Factory->GetSystemFontCollection(collection.GetAddressOf(), refresh).ThrowOnError();

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
                // A family that cannot be read is not listed.
            }
        }

        return result;
    }

    /// <summary>Lists the faces of an installed font family.</summary>
    /// <param name="familyId">The family.</param>
    /// <returns>The faces that are not simulated, by weight, stretch and then slant.</returns>
    /// <exception cref="FileNotFoundException">If the family is not installed.</exception>
    public static List<IFontFaceId> ListFaces(SystemFontFamilyId familyId)
    {
        using var family = FindFamily(familyId.Name.English);
        var fontCount = (int)family.Get()->GetFontCount();
        var fonts = new List<IFontFaceId>(fontCount);
        for (var i = 0; i < fontCount; i++)
        {
            using var font = default(ComPtr<IDWriteFont>);
            if (family.Get()->GetFont((uint)i, font.GetAddressOf()).FAILED)
                continue;

            if (font.Get()->GetSimulations() != DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_NONE)
                continue;

            FontStyle style = new(
                (int)font.Get()->GetWeight(),
                font.Get()->GetStyle() != DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL,
                (int)font.Get()->GetStretch());
            using var names = default(ComPtr<IDWriteLocalizedStrings>);
            if (fonts.Exists(x => x.Style == style) || font.Get()->GetFaceNames(names.GetAddressOf()).FAILED)
                continue;
            fonts.Add(new SystemFontFaceId(familyId, style, font.Get()->IsSymbolFont(), GetLocalizedName(names)));
        }

        fonts.Sort(
            (a, b) =>
            {
                var comp = a.Style.Weight.CompareTo(b.Style.Weight);
                if (comp != 0)
                    return comp;

                comp = a.Style.Stretch.CompareTo(b.Style.Stretch);
                return comp != 0 ? comp : a.Style.Italicized.CompareTo(b.Style.Italicized);
            });
        return fonts;
    }

    /// <summary>Finds the file of the installed face closest to a font id.</summary>
    /// <param name="id">The font id.</param>
    /// <returns>The path of the file, and the index of the face in it.</returns>
    /// <exception cref="FileNotFoundException">If the family is not installed.</exception>
    /// <exception cref="NotSupportedException">If the face is not of one file on disk.</exception>
    public static (string Path, int Index) GetFileAndIndex(SystemFontFaceId id)
    {
        using var face = CreateFace(id);
        return (GetFilePath(face.Get()) ?? throw new NotSupportedException(), (int)face.Get()->GetIndex());
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
    /// <returns>The path, or null if the face is not of one file on disk.</returns>
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

    /// <summary>Reads how a monitor wants text drawn.</summary>
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
        return new(
            SubpixelSettings.GammaRatiosOf(p->GetGamma()),
            contrast,
            grayscaleContrast,
            level,
            clearType && level > 0 && geometry != DWRITE_PIXEL_GEOMETRY.DWRITE_PIXEL_GEOMETRY_FLAT &&
            orientation is DMDO_DEFAULT or DMDO_180,
            bgr != (orientation == DMDO_180));
    }

    /// <summary>Tests whether a font family is installed.</summary>
    /// <param name="name">The family name, in any language the font has it in.</param>
    /// <returns>Whether it is. False if DirectWrite fails to tell.</returns>
    public static bool IsFamilyInstalled(string name)
    {
        using var collection = default(ComPtr<IDWriteFontCollection>);
        if (Factory->GetSystemFontCollection(collection.GetAddressOf(), false).FAILED)
            return false;

        var index = 0u;
        BOOL exists = false;
        fixed (char* p = name)
            return collection.Get()->FindFamilyName(p, &index, &exists).SUCCEEDED && exists;
    }

    // Localized names by lowercase locale code. The English one is of en-us, else of en, else the first, else empty.
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

        // The lengths come from the font file.
        maxStrLen++;
        var buffer = maxStrLen <= 256 ? stackalloc char[(int)maxStrLen] : new char[maxStrLen];
        var byLocale = new Dictionary<string, string>((int)count);
        fixed (char* buf = buffer)
        {
            for (var i = 0u; i < count; i++)
            {
                names->GetLocaleName(i, buf, maxStrLen).ThrowOnError();
                var key = new string(buf);
                names->GetString(i, buf, maxStrLen).ThrowOnError();
                byLocale[key.ToLowerInvariant()] = new(buf);
            }
        }

        if (!byLocale.TryGetValue("en-us", out var english) && !byLocale.TryGetValue("en", out english))
            english = byLocale.Values.FirstOrDefault() ?? string.Empty;
        return new(english, byLocale);
    }

    private static ComPtr<IDWriteFontFamily> FindFamily(string name)
    {
        using var collection = default(ComPtr<IDWriteFontCollection>);
        Factory->GetSystemFontCollection(collection.GetAddressOf(), false).ThrowOnError();

        var index = 0u;
        BOOL exists = false;
        fixed (char* p = name)
            collection.Get()->FindFamilyName(p, &index, &exists).ThrowOnError();
        if (!exists)
            throw new FileNotFoundException($"Font \"{name}\" not found.");

        var family = default(ComPtr<IDWriteFontFamily>);
        collection.Get()->GetFontFamily(index, family.GetAddressOf()).ThrowOnError();
        return family;
    }

    private static ComPtr<IDWriteFactory5> CreateFactory()
    {
        var factory = default(ComPtr<IDWriteFactory5>);
        fixed (Guid* iid = &IID.IID_IDWriteFactory5)
        {
            DirectX.DWriteCreateFactory(
                DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED,
                iid,
                (IUnknown**)factory.GetAddressOf()).ThrowOnError();
        }

        return factory;
    }
}
