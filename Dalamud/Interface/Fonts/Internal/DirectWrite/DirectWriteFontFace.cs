using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>One face of an OpenType/TrueType file, with its design metrics and glyph images. The
/// <see cref="DirectWriteFont"/>s of all locales and slants of the face share it.</summary>
/// <remarks><ul>
/// <li>The size is the line height from the hhea table, line gap included, as with the game's fonts. Fonts that keep
/// their spacing in the line gap (AXIS Basic ProN) are then as large as those that keep it in the ascent and descent
/// (Noto Sans CJK).</li>
/// <li>The glyph images are main thread only. The rest may be used on any thread.</li>
/// </ul></remarks>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "QI")]
internal sealed unsafe partial class DirectWriteFontFace
{
    private static readonly Lazy<ComPtr<IDWriteInMemoryFontFileLoader>> LoaderLazy =
        new(CreateLoader, LazyThreadSafetyMode.ExecutionAndPublication);

    // The slants the image keys tell apart, by their id. Fonts are made on any thread.
    private readonly List<float> slants = [0f];
    private readonly Lock slantsLock = new();

    private float designAscent;
    private float designDescent;
    private float designLineGap;
    private float unitsPerEm;
    private float underlinePosition;
    private float underlineThickness;
    private float strikethroughPosition;
    private float strikethroughThickness;
    private IDWriteFontFace* face;

    /// <summary>Initializes a new instance of the <see cref="DirectWriteFontFace"/> class.</summary>
    /// <param name="name">Name for debugging.</param>
    /// <param name="data">The font file, copied.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    /// <param name="colorGlyphs">Draws color glyphs and outlines too large for DirectWrite, or null to draw the
    /// outlines of color glyphs and no too large ones.</param>
    /// <param name="deferral">Rasterizes glyphs beyond the frame's budget, or null to rasterize all of them at once.
    /// </param>
    /// <param name="owner">The sources whose face cache has this face, or null.</param>
    /// <param name="key">The key of this face in the face cache of <paramref name="owner"/>, or null.</param>
    /// <seealso cref="CreateDeferral"/>
    public DirectWriteFontFace(
        string name, ReadOnlySpan<byte> data, int fontNo, ColorGlyphRasterizer? colorGlyphs = null,
        ConcurrentGlyphQueue<GlyphRasterRequest>? deferral = null, DirectWriteFontSources? owner = null,
        FaceKey? key = null)
    {
        this.Name = name;
        this.ColorGlyphs = colorGlyphs;
        this.Deferral = deferral;
        this.Owner = owner;
        this.Key = key;
        try
        {
            var factory = DirectWriteUtilities.Factory;
            using var file = default(ComPtr<IDWriteFontFile>);

            fixed (byte* p = data)
            {
                LoaderLazy.Value.Get()->CreateInMemoryFontFileReference(
                    (IDWriteFactory*)factory,
                    p,
                    (uint)data.Length,
                    null,
                    file.GetAddressOf()).ThrowOnError();
            }

            BOOL supported;
            DWRITE_FONT_FILE_TYPE fileType;
            DWRITE_FONT_FACE_TYPE faceType;
            uint faceCount;
            file.Get()->Analyze(&supported, &fileType, &faceType, &faceCount).ThrowOnError();
            if (!supported)
                throw new NotSupportedException($"{name}: not a font file DirectWrite supports.");
            if (fontNo >= faceCount)
                throw new ArgumentOutOfRangeException(nameof(fontNo), fontNo, $"{name} has {faceCount} faces.");

            fixed (IDWriteFontFace** pFace = &this.face)
            {
                var pFile = file.Get();
                factory->CreateFontFace(
                        faceType,
                        1,
                        &pFile,
                        (uint)fontNo,
                        DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_NONE,
                        pFace)
                    .ThrowOnError();
            }

            this.ReadMetrics();
        }
        catch
        {
            this.ReleaseResources();
            throw;
        }
    }

