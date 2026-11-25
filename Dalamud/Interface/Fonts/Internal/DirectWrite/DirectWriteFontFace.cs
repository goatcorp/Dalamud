using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>An OpenType or TrueType font face shared across <see cref="DirectWriteFont"/> instances of any locale or
/// slant.</summary>
/// <remarks><ul>
/// <li>Layout size equals the <c>hhea</c> line height including line gap, matching game fonts so line-gap-spaced fonts
/// (AXIS Basic ProN) match ascent/descent-spaced ones (Noto Sans CJK).</li>
/// <li><see cref="GetImages"/> and <see cref="AddRasterized"/> are main-thread only. Metrics and
/// <see cref="Rasterize(in GlyphRasterRequest)"/> are thread-safe.</li>
/// </ul></remarks>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "QI")]
internal sealed unsafe partial class DirectWriteFontFace
{
    private static readonly ModuleLog Log = ModuleLog.Create<DirectWriteFontFace>();

    // Distinct slant values indexed by slantId for glyph cache keys, guarded by slantsLock for off-thread font creation.
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
    /// <param name="open">Opens the font stream. Called only if the file for <paramref name="key"/> is not already in
    /// memory.</param>
    /// <param name="key">The key of this face, in the face cache of <paramref name="owner"/> if there is one.</param>
    /// <param name="colorGlyphs">Draws color glyphs and outlines too large for DirectWrite.</param>
    /// <param name="deferral">Rasterizes glyphs beyond the frame's budget.</param>
    /// <param name="owner">The sources whose face cache has this face.</param>
    /// <seealso cref="ConcurrentGlyphQueue"/>
    public DirectWriteFontFace(
        string name, Func<Stream> open, FaceKey key, ColorGlyphRasterizer colorGlyphs, ConcurrentGlyphQueue deferral,
        DirectWriteFontSources owner)
        : this(name, colorGlyphs, deferral, owner, key, open, null, default)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DirectWriteFontFace"/> class.</summary>
    /// <param name="name">Name for debugging.</param>
    /// <param name="from">A face of the file, such as one of the system fonts.</param>
    /// <param name="simulations">What DirectWrite is to simulate.</param>
    /// <param name="colorGlyphs">Draws color glyphs and outlines too large for DirectWrite.</param>
    /// <param name="deferral">Rasterizes glyphs beyond the frame's budget.</param>
    /// <param name="owner">The sources whose face cache has this face.</param>
    /// <param name="key">The key of this face in the face cache of <paramref name="owner"/>, or null.</param>
    /// <remarks>The new instance adds a reference to <paramref name="from"/> if it has the same simulations.</remarks>
    public DirectWriteFontFace(
        string name, IDWriteFontFace* from, DWRITE_FONT_SIMULATIONS simulations, ColorGlyphRasterizer colorGlyphs,
        ConcurrentGlyphQueue deferral, DirectWriteFontSources owner, FaceKey? key)
        : this(name, colorGlyphs, deferral, owner, key, null, from, simulations)
    {
    }

