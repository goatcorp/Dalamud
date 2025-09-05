using CheapLoc;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Utility.Internal;

/// <summary>
/// Represents a localization reference consisting of a key and a fallback text.
/// </summary>
/// <param name="Key">The localization key used to retrieve the localized text.</param>
/// <param name="Fallback">The fallback text to use if the localization key is not found.</param>
internal readonly record struct LocRef(string Key, string Fallback)
{
    public static implicit operator LocRef((string Key, string Fallback) tuple)
        => new(tuple.Key, tuple.Fallback);

    public static implicit operator ImU8String(LocRef locRef)
        => new(locRef.ToString());

    /// <inheritdoc/>
    public override string ToString()
    {
        return Loc.Localize(this.Key, this.Fallback);
    }
}