    /// <summary>Initializes a new instance of the <see cref="DirectWriteFontFace"/> class.</summary>
    /// <param name="name">Name for debugging.</param>
    /// <param name="from">A face of the file, such as one of the system fonts.</param>
    /// <param name="simulations">What DirectWrite is to simulate.</param>
    /// <param name="colorGlyphs">Draws color glyphs and outlines too large for DirectWrite, or null to draw the
    /// outlines of color glyphs and no too large ones.</param>
    /// <param name="deferral">Rasterizes glyphs beyond the frame's budget, or null to rasterize all of them at once.
    /// </param>
    /// <param name="owner">The sources whose face cache has this face, or null.</param>
    /// <param name="key">The key of this face in the face cache of <paramref name="owner"/>, or null.</param>
    /// <remarks>The new instance adds a reference to <paramref name="from"/> if it has the same simulations.</remarks>
    public DirectWriteFontFace(
        string name, IDWriteFontFace* from, DWRITE_FONT_SIMULATIONS simulations,
        ColorGlyphRasterizer? colorGlyphs = null, ConcurrentGlyphQueue<GlyphRasterRequest>? deferral = null,
        DirectWriteFontSources? owner = null, FaceKey? key = null)
    {
        this.Name = name;
        this.ColorGlyphs = colorGlyphs;
        this.Deferral = deferral;
        this.Owner = owner;
        this.Key = key;
        try
        {
            if (from->GetSimulations() == simulations)
            {
                this.face = from;
                from->AddRef();
            }
            else
            {
                fixed (IDWriteFontFace** pFace = &this.face)
                    CreateFace(from, simulations, pFace).ThrowOnError();
            }

            this.ReadMetrics();
        }
        catch
        {
            this.ReleaseResources();
            throw;
        }
    }

    /// <summary>Finalizes an instance of the <see cref="DirectWriteFontFace"/> class.</summary>
    ~DirectWriteFontFace() => this.ReleaseResources();

    /// <summary>Gets the name, for debugging.</summary>
    public string Name { get; }

    /// <summary>Gets the sources whose face cache has this face, or null if none has it.</summary>
    /// <remarks>The faces of other styles are taken from there too.</remarks>
    public DirectWriteFontSources? Owner { get; }

    /// <summary>Gets the key of this face in the face cache of <see cref="Owner"/>, or null if it is not cached.
    /// </summary>
    public FaceKey? Key { get; }

    /// <summary>Gets the DirectWrite face.</summary>
    public IDWriteFontFace* Native => this.face;

    /// <summary>Gets a value indicating whether the face has color glyphs.</summary>
    public bool HasColorGlyphs { get; private set; }

    // The rasterizer of color glyphs and outlines too large for DirectWrite, or null.
    private ColorGlyphRasterizer? ColorGlyphs { get; }

    // The queue that rasterizes glyphs beyond the frame's budget, or null.
    private ConcurrentGlyphQueue<GlyphRasterRequest>? Deferral { get; }

    // What DirectWrite simulates.
    private DWRITE_FONT_SIMULATIONS Simulations => this.face->GetSimulations();

    /// <summary>Gets the metrics at a size, as designed.</summary>
    /// <param name="size">Size in pixels.</param>
    /// <returns>The metrics.</returns>
    public FontMetrics GetMetrics(float size)
    {
        var scale = this.Scale(size);
        return new(
            MathF.Round(this.designAscent * scale),
            MathF.Round(this.designDescent * scale),
            size, // The line gap takes up the rounding.
            Math.Max(1f, MathF.Round(this.underlinePosition * scale)),
            Math.Max(1f, MathF.Round(this.underlineThickness * scale)),
            MathF.Round(this.strikethroughPosition * scale),
            Math.Max(1f, MathF.Round(this.strikethroughThickness * scale)));
    }

    /// <summary>Gets the pixels per design unit at a size.</summary>
    /// <param name="size">Size in pixels. See the class remarks.</param>
    /// <returns>The scale.</returns>
    public float Scale(float size) => size / (this.designAscent + this.designDescent + this.designLineGap);

