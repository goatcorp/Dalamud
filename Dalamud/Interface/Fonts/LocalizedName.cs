using System.Collections.Generic;
using System.Linq;

namespace Dalamud.Interface.Fonts;

/// <summary>A name with its translations, such as of a font.</summary>
/// <param name="English">The name in English, or the only name there is.</param>
/// <param name="ByLocale">The names by lowercase locale code, such as <c>ko-kr</c>, or null.</param>
public sealed record LocalizedName(string English, IReadOnlyDictionary<string, string>? ByLocale = null)
{
    // The last name found, as lists ask for the name of each row in every frame.
    private Lookup? last;

    /// <summary>Gets the name in a locale.</summary>
    /// <param name="localeCode">The locale, such as "en-US", or a language, such as "en".</param>
    /// <returns>The name of the locale, else of the first locale of its language, else <see cref="English"/>.
    /// </returns>
    public string Get(string? localeCode)
    {
        if (this.ByLocale is null || string.IsNullOrEmpty(localeCode))
            return this.English;
        if (this.last is { } last && string.Equals(last.LocaleCode, localeCode, StringComparison.Ordinal))
            return last.Name;

        var name = this.Find(localeCode);
        this.last = new(localeCode, name);
        return name;
    }

    /// <summary>Compares the names and their translations by value.</summary>
    /// <param name="other">The other name.</param>
    /// <returns>Whether they are equal.</returns>
    public bool Equals(LocalizedName? other) =>
        other is not null &&
        this.English == other.English &&
        (ReferenceEquals(this.ByLocale, other.ByLocale) ||
         (this.ByLocale is not null && other.ByLocale is not null && this.ByLocale.Count == other.ByLocale.Count &&
          this.ByLocale.All(x => other.ByLocale.TryGetValue(x.Key, out var v) && v == x.Value)));

    /// <inheritdoc/>
    public override int GetHashCode() => this.English.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => this.English;

    private string Find(string localeCode)
    {
        if (this.ByLocale!.TryGetValue(localeCode, out var exact))
            return exact;

        var dash = localeCode.IndexOf('-');
        var language = dash < 0 ? localeCode.AsSpan() : localeCode.AsSpan(0, dash);
        string? sameLanguage = null;
        foreach (var (key, name) in this.ByLocale)
        {
            if (key.Equals(localeCode, StringComparison.OrdinalIgnoreCase))
                return name;
            if (sameLanguage is null
                && key.AsSpan().StartsWith(language, StringComparison.OrdinalIgnoreCase)
                && (key.Length == language.Length || key[language.Length] == '-'))
            {
                sameLanguage = name;
            }
        }

        return sameLanguage ?? this.English;
    }

    private sealed record Lookup(string LocaleCode, string Name);
}
