using System.Collections.Generic;

using Dalamud.Utility;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents an object with localizable names.
/// </summary>
public interface IObjectWithLocalizableName
{
    /// <summary>
    /// Gets the name, preferrably in English.
    /// </summary>
    string EnglishName { get; }

    /// <summary>
    /// Gets the names per locales.
    /// </summary>
    IReadOnlyDictionary<string, string>? LocaleNames { get; }

    /// <summary>
    /// Gets the name in the requested locale if available; otherwise, <see cref="EnglishName"/>.
    /// </summary>
    /// <param name="localeCode">The locale code. Must be in lowercase(invariant).</param>
    /// <returns>The value.</returns>
    string GetLocalizedName(string localeCode)
    {
        if (this.LocaleNames is null)
            return this.EnglishName;
        if (this.LocaleNames.TryGetValue(localeCode, out var v))
            return v;
        foreach (var (a, b) in this.LocaleNames)
        {
            if (a.StartsWith(localeCode))
                return b;
        }

        return this.EnglishName;
    }

    /// <summary>
    /// Resolves all names per locales.
    /// </summary>
    /// <param name="fn">The names.</param>
    /// <returns>A new dictionary mapping from locale code to localized names.</returns>
    internal static unsafe IReadOnlyDictionary<string, string> GetLocaleNames(IDWriteLocalizedStrings* fn)
    {
        var count = fn->GetCount();
        var maxStrLen = 0u;
        for (var i = 0u; i < count; i++)
        {
            var length = 0u;
            fn->GetStringLength(i, &length).ThrowOnError();
            maxStrLen = Math.Max(maxStrLen, length);
            fn->GetLocaleNameLength(i, &length).ThrowOnError();
            maxStrLen = Math.Max(maxStrLen, length);
        }

        maxStrLen++;
        var buf = stackalloc char[(int)maxStrLen];
        var result = new Dictionary<string, string>((int)count);
        for (var i = 0u; i < count; i++)
        {
            fn->GetLocaleName(i, (ushort*)buf, maxStrLen).ThrowOnError();
            var key = new string(buf);
            fn->GetString(i, (ushort*)buf, maxStrLen).ThrowOnError();
            var value = new string(buf);
            result[key.ToLowerInvariant()] = value;
        }

        return result;
    }
}