    /// <summary>Gets the em size at a size.</summary>
    /// <param name="size">Size in pixels. See the class remarks.</param>
    /// <returns>The em size in pixels.</returns>
    public float EmSize(float size) => this.Scale(size) * this.unitsPerEm;

    /// <summary>Gets the id of a slant in the image keys. Called when a font is made.</summary>
    /// <param name="slant">Horizontal shift per pixel above the baseline.</param>
    /// <returns>The id. 0 is no slant.</returns>
    /// <exception cref="NotSupportedException">If the face has <see cref="MaxSlants"/> slants already.</exception>
    public int SlantIdOf(float slant)
    {
        lock (this.slantsLock)
        {
            var id = this.slants.IndexOf(slant);
            if (id >= 0)
                return id;
            if (this.slants.Count >= MaxSlants)
                throw new NotSupportedException($"{this.Name} has {MaxSlants} slants already.");
            this.slants.Add(slant);
            return this.slants.Count - 1;
        }
    }

    /// <summary>Gets the face to draw a style with.</summary>
    /// <param name="key">The face key of the style.</param>
    /// <param name="italic">Whether this face is already slanted for italic.</param>
    /// <param name="slanted">Whether the outlines are to be slanted for italic, as no italic face was found.</param>
    /// <returns>The face, from the face cache of <see cref="Owner"/> if there is one.</returns>
    /// <remarks>Takes the faces of the installed family of the same name if it has them, and has DirectWrite simulate
    /// bold otherwise. A face with simulated bold counts as weight 700 or more, and keeps it while the style is bold
    /// enough.</remarks>
    public DirectWriteFontFace GetStyled(in TextStyle key, bool italic, out bool slanted)
    {
        var own = this.StyleOf();
        var bold = (this.Simulations & DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_BOLD) != 0;
        var target = key.On(
            own with
            {
                Weight = bold ? Math.Max(own.EffectiveWeight, 700) : own.Weight,
                Italicized = own.Italicized || italic,
            });
        var sims = (target.EffectiveWeight >= 600 && (bold || own.EffectiveWeight < 600)
                        ? DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_BOLD
                        : 0) |
                   (target.Italicized && !own.Italicized && !italic
                        ? DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_OBLIQUE
                        : 0);
        using var installed = this.FindInstalledFace(target, ref sims);

        // The game slants italic text by an angle of its own. DirectWrite's oblique has another.
        slanted = (sims & DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_OBLIQUE) != 0;
        sims &= ~DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_OBLIQUE;
        return installed.Get() is null
                   ? this.WithSimulations(sims)
                   : this.Find($"{this.Name} {target}", installed.Get(), sims, null);
    }

    // The same face as another, with simulations. An instance of a variable font keeps its axis values.
    private static HRESULT CreateFace(IDWriteFontFace* from, DWRITE_FONT_SIMULATIONS sims, IDWriteFontFace** face)
    {
        using var resource = default(ComPtr<IDWriteFontResource>);
        var axes = AxisValuesOf(from, resource.GetAddressOf());
        if (axes.Length > 0)
        {
            using var face5 = default(ComPtr<IDWriteFontFace5>);
            fixed (DWRITE_FONT_AXIS_VALUE* values = axes)
            {
                var made = resource.Get()->CreateFontFace(sims, values, (uint)axes.Length, face5.GetAddressOf());
                if (made.FAILED)
                    return made;
            }

            *face = (IDWriteFontFace*)face5.Detach();
            return S.S_OK;
        }

        uint fileCount = 0;
        var hr = from->GetFiles(&fileCount, null);
        if (hr.FAILED || fileCount == 0)
            return hr.FAILED ? hr : E.E_FAIL;
        // A face is one file, or a few for formats such as Type 1.
        var buffer = fileCount <= 4 ? stackalloc nint[(int)fileCount] : new nint[fileCount];
        fixed (nint* p = buffer)
        {
            var files = (IDWriteFontFile**)p;
            hr = from->GetFiles(&fileCount, files);
            if (hr.FAILED)
                return hr;
            hr = DirectWriteUtilities.Factory->CreateFontFace(
                from->GetType(),
                fileCount,
                files,
                from->GetIndex(),
                sims,
                face);
            for (var i = 0; i < fileCount; i++)
                files[i]->Release();
            return hr;
        }
    }

