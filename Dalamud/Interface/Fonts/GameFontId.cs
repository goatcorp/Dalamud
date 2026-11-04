using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>
/// Represents a font from the game.
/// </summary>
public sealed class GameFontId : SingleFontId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameFontId"/> class.
    /// </summary>
    /// <param name="gameFontFamily">The game font family.</param>
    [JsonConstructor]
    internal GameFontId(GameFontFamily gameFontFamily) => this.GameFontFamily = gameFontFamily;

    /// <summary>
    /// Gets the game font family.
    /// </summary>
    [JsonProperty]
    public GameFontFamily GameFontFamily { get; init; }

    /// <inheritdoc/>
    [JsonIgnore]
    [field: JsonIgnore]
    public override LocalizedName Name =>
        field ??= new(
            Enum.GetName(this.GameFontFamily) is { } name ? $"Game: {name}" : $"Game: {(int)this.GameFontFamily}");

    /// <inheritdoc/>
    private protected override bool PreviewsName =>
        this.GameFontFamily is not (GameFontFamily.Meidinger or GameFontFamily.MiedingerMid
                                    or GameFontFamily.JupiterNumeric);

    /// <inheritdoc/>
    private protected override string ShortName => $"{this.GameFontFamily}";

    public static bool operator ==(GameFontId? left, GameFontId? right) => Equals(left, right);

    public static bool operator !=(GameFontId? left, GameFontId? right) => !Equals(left, right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is GameFontId other && this.Equals(other));

    /// <inheritdoc/>
    public override int GetHashCode() => (int)this.GameFontFamily;

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(GameFontId)}:{this.GameFontFamily}";

    private bool Equals(GameFontId other) => this.GameFontFamily == other.GameFontFamily;
}
