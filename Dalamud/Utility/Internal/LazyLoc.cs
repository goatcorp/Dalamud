using CheapLoc;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Utility.Internal;

/// <summary>
/// Represents a lazily localized string, consisting of a localization key and a fallback value.
/// </summary>
/// <param name="Key">The localization key used to retrieve the localized value.</param>
/// <param name="Fallback">The fallback text to use if the localization key is not found.</param>
internal readonly record struct LazyLoc(string Key, string Fallback)
{
    public static implicit operator ImU8String(LazyLoc locRef)
        => new(locRef.ToString());

    /// <summary>
    /// Creates a new instance of <see cref="LazyLoc"/> with the specified localization key and fallback text.
    /// </summary>
    /// <param name="key">The localization key used to retrieve the localized value.</param>
    /// <param name="fallback">The fallback text to use if the localization key is not found.</param>
    /// <returns>A <see cref="LazyLoc"/> instance representing the localized value.</returns>
    public static LazyLoc Localize(string key, string fallback)
        => new(key, fallback);

    /// <inheritdoc/>
    public override string ToString()
    {
        return Loc.Localize(this.Key, this.Fallback);
    }
}