    // The axis values of an instance of a variable font, with the resource to make other faces of the font from.
    // Empty for other fonts, and before Windows 10 1803, which has no interface for them.
    private static DWRITE_FONT_AXIS_VALUE[] AxisValuesOf(IDWriteFontFace* face, IDWriteFontResource** resource)
    {
        using var face5 = default(ComPtr<IDWriteFontFace5>);
        fixed (Guid* iid = &IID.IID_IDWriteFontFace5)
        {
            if (face->QueryInterface(iid, (void**)face5.GetAddressOf()).FAILED || !face5.Get()->HasVariations())
                return [];
        }

        var axes = new DWRITE_FONT_AXIS_VALUE[face5.Get()->GetFontAxisValueCount()];
        fixed (DWRITE_FONT_AXIS_VALUE* p = axes)
        {
            if (axes.Length == 0 || face5.Get()->GetFontAxisValues(p, (uint)axes.Length).FAILED ||
                face5.Get()->GetFontResource(resource).FAILED)
            {
                return [];
            }
        }

        return axes;
    }

    // The axis values of an instance of a variable font that differ from the font's defaults, as text. Empty for the
    // default instance and other fonts.
    private static string AxisKeyOf(IDWriteFontFace* face)
    {
        using var resource = default(ComPtr<IDWriteFontResource>);
        var axes = AxisValuesOf(face, resource.GetAddressOf());
        if (axes.Length == 0)
            return string.Empty;
        var defaults = new DWRITE_FONT_AXIS_VALUE[resource.Get()->GetFontAxisCount()];
        fixed (DWRITE_FONT_AXIS_VALUE* p = defaults)
        {
            if (resource.Get()->GetDefaultFontAxisValues(p, (uint)defaults.Length).FAILED)
                defaults = [];
        }

        var key = new StringBuilder();
        foreach (var axis in axes)
        {
            var isDefault = false;
            foreach (var d in defaults)
                isDefault |= d.axisTag == axis.axisTag && d.value == axis.value;
            if (isDefault)
                continue;
            var tag = (uint)axis.axisTag;
            key.Append(key.Length > 0 ? " " : null).Append((char)(tag & 0xFF)).Append((char)((tag >> 8) & 0xFF))
               .Append((char)((tag >> 16) & 0xFF)).Append((char)(tag >> 24))
               .Append('=').Append(axis.value.ToString(CultureInfo.InvariantCulture));
        }

        return key.ToString();
    }

    private static ComPtr<IDWriteInMemoryFontFileLoader> CreateLoader()
    {
        var loader = default(ComPtr<IDWriteInMemoryFontFileLoader>);
        var factory = DirectWriteUtilities.Factory;
        factory->CreateInMemoryFontFileLoader(loader.GetAddressOf()).ThrowOnError();
        factory->RegisterFontFileLoader((IDWriteFontFileLoader*)loader.Get()).ThrowOnError();
        return loader;
    }

    // The same face with other simulations, from the face cache of Owner if there is one. This one if the simulations
    // are the same.
    private DirectWriteFontFace WithSimulations(DWRITE_FONT_SIMULATIONS simulations) =>
        simulations == this.Simulations
            ? this
            : this.Find(
                $"{this.Name} {simulations}",
                this.face,
                simulations,
                this.Key is { } key ? key with { Simulations = simulations } : null);

