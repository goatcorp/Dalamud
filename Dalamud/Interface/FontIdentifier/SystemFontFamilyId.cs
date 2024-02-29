using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dalamud.Utility;

using Newtonsoft.Json;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents a font from system.
/// </summary>
public sealed class SystemFontFamilyId : IFontFamilyId
{
    [JsonIgnore]
    private IReadOnlyList<IFontId>? fontsLazy;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemFontFamilyId"/> class.
    /// </summary>
    /// <param name="englishName">The font name in English.</param>
    /// <param name="localeNames">The localized font name for display purposes.</param>
    [JsonConstructor]
    internal SystemFontFamilyId(string englishName, IReadOnlyDictionary<string, string> localeNames)
    {
        this.EnglishName = englishName;
        this.LocaleNames = localeNames;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemFontFamilyId"/> class.
    /// </summary>
    /// <param name="localeNames">The localized font name for display purposes.</param>
    internal SystemFontFamilyId(IReadOnlyDictionary<string, string> localeNames)
    {
        if (localeNames.TryGetValue("en-us", out var name))
            this.EnglishName = name;
        else if (localeNames.TryGetValue("en", out name))
            this.EnglishName = name;
        else 
            this.EnglishName = localeNames.Values.First();
        this.LocaleNames = localeNames;
    }

    /// <inheritdoc/>
    [JsonProperty]
    public string EnglishName { get; init; }

    /// <inheritdoc/>
    [JsonProperty]
    public IReadOnlyDictionary<string, string>? LocaleNames { get; }

    /// <inheritdoc/>
    [JsonIgnore]
    public IReadOnlyList<IFontId> Fonts => this.fontsLazy ??= this.GetFonts();

    public static bool operator ==(SystemFontFamilyId? left, SystemFontFamilyId? right) => Equals(left, right);

    public static bool operator !=(SystemFontFamilyId? left, SystemFontFamilyId? right) => !Equals(left, right);

    /// <inheritdoc/>
    public int FindBestMatch(int weight, int stretch, int style)
    {
        using var matchingFont = default(ComPtr<IDWriteFont>);

        var candidates = this.Fonts.ToList();
        var minGap = int.MaxValue;
        foreach (var c in candidates)
            minGap = Math.Min(minGap, Math.Abs(c.Weight - weight));
        candidates.RemoveAll(c => Math.Abs(c.Weight - weight) != minGap);

        minGap = int.MaxValue;
        foreach (var c in candidates)
            minGap = Math.Min(minGap, Math.Abs(c.Stretch - stretch));
        candidates.RemoveAll(c => Math.Abs(c.Stretch - stretch) != minGap);

        if (candidates.Any(x => x.Style == style))
            candidates.RemoveAll(x => x.Style != style);
        else if (candidates.Any(x => x.Style == (int)DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL))
            candidates.RemoveAll(x => x.Style != (int)DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL);

        if (!candidates.Any())
            return 0;

        for (var i = 0; i < this.Fonts.Count; i++)
        {
            if (Equals(this.Fonts[i], candidates[0]))
                return i;
        }

        return 0;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(SystemFontFamilyId)}:{this.EnglishName}";

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is SystemFontFamilyId other && this.Equals(other));

    /// <inheritdoc/>
    public override int GetHashCode() => this.EnglishName.GetHashCode();

    /// <summary>
    /// Create a new instance of <see cref="SystemFontFamilyId"/> from an <see cref="IDWriteFontFamily"/>.
    /// </summary>
    /// <param name="family">The family.</param>
    /// <returns>The new instance.</returns>
    internal static unsafe SystemFontFamilyId FromDWriteFamily(ComPtr<IDWriteFontFamily> family)
    {
        using var fn = default(ComPtr<IDWriteLocalizedStrings>);
        family.Get()->GetFamilyNames(fn.GetAddressOf()).ThrowOnError();
        return new(IObjectWithLocalizableName.GetLocaleNames(fn));
    }

    private unsafe IReadOnlyList<IFontId> GetFonts()
    {
        using var dwf = default(ComPtr<IDWriteFactory>);
        fixed (Guid* piid = &IID.IID_IDWriteFactory)
        {
            DirectX.DWriteCreateFactory(
                DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED,
                piid,
                (IUnknown**)dwf.GetAddressOf()).ThrowOnError();
        }

        using var sfc = default(ComPtr<IDWriteFontCollection>);
        dwf.Get()->GetSystemFontCollection(sfc.GetAddressOf(), false).ThrowOnError();

        var familyIndex = 0u;
        BOOL exists = false;
        fixed (void* pName = this.EnglishName)
            sfc.Get()->FindFamilyName((ushort*)pName, &familyIndex, &exists).ThrowOnError();
        if (!exists)
            throw new FileNotFoundException($"Font \"{this.EnglishName}\" not found.");

        using var family = default(ComPtr<IDWriteFontFamily>);
        sfc.Get()->GetFontFamily(familyIndex, family.GetAddressOf()).ThrowOnError();

        var fontCount = (int)family.Get()->GetFontCount();
        var fonts = new List<IFontId>(fontCount);
        for (var i = 0; i < fontCount; i++)
        {
            using var font = default(ComPtr<IDWriteFont>);
            if (family.Get()->GetFont((uint)i, font.GetAddressOf()).FAILED)
            {
                // Ignore errors, if any
                continue;
            }

            if (font.Get()->GetSimulations() != DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_NONE)
            {
                // No simulation support
                continue;
            }

            fonts.Add(new SystemFontId(this, font));
        }

        fonts.Sort(
            (a, b) =>
            {
                var comp = a.Weight.CompareTo(b.Weight);
                if (comp != 0)
                    return comp;

                comp = a.Stretch.CompareTo(b.Stretch);
                if (comp != 0)
                    return comp;

                return a.Style.CompareTo(b.Style);
            });
        return fonts;
    }

    private bool Equals(SystemFontFamilyId other) => this.EnglishName == other.EnglishName;
}
