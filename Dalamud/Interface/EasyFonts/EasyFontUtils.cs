using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Utility;
using SharpDX;

using SDXFactory = SharpDX.DirectWrite.Factory;
using SDXFont1 = SharpDX.DirectWrite.Font1;
using SDXFontSimulations = SharpDX.DirectWrite.FontSimulations;
using SDXUnicodeRange = SharpDX.DirectWrite.UnicodeRange;

namespace Dalamud.Interface.EasyFonts;

/// <summary>
/// Font utility functions.
/// </summary>
public static class EasyFontUtils
{
    /// <summary>
    /// Get the list of installed system fonts.
    /// </summary>
    /// <param name="preferredLanguageName">Preferred language name prefix, in "en-us" format.</param>
    /// <param name="nameSortComparison">Comparison method for names for sorting the result.</param>
    /// <param name="refreshSystem">Whether to refresh installed font lists.</param>
    /// <param name="excludeSimulated">Exclude simulated fonts.</param>
    /// <param name="requiredChars">Characters required for every font in the return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of found fonts.</returns>
    public static Task<(
        string Name,
        Dictionary<string, string> LocalizedNames,
        FontIdent[] Variants)[]> GetSystemFontsAsync(
        string? preferredLanguageName = null,
        StringComparison nameSortComparison = StringComparison.CurrentCultureIgnoreCase,
        bool refreshSystem = false,
        bool excludeSimulated = true,
        IEnumerable<char>? requiredChars = null,
        CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        var languageNamePrefixes = new[]
        {
            (preferredLanguageName ?? Service<DalamudConfiguration>.Get().EffectiveLanguage).ToLowerInvariant(),
            "en",
            string.Empty,
        };
        var requiredCharsArray = requiredChars?.ToArray() ?? Array.Empty<char>();
        var unicodeRanges = Array.Empty<SDXUnicodeRange>();

        using var factory = new SDXFactory();
        using var collection = factory.GetSystemFontCollection(refreshSystem);

        var result = new List<(
            string Name,
            Dictionary<string, string> LocalizedNames,
            FontIdent[] Variants)>(collection.FontFamilyCount);
        var tempLocalizedNames = new List<string>();
        var tempLanguageNames = new List<string>();
        var tempVariants = new List<FontIdent>();
        try
        {
            foreach (var familyIndex in Enumerable.Range(0, collection.FontFamilyCount))
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var family = collection.GetFontFamily(familyIndex);
                if (family.FontCount == 0)
                    continue;

                using var familyNames = family.FamilyNames;
                tempLocalizedNames.EnsureCapacity(familyNames.Count);
                tempLocalizedNames.Clear();
                tempLocalizedNames.AddRange(
                    Enumerable.Range(0, familyNames.Count)
                              .Select(x => familyNames.GetString(x)));
                tempLanguageNames.EnsureCapacity(familyNames.Count);
                tempLanguageNames.Clear();
                tempLanguageNames.AddRange(
                    Enumerable.Range(0, familyNames.Count)
                              .Select(x => familyNames.GetLocaleName(x).ToLowerInvariant()));

                languageNamePrefixes[0] = Service<DalamudConfiguration>.Get().EffectiveLanguage.ToLowerInvariant();
                string? name = null;
                foreach (var languageNamePrefix in languageNamePrefixes)
                {
                    var localeNameIndex = tempLanguageNames.IndexOf(x => x.StartsWith(languageNamePrefix));
                    if (localeNameIndex != -1)
                    {
                        name = tempLocalizedNames[localeNameIndex];
                        break;
                    }
                }

                if (name is null)
                    continue;

                tempVariants.Clear();
                tempVariants.EnsureCapacity(family.FontCount);
                foreach (var fontIndex in Enumerable.Range(0, family.FontCount))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using var font = family.GetFont(fontIndex);

                    // we can't handle faux italic/bold at the moment; skip them
                    if (font.Simulations != SDXFontSimulations.None && excludeSimulated)
                        continue;

                    // Wingdings and some symbol fonts fail because they do not have CMAP formats supported by
                    // stb_truetype; this is not a correct check but it still works
                    if (font.IsSymbolFont)
                        continue;

                    if (requiredCharsArray.Any())
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        if (!requiredCharsArray.Any(x => font.HasCharacter(x)))
                            continue;
                    }
                    else
                    {
                        // imgui crashes if the font does not result in any rendered glyphs.
                        // Need to check if it's the case.
                        // This interface is available starting from Platform Update for Windows 7.
                        using var font1 = font.QueryInterfaceOrNull<SDXFont1>();
                        if (font1 is not null)
                        {
                            var rc = 0;
                            try
                            {
                                font1.GetUnicodeRanges(unicodeRanges.Length, unicodeRanges, out rc);
                            }
                            catch (SharpDXException sdxe) when (sdxe.HResult == unchecked((int)0x8007007a))
                            {
                                // expected exception: HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER)
                                if (rc <= 0) continue;
                                ArrayPool<SDXUnicodeRange>.Shared.Return(unicodeRanges);
                                unicodeRanges = ArrayPool<SDXUnicodeRange>.Shared.Rent(rc + 64);

                                font1.GetUnicodeRanges(unicodeRanges.Length, unicodeRanges, out _);
                            }

                            if (!unicodeRanges.Take(rc).Any(x => x.Last <= 0xFFFF))
                                continue;
                        }
                        else
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            if (!" Aa0_!,字가あアㄱ●≤ㅥ㎘㉠Γⓐⅸ⅓─\uFFFE".Any(x => font.HasCharacter(x)))
                                continue;
                        }
                    }

                    tempVariants.Add(new(name, new FontVariant(font.Weight, font.Stretch, font.Style)));
                }

                if (!tempVariants.Any())
                    continue;

                result.Add((
                               name,
                               tempLanguageNames.Zip(tempLocalizedNames).ToDictionary(x => x.First, x => x.Second),
                               tempVariants.ToArray()));
            }

            result.Sort((a, b) => string.Compare(a.Name, b.Name, nameSortComparison));

            return result.ToArray();
        }
        finally
        {
            ArrayPool<SDXUnicodeRange>.Shared.Return(unicodeRanges);
        }
    }, cancellationToken);
}
