using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Utility;

using ImGuiNET;

using Newtonsoft.Json;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents a font installed in the system.
/// </summary>
public sealed class SystemFontId : IFontId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemFontId"/> class.
    /// </summary>
    /// <param name="family">The parent font family.</param>
    /// <param name="font">The font.</param>
    internal unsafe SystemFontId(SystemFontFamilyId family, ComPtr<IDWriteFont> font)
    {
        this.Family = family;
        this.Weight = (int)font.Get()->GetWeight();
        this.Stretch = (int)font.Get()->GetStretch();
        this.Style = (int)font.Get()->GetStyle();

        using var fn = default(ComPtr<IDWriteLocalizedStrings>);
        font.Get()->GetFaceNames(fn.GetAddressOf()).ThrowOnError();
        this.LocaleNames = IObjectWithLocalizableName.GetLocaleNames(fn);
        if (this.LocaleNames.TryGetValue("en-us", out var name))
            this.EnglishName = name;
        else if (this.LocaleNames.TryGetValue("en", out name))
            this.EnglishName = name;
        else 
            this.EnglishName = this.LocaleNames.Values.First();
    }

    [JsonConstructor]
    private SystemFontId(string englishName, IReadOnlyDictionary<string, string> localeNames, IFontFamilyId family)
    {
        this.EnglishName = englishName;
        this.LocaleNames = localeNames;
        this.Family = family;
    }

    /// <inheritdoc/>
    [JsonProperty]
    public string EnglishName { get; init; }

    /// <inheritdoc/>
    [JsonProperty]
    public IReadOnlyDictionary<string, string>? LocaleNames { get; }

    /// <inheritdoc/>
    [JsonProperty]
    public IFontFamilyId Family { get; init; }

    /// <inheritdoc/>
    [JsonProperty]
    public int Weight { get; init; } = (int)DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL;

    /// <inheritdoc/>
    [JsonProperty]
    public int Stretch { get; init; } = (int)DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL;

    /// <inheritdoc/>
    [JsonProperty]
    public int Style { get; init; } = (int)DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL;

    public static bool operator ==(SystemFontId? left, SystemFontId? right) => Equals(left, right);

    public static bool operator !=(SystemFontId? left, SystemFontId? right) => !Equals(left, right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is SystemFontId other && this.Equals(other));

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(this.Family, this.Weight, this.Stretch, this.Style);

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(SystemFontId)}:{this.Weight}:{this.Stretch}:{this.Style}:{this.Family}";

    /// <inheritdoc/>
    public ImFontPtr AddToBuildToolkit(IFontAtlasBuildToolkitPreBuild tk, in SafeFontConfig config)
    {
        var (path, index) = this.GetFileAndIndex();
        return tk.AddFontFromFile(path, config with { FontNo = index });
    }

    /// <summary>
    /// Gets the file containing this font, and the font index within.
    /// </summary>
    /// <returns>The path and index.</returns>
    public unsafe (string Path, int Index) GetFileAndIndex()
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
        fixed (void* name = this.Family.EnglishName)
            sfc.Get()->FindFamilyName((ushort*)name, &familyIndex, &exists).ThrowOnError();
        if (!exists)
            throw new FileNotFoundException($"Font \"{this.Family.EnglishName}\" not found.");

        using var family = default(ComPtr<IDWriteFontFamily>);
        sfc.Get()->GetFontFamily(familyIndex, family.GetAddressOf()).ThrowOnError();

        using var font = default(ComPtr<IDWriteFont>);
        family.Get()->GetFirstMatchingFont(
            (DWRITE_FONT_WEIGHT)this.Weight,
            (DWRITE_FONT_STRETCH)this.Stretch,
            (DWRITE_FONT_STYLE)this.Style,
            font.GetAddressOf()).ThrowOnError();

        using var fface = default(ComPtr<IDWriteFontFace>);
        font.Get()->CreateFontFace(fface.GetAddressOf()).ThrowOnError();
        var fileCount = 0;
        fface.Get()->GetFiles((uint*)&fileCount, null).ThrowOnError();
        if (fileCount != 1)
            throw new NotSupportedException();

        using var ffile = default(ComPtr<IDWriteFontFile>);
        fface.Get()->GetFiles((uint*)&fileCount, ffile.GetAddressOf()).ThrowOnError();
        void* refKey;
        var refKeySize = 0u;
        ffile.Get()->GetReferenceKey(&refKey, &refKeySize).ThrowOnError();

        using var floader = default(ComPtr<IDWriteFontFileLoader>);
        ffile.Get()->GetLoader(floader.GetAddressOf()).ThrowOnError();

        using var flocal = default(ComPtr<IDWriteLocalFontFileLoader>);
        floader.As(&flocal).ThrowOnError();

        var pathSize = 0u;
        flocal.Get()->GetFilePathLengthFromKey(refKey, refKeySize, &pathSize).ThrowOnError();

        var path = stackalloc char[(int)pathSize + 1];
        flocal.Get()->GetFilePathFromKey(refKey, refKeySize, (ushort*)path, pathSize + 1).ThrowOnError();
        return (new(path, 0, (int)pathSize), (int)fface.Get()->GetIndex());
    }

    private bool Equals(SystemFontId other) => this.Family.Equals(other.Family) && this.Weight == other.Weight &&
                                               this.Stretch == other.Stretch && this.Style == other.Style;
}
