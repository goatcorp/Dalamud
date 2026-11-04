using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>
/// Represents the default Dalamud font.
/// </summary>
public sealed class DalamudDefaultFontId : SingleFontId
{
    /// <summary>
    /// The shared instance of <see cref="DalamudDefaultFontId"/>.
    /// </summary>
    public static readonly DalamudDefaultFontId Instance = new();

    private DalamudDefaultFontId()
    {
    }

    /// <inheritdoc/>
    [JsonIgnore]
    public override LocalizedName Name { get; } = new("(Default)");

    /// <inheritdoc/>
    private protected override bool PreviewsName => false;

    /// <inheritdoc/>
    private protected override string ShortName => "Default";

    public static bool operator ==(DalamudDefaultFontId? left, DalamudDefaultFontId? right) =>
        left is null == right is null;

    public static bool operator !=(DalamudDefaultFontId? left, DalamudDefaultFontId? right) =>
        left is null != right is null;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DalamudDefaultFontId;

    /// <inheritdoc/>
    public override int GetHashCode() => 12345678;

    /// <inheritdoc/>
    public override string ToString() => nameof(DalamudDefaultFontId);
}