    private DirectWriteFontFace(
        string name, ColorGlyphRasterizer colorGlyphs, ConcurrentGlyphQueue deferral, DirectWriteFontSources owner,
        FaceKey? key, Func<Stream>? open, IDWriteFontFace* from, DWRITE_FONT_SIMULATIONS simulations)
    {
        this.Name = name;
        this.ColorGlyphs = colorGlyphs;
        this.Deferral = deferral;
        this.Owner = owner;
        this.Key = key;
        try
        {
            if (open is not null)
            {
                var factory = DirectWriteUtilities.Factory;
                var fontNo = key!.Value.FontNo;
                this.memoryFile = InMemoryFile.Acquire(key.Value.File, open);
                var file = this.memoryFile.File;

                BOOL supported;
                DWRITE_FONT_FILE_TYPE fileType;
                DWRITE_FONT_FACE_TYPE faceType;
                uint faceCount;
                file->Analyze(&supported, &fileType, &faceType, &faceCount).ThrowOnError();
                if (!supported)
                    throw new NotSupportedException($"{name}: not a font file DirectWrite supports.");
                if (fontNo >= faceCount)
                    throw new ArgumentOutOfRangeException(nameof(key), fontNo, $"{name} has {faceCount} faces.");

                fixed (IDWriteFontFace** pFace = &this.face)
                {
                    factory->CreateFontFace(
                            faceType,
                            1,
                            &file,
                            (uint)fontNo,
                            DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_NONE,
                            pFace)
                        .ThrowOnError();
                }
            }
            else
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

                this.memoryFile = InMemoryFile.Find(this.face);
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

    /// <summary>Gets the DirectWrite face.</summary>
    public IDWriteFontFace* Native => this.face;

    /// <summary>Gets a value indicating whether the face has color glyphs.</summary>
    public bool HasColorGlyphs { get; private set; }

    private DirectWriteFontSources Owner { get; }

    private FaceKey? Key { get; }

    private ColorGlyphRasterizer ColorGlyphs { get; }

    private ConcurrentGlyphQueue Deferral { get; }

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

    /// <summary>Gets or assigns the 6-bit cache-key ID for <paramref name="slant"/>.</summary>
    /// <param name="slant">Horizontal shift per pixel above the baseline.</param>
    /// <returns>The slant ID (<c>0</c> for no slant).</returns>
    /// <exception cref="NotSupportedException">If the face has <see cref="MaxSlants"/> slants already.</exception>
    public int GetOrAddSlantId(float slant)
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
    /// <param name="key">The target text style.</param>
    /// <param name="italic">Whether the caller already applies synthetic italic slant.</param>
    /// <param name="slanted">Whether the caller should apply synthetic italic slant because no real italic face was
    /// found.</param>
    /// <param name="italicFace">Whether the returned face switched from upright to a real italic or slanted face,
    /// replacing any synthetic slant on the caller.</param>
    /// <returns>The resolved face from <see cref="DirectWriteFontSources"/>'s cache.</returns>
    /// <remarks><ul>
    /// <li>Variable fonts adjust their <c>wght</c>, <c>wdth</c>, <c>ital</c>, and <c>slnt</c> axes.</li>
    /// <li>Static fonts, and variable fonts lacking an italic or slant axis, look up matching faces in the installed
    /// family of the same name.</li>
    /// <li>Otherwise DirectWrite bold simulation is applied for weights of 600 or above. A face that already has
    /// simulated bold counts as weight 700 or above.</li>
    /// </ul></remarks>
    public DirectWriteFontFace GetStyled(in TextStyle key, bool italic, out bool slanted, out bool italicFace)
    {
        var own = this.GetStyle();
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
        var installedSims = sims;
        // Prefer adjusting this variable font's axes over a named installed face, which would drop custom axis values.
        italicFace = false;
        using var variable = this.CreateVariableInstance(target, own, ref sims, ref italicFace);
        var installedItalic = false;
        using var installed = variable.Get() is null || (target.Italicized && !own.Italicized && !italicFace)
                                  ? this.FindInstalledFace(target, own, ref installedSims, out installedItalic)
                                  : default;

        // The game slants italic text by an angle of its own. DirectWrite's oblique has another.
        if (installed.Get() is not null && (variable.Get() is null || installedItalic))
        {
            italicFace = installedItalic;
            slanted = TakeOblique(ref installedSims);
            return this.Find($"{this.Name} {target}", installed.Get(), installedSims, null);
        }

        slanted = TakeOblique(ref sims);
        if (variable.Get() is null)
            return this.WithSimulations(sims);

        var instance = (IDWriteFontFace*)variable.Get();
        return this.Find(
            $"{this.Name} {target}",
            instance,
            instance->GetSimulations(),
            this.Key is { } k ? k with { Simulations = instance->GetSimulations(), Axes = GetAxisKey(instance) } : null);
    }

    /// <summary>Gets an instance of this variable font at axis values.</summary>
    /// <param name="axes">The values. Axes it does not have are left out.</param>
    /// <returns>The face, from the face cache of the sources that own this face. This face if it is not
    /// variable, or the values are empty.</returns>
    public DirectWriteFontFace WithAxes(ImmutableArray<FontAxis> axes)
    {
        if (axes.IsDefaultOrEmpty)
            return this;
        using var resource = default(ComPtr<IDWriteFontResource>);
        var values = GetAxisValues(this.face, resource.GetAddressOf());
        if (values.Length == 0)
            return this;

        foreach (var axis in axes)
        {
            for (var i = 0; i < values.Length; i++)
            {
                if ((uint)values[i].axisTag == axis.Tag)
                    values[i].value = axis.Value;
            }
        }

        using var instance = default(ComPtr<IDWriteFontFace5>);
        fixed (DWRITE_FONT_AXIS_VALUE* p = values)
        {
            resource.Get()->CreateFontFace(this.Simulations, p, (uint)values.Length, instance.GetAddressOf())
                    .ThrowOnError();
        }

        var native = (IDWriteFontFace*)instance.Get();
        var key = GetAxisKey(native);
        return this.Find(
            $"{this.Name} {key}",
            native,
            this.Simulations,
            this.Key is { } k ? k with { Axes = key } : null);
    }

    /// <summary>Clears <see cref="DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_OBLIQUE"/> from
    /// <paramref name="sims"/> because italic uses the game's slant angle instead.</summary>
    /// <param name="sims">The simulations. Receives them without the oblique simulation.</param>
    /// <returns>Whether the oblique simulation was in them.</returns>
    internal static bool TakeOblique(ref DWRITE_FONT_SIMULATIONS sims)
    {
        var oblique = (sims & DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_OBLIQUE) != 0;
        sims &= ~DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_OBLIQUE;
        return oblique;
    }

    /// <summary>Gets the axis values of an instance of a variable font that differ from the font's defaults.</summary>
    /// <param name="face">The face.</param>
    /// <returns>The values in the order of the font, or empty for the default instance and other fonts.</returns>
    internal static ImmutableArray<FontAxis> GetAxes(IDWriteFontFace* face)
    {
        using var resource = default(ComPtr<IDWriteFontResource>);
        var axes = GetAxisValues(face, resource.GetAddressOf());
        if (axes.Length == 0)
            return [];
        var defaults = new DWRITE_FONT_AXIS_VALUE[resource.Get()->GetFontAxisCount()];
        fixed (DWRITE_FONT_AXIS_VALUE* p = defaults)
        {
            if (resource.Get()->GetDefaultFontAxisValues(p, (uint)defaults.Length).FAILED)
                defaults = [];
        }

        var result = ImmutableArray.CreateBuilder<FontAxis>();
        foreach (var axis in axes)
        {
            var isDefault = false;
            foreach (var d in defaults)
                isDefault |= d.axisTag == axis.axisTag && d.value == axis.value;
            if (!isDefault)
                result.Add(new((uint)axis.axisTag, axis.value));
        }

        return result.ToImmutable();
    }

    private static string GetAxisKey(IDWriteFontFace* face) => string.Join(' ', GetAxes(face));

    // Clones a font face with new simulations, preserving variable-font axis values.
    private static HRESULT CreateFace(IDWriteFontFace* from, DWRITE_FONT_SIMULATIONS sims, IDWriteFontFace** face)
    {
        using var resource = default(ComPtr<IDWriteFontResource>);
        var axes = GetAxisValues(from, resource.GetAddressOf());
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

    // Returns axis values and font resource for a variable font, or empty for static fonts or pre-Windows 10 1803.
    private static DWRITE_FONT_AXIS_VALUE[] GetAxisValues(IDWriteFontFace* face, IDWriteFontResource** resource)
    {
        using var face5 = default(ComPtr<IDWriteFontFace5>);
        if (face->QueryInterface(__uuidof<IDWriteFontFace5>(), (void**)face5.GetAddressOf()).FAILED ||
            !face5.Get()->HasVariations())
        {
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

    // The wdth axis value of a stretch, as OpenType maps the usWidthClass of OS/2.
    private static float GetWidthAxisValue(int stretch) => stretch switch
    {
        <= 1 => 50,
        2 => 62.5f,
        3 => 75,
        4 => 87.5f,
        5 => 100,
        6 => 112.5f,
        7 => 125,
        8 => 150,
        _ => 200,
    };

    // Creates a variable-font instance applying target weight, stretch, and italic axes, clearing any simulations they
    // satisfy. Returns empty if no axis changes.
    private ComPtr<IDWriteFontFace5> CreateVariableInstance(
        FontStyle target, FontStyle own, ref DWRITE_FONT_SIMULATIONS sims, ref bool italicFace)
    {
        var result = default(ComPtr<IDWriteFontFace5>);
        using var resource = default(ComPtr<IDWriteFontResource>);
        var values = GetAxisValues(this.face, resource.GetAddressOf());
        if (values.Length == 0)
            return result;
        var ranges = new DWRITE_FONT_AXIS_RANGE[resource.Get()->GetFontAxisCount()];
        fixed (DWRITE_FONT_AXIS_RANGE* p = ranges)
        {
            if (resource.Get()->GetFontAxisRanges(p, (uint)ranges.Length).FAILED)
                return result;
        }

        var changed = false;
        var weighted = false;
        var italicized = false;
        var wantsItalic = target.Italicized && !own.Italicized;
        var hasItalAxis = false;
        foreach (var r in ranges)
            hasItalAxis |= r.axisTag == DWRITE_FONT_AXIS_TAG.DWRITE_FONT_AXIS_TAG_ITALIC && r.maxValue >= 1;
        for (var i = 0; i < values.Length; i++)
        {
            ref var v = ref values[i];
            var range = default(DWRITE_FONT_AXIS_RANGE);
            foreach (var r in ranges)
            {
                if (r.axisTag == v.axisTag)
                    range = r;
            }

            if (range.axisTag != v.axisTag)
                continue;
            float value;
            // Only modify axes whose style property changed so custom axis values are preserved.
            switch (v.axisTag)
            {
                case DWRITE_FONT_AXIS_TAG.DWRITE_FONT_AXIS_TAG_WEIGHT
                    when target.EffectiveWeight != own.EffectiveWeight:
                    value = Math.Clamp(target.EffectiveWeight, range.minValue, range.maxValue);
                    weighted = value >= 600;
                    break;
                case DWRITE_FONT_AXIS_TAG.DWRITE_FONT_AXIS_TAG_WIDTH
                    when target.EffectiveStretch != own.EffectiveStretch:
                    value = Math.Clamp(GetWidthAxisValue(target.EffectiveStretch), range.minValue, range.maxValue);
                    break;
                case DWRITE_FONT_AXIS_TAG.DWRITE_FONT_AXIS_TAG_ITALIC when wantsItalic && hasItalAxis:
                    value = 1;
                    italicized = true;
                    break;
                case DWRITE_FONT_AXIS_TAG.DWRITE_FONT_AXIS_TAG_SLANT when wantsItalic && !hasItalAxis && range.minValue < 0:
                    // Negative slants lean right, as italic does.
                    value = range.minValue;
                    italicized = true;
                    break;
                default:
                    continue;
            }

            changed |= value != v.value;
            v.value = value;
        }

        if (!changed)
            return result;
        var instanceSims = sims & ~DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_OBLIQUE;
        if (weighted)
            instanceSims &= ~DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_BOLD;
        fixed (DWRITE_FONT_AXIS_VALUE* p = values)
        {
            if (resource.Get()->CreateFontFace(instanceSims, p, (uint)values.Length, result.GetAddressOf()).FAILED)
            {
                result.Reset();
                return result;
            }
        }

        if (weighted)
            sims &= ~DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_BOLD;
        if (italicized)
        {
            sims &= ~DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_OBLIQUE;
            italicFace = true;
        }

        return result;
    }

    private DirectWriteFontFace WithSimulations(DWRITE_FONT_SIMULATIONS simulations) =>
        simulations == this.Simulations
            ? this
            : this.Find(
                $"{this.Name} {simulations}",
                this.face,
                simulations,
                this.Key is { } key ? key with { Simulations = simulations } : null);

    private DirectWriteFontFace Find(string name, IDWriteFontFace* from, DWRITE_FONT_SIMULATIONS sims, FaceKey? key) =>
        this.Owner.GetFace(name, from, sims, key);

    // Looks up a non-simulated matching face in the installed family of the same name (requiring matching glyph count)
    // and clears satisfied simulations.
    private ComPtr<IDWriteFontFace> FindInstalledFace(
        FontStyle target, FontStyle own, ref DWRITE_FONT_SIMULATIONS sims, out bool italicFace)
    {
        italicFace = false;
        var result = default(ComPtr<IDWriteFontFace>);
        using var face3 = default(ComPtr<IDWriteFontFace3>);
        using var names = default(ComPtr<IDWriteLocalizedStrings>);
        using var regular = default(ComPtr<IDWriteFont>);
        using var regularFace = default(ComPtr<IDWriteFontFace>);
        using var font = default(ComPtr<IDWriteFont>);
        if (this.face->QueryInterface(__uuidof<IDWriteFontFace3>(), (void**)face3.GetAddressOf()).FAILED)
            return result;

        if (face3.Get()->GetFamilyNames(names.GetAddressOf()).FAILED ||
            !DirectWriteUtilities.TryFindFamily(DirectWriteUtilities.GetEnglishName(names.Get()), out var installed))
        {
            return result;
        }

        using var family = installed;
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
        // Only accept a family match for properties that changed so custom intermediate faces stay intact.
        var realWeight = target.EffectiveWeight != own.EffectiveWeight && font.Get()->GetWeight() != weight &&
                         (found & DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_BOLD) == 0;
        var realStretch = target.EffectiveStretch != own.EffectiveStretch && font.Get()->GetStretch() != stretch;
        var realItalic = target.Italicized && style == DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL &&
                         font.Get()->GetStyle() != DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL &&
                         (found & DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_OBLIQUE) == 0;
        if ((!realWeight && !realStretch && !realItalic) || font.Get()->CreateFontFace(result.GetAddressOf()).FAILED)
            return result;
        if (realWeight && (int)font.Get()->GetWeight() >= 600)
            sims &= ~DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_BOLD;
        if (realItalic)
            sims &= ~DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_OBLIQUE;
        italicFace = realItalic;
        return result;
    }

    private FontStyle GetStyle()
    {
        using var face3 = default(ComPtr<IDWriteFontFace3>);
        if (this.face->QueryInterface(__uuidof<IDWriteFontFace3>(), (void**)face3.GetAddressOf()).FAILED)
            return default;

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

        // Read hhea metrics to match stb_truetype and ImGui, since DirectWrite's GetMetrics reports OS/2 metrics.
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

        this.memoryFile?.Release();
        this.memoryFile = null;
    }

    /// <summary>Cache key identifying a <see cref="DirectWriteFontFace"/> in <see cref="DirectWriteFontSources"/>.
    /// </summary>
    /// <param name="File">Lowercased full file path, or an asset, memory, or resource key for non-file sources.</param>
    /// <param name="FontNo">Index of the face in a font collection.</param>
    /// <param name="Simulations">DirectWrite font simulations.</param>
    /// <param name="Axes">Non-default variable-font axis values such as <c>"wght=700"</c>, or empty for default and
    /// static faces.</param>
    public readonly record struct FaceKey(
        string File, int FontNo, DWRITE_FONT_SIMULATIONS Simulations, string Axes = "")
    {
        /// <summary>Makes the key of the default instance of a face of a file on disk.</summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="fontNo">Index of the face in a collection.</param>
        /// <param name="simulations">DirectWrite font simulations.</param>
        /// <returns>The key.</returns>
        public static FaceKey OfFile(
            string path, int fontNo,
            DWRITE_FONT_SIMULATIONS simulations = DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_NONE) =>
            new(Path.GetFullPath(path).ToLowerInvariant(), fontNo, simulations);

        /// <summary>Makes the key of a DirectWrite face from its file and its axis values.</summary>
        /// <param name="face">The face.</param>
        /// <param name="simulations">DirectWrite font simulations.</param>
        /// <returns>The key, or <see langword="null"/> if the face is not backed by a single file on disk.</returns>
        public static FaceKey? Of(IDWriteFontFace* face, DWRITE_FONT_SIMULATIONS simulations) =>
            DirectWriteUtilities.GetFilePath(face) is { } path
                ? OfFile(path, (int)face->GetIndex(), simulations) with { Axes = GetAxisKey(face) }
                : null;
    }
}