    // A face of the file of another, from the owner's face cache if there is one. A null key takes the key from the
    // file.
    private DirectWriteFontFace Find(string name, IDWriteFontFace* from, DWRITE_FONT_SIMULATIONS sims, FaceKey? key) =>
        this.Owner?.GetFace(name, from, sims, key) ??
        new DirectWriteFontFace(name, from, sims, this.ColorGlyphs, this.Deferral);

    // The installed face of the family of the same name for a style, if it is a real one, with the simulations it does
    // away with cleared. Null if the family is not installed, or has other glyphs than this face.
    private ComPtr<IDWriteFontFace> FindInstalledFace(FontStyle target, ref DWRITE_FONT_SIMULATIONS sims)
    {
        var result = default(ComPtr<IDWriteFontFace>);
        using var face3 = default(ComPtr<IDWriteFontFace3>);
        using var names = default(ComPtr<IDWriteLocalizedStrings>);
        using var collection = default(ComPtr<IDWriteFontCollection>);
        using var family = default(ComPtr<IDWriteFontFamily>);
        using var regular = default(ComPtr<IDWriteFont>);
        using var regularFace = default(ComPtr<IDWriteFontFace>);
        using var font = default(ComPtr<IDWriteFont>);

        // For the platform analyzer.
        if (!OperatingSystem.IsWindowsVersionAtLeast(10))
            return result;
        fixed (Guid* iid = &IID.IID_IDWriteFontFace3)
        {
            if (this.face->QueryInterface(iid, (void**)face3.GetAddressOf()).FAILED)
                return result;
        }

        if (face3.Get()->GetFamilyNames(names.GetAddressOf()).FAILED ||
            DirectWriteUtilities.Factory->GetSystemFontCollection(collection.GetAddressOf(), false).FAILED)
        {
            return result;
        }

        fixed (char* name = SystemFallbackFont.EnglishNameOf(names.Get()))
        {
            uint index;
            BOOL exists;
            if (collection.Get()->FindFamilyName(name, &index, &exists).FAILED || !exists ||
                collection.Get()->GetFontFamily(index, family.GetAddressOf()).FAILED)
            {
                return result;
            }
        }

        var weight = face3.Get()->GetWeight();
        var stretch = face3.Get()->GetStretch();
        var style = face3.Get()->GetStyle();
        if (family.Get()->GetFirstMatchingFont(weight, stretch, style, regular.GetAddressOf()).FAILED ||
            regular.Get()->CreateFontFace(regularFace.GetAddressOf()).FAILED ||
            regularFace.Get()->GetGlyphCount() != this.face->GetGlyphCount())
        {
            return result;
        }

        if (family.Get()->GetFirstMatchingFont(
                (DWRITE_FONT_WEIGHT)Math.Min(target.EffectiveWeight, 999),
                (DWRITE_FONT_STRETCH)target.EffectiveStretch,
                target.Italicized ? DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_ITALIC : style,
                font.GetAddressOf()).FAILED)
        {
            return result;
        }

        var found = font.Get()->GetSimulations();
        var realWeight = font.Get()->GetWeight() != weight &&
                         (found & DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_BOLD) == 0;
        var realStretch = font.Get()->GetStretch() != stretch;
        var realItalic = target.Italicized && style == DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL &&
                         font.Get()->GetStyle() != DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL &&
                         (found & DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_OBLIQUE) == 0;
        if ((!realWeight && !realStretch && !realItalic) || font.Get()->CreateFontFace(result.GetAddressOf()).FAILED)
            return result;
        if (realWeight && (int)font.Get()->GetWeight() >= 600)
            sims &= ~DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_BOLD;
        if (realItalic)
            sims &= ~DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_OBLIQUE;
        return result;
    }

