using System.Collections.Generic;

using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>Represents a font with a single face, which is both its own face and its own family.<br />
/// Not intended for plugins to implement.</summary>
public abstract class SingleFontId : IFontFamilyId, IFontFaceId
{
    /// <summary>Initializes a new instance of the <see cref="SingleFontId"/> class.</summary>
    private protected SingleFontId()
    {
    }

    /// <inheritdoc cref="IFontFamilyId.Name"/>
    [JsonIgnore]
    public abstract LocalizedName Name { get; }

    /// <inheritdoc/>
    /// <value>A list of this font alone.</value>
    [JsonIgnore]
    [field: JsonIgnore]
    public IReadOnlyList<IFontFaceId> Fonts => field ??= [this];

    /// <inheritdoc/>
    /// <value>This font.</value>
    [JsonIgnore]
    public IFontFamilyId Family => this;

    /// <inheritdoc/>
    /// <value><see cref="FontStyle.Default"/>.</value>
    [JsonIgnore]
    public FontStyle Style => FontStyle.Default;

    /// <inheritdoc/>
    bool IFontFaceId.PreviewsName => this.PreviewsName;

    /// <inheritdoc/>
    string IFontFamilyId.ShortName => this.ShortName;

    /// <inheritdoc cref="IFontFaceId.PreviewsName"/>
    private protected virtual bool PreviewsName => true;

    /// <inheritdoc cref="IFontFamilyId.ShortName"/>
    private protected abstract string ShortName { get; }

    /// <inheritdoc/>
    /// <returns>0, as there is one font.</returns>
    public int FindBestMatch(FontStyle style) => 0;
}