    private FontStyle StyleOf()
    {
        using var face3 = default(ComPtr<IDWriteFontFace3>);
        fixed (Guid* iid = &IID.IID_IDWriteFontFace3)
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10) ||
                this.face->QueryInterface(iid, (void**)face3.GetAddressOf()).FAILED)
            {
                return default;
            }
        }

        return new(
            (int)face3.Get()->GetWeight(),
            face3.Get()->GetStyle() != DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL,
            (int)face3.Get()->GetStretch());
    }

    private void ReadMetrics()
    {
        this.HasColorGlyphs = ColorGlyphRasterizer.HasColorGlyphs(this.face);

        DWRITE_FONT_METRICS fm;
        this.face->GetMetrics(&fm);
        this.unitsPerEm = fm.designUnitsPerEm;
        this.underlinePosition = -fm.underlinePosition;
        this.underlineThickness = fm.underlineThickness;
        this.strikethroughThickness = fm.strikethroughThickness > 0 ? fm.strikethroughThickness : fm.underlineThickness;
        this.strikethroughPosition = fm.strikethroughPosition > 0
                                         ? fm.strikethroughPosition
                                         : (fm.designUnitsPerEm * 0.28f) + (this.strikethroughThickness * 0.5f);

        // Measured with hhea, as stb_truetype (and so Dear ImGui) does. DirectWrite reports the OS/2 metrics.
        void* hhea = null;
        uint hheaLength = 0;
        void* context = null;
        BOOL exists = false;
        if (this.face->TryGetFontTable(
                BinaryPrimitives.ReverseEndianness(0x68686561u /* hhea */), &hhea, &hheaLength, &context, &exists)
            .SUCCEEDED && exists)
        {
            if (hheaLength >= 10)
            {
                var span = new ReadOnlySpan<byte>(hhea, (int)hheaLength);
                this.designAscent = BinaryPrimitives.ReadInt16BigEndian(span[4..]);
                this.designDescent = -BinaryPrimitives.ReadInt16BigEndian(span[6..]);
                this.designLineGap = BinaryPrimitives.ReadInt16BigEndian(span[8..]);
            }

            this.face->ReleaseFontTable(context);
        }

        if (this.designAscent + this.designDescent <= 0)
        {
            this.designAscent = fm.ascent;
            this.designDescent = fm.descent;
            this.designLineGap = fm.lineGap;
        }

        this.designLineGap = Math.Max(this.designLineGap, 0);
        this.bitmapEms = this.FindBitmapEms();
    }

    private void ReleaseResources()
    {
        if (this.face is not null)
        {
            this.face->Release();
            this.face = null;
        }
    }

    /// <summary>What tells faces apart in the face cache of <see cref="DirectWriteFontSources"/>.</summary>
    /// <param name="File">The lowercased full path of the file. A stream or asset is "stream:" or "asset:" and its
    /// id.</param>
    /// <param name="FontNo">Index of the face in a collection.</param>
    /// <param name="Simulations">What DirectWrite simulates.</param>
    /// <param name="Axes">The axis values of an instance of a variable font that differ from the font's defaults,
    /// such as "wght=700". Empty for the default instance and other fonts.</param>
    public readonly record struct FaceKey(
        string File, int FontNo, DWRITE_FONT_SIMULATIONS Simulations, string Axes = "")
    {
        /// <summary>Makes the key of the default instance of a face of a file on disk.</summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="fontNo">Index of the face in a collection.</param>
        /// <param name="simulations">What DirectWrite simulates.</param>
        /// <returns>The key.</returns>
        public static FaceKey OfFile(
            string path, int fontNo,
            DWRITE_FONT_SIMULATIONS simulations = DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_NONE) =>
            new(Path.GetFullPath(path).ToLowerInvariant(), fontNo, simulations);

        /// <summary>Makes the key of a DirectWrite face from its file and its axis values.</summary>
        /// <param name="face">The face.</param>
        /// <param name="simulations">What DirectWrite simulates.</param>
        /// <returns>The key, or null if the face is not of one file on disk.</returns>
        public static FaceKey? Of(IDWriteFontFace* face, DWRITE_FONT_SIMULATIONS simulations) =>
            DirectWriteUtilities.GetFilePath(face) is { } path
                ? OfFile(path, (int)face->GetIndex(), simulations) with { Axes = AxisKeyOf(face) }
                : null;
    }
}
