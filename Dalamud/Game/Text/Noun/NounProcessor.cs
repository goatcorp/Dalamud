using System.Collections.Concurrent;

using Dalamud.Data;
using Dalamud.Game.Text.Noun.Enums;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

using Lumina.Excel;
using Lumina.Text.ReadOnly;

using LSheets = Lumina.Excel.Sheets;

namespace Dalamud.Game.Text.Noun;

/*
Attributive sheet:
  Japanese:
    Column 0 = Singular Demonstrative
    Column 1 = Plural Demonstrative
  English:
    Column 2 = Singular Consonant
    Column 3 = Generic Consonant
    Column 4 = Plural Consonant
    Column 5 = Singular Vowel
    Column 6 = Generic Vowel
    Column 7 = Plural Vowel
  German:
    Column 8 = Nominative Masculine
    Column 9 = Nominative Feminine
    Column 10 = Nominative Neutral
    Column 11 = Nominative Plural
    Column 12 = Genitive Masculine
    Column 13 = Genitive Feminine
    Column 14 = Genitive Neutral
    Column 15 = Genitive Plural
    Column 16 = Dative Masculine
    Column 17 = Dative Feminine
    Column 18 = Dative Neutral
    Column 19 = Dative Plural
    Column 20 = Accusative Masculine
    Column 21 = Accusative Feminine
    Column 22 = Accusative Neutral
    Column 23 = Accusative Plural
  French:
    Column 24 = Masculine Consonant Base
    Column 25 = Masculine Consonant Singular
    Column 26 = Masculine Consonant Plural
    Column 27 = Masculine Consonant Mass
    Column 28 = Masculine Vowel Base
    Column 29 = Masculine Vowel Singular
    Column 30 = Masculine Vowel Plural
    Column 31 = Masculine Vowel Mass
    Column 32 = Feminine Consonant Base
    Column 33 = Feminine Consonant Singular
    Column 34 = Feminine Consonant Plural
    Column 35 = Feminine Consonant Mass
    Column 36 = Feminine Vowel Base
    Column 37 = Feminine Vowel Singular
    Column 38 = Feminine Vowel Plural
    Column 39 = Feminine Vowel Mass
    Column 40 = N/A

Placeholders:
    [t] = article or grammatical gender (EN: the, DE: der, die, das)
    [n] = amount (number)
    [a] = declension
    [p] = plural
    [pa] = ?
*/

/// <summary>
/// Provides functionality to process texts from sheets containing grammatical placeholders.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class NounProcessor : IServiceType
{
    // column names from ExdSchema, most likely incorrect
    private const int SingularColumnIdx = 0;
    private const int AdjectiveColumnIdx = 1;
    private const int PluralColumnIdx = 2;
    private const int PossessivePronounColumnIdx = 3;
    private const int StartsWithVowelColumnIdx = 4;
    private const int CountabilityColumnIdx = 5;
    private const int PronounColumnIdx = 6;
    private const int ArticleColumnIdx = 7;

    private static readonly ModuleLog Log = ModuleLog.Create<NounProcessor>();

    [ServiceManager.ServiceDependency]
    private readonly DataManager dataManager = Service<DataManager>.Get();

    private readonly ConcurrentDictionary<NounParams, ReadOnlySeString> cache = [];

    [ServiceManager.ServiceConstructor]
    private NounProcessor()
    {
    }

    /// <summary>
    /// Processes a specific row from a sheet and generates a formatted string based on grammatical and language-specific rules.
    /// </summary>
    /// <param name="nounParams">Parameters for processing.</param>
    /// <returns>A ReadOnlySeString representing the processed text.</returns>
    public ReadOnlySeString ProcessNoun(NounParams nounParams)
    {
        if (nounParams.GrammaticalCase < 0 || nounParams.GrammaticalCase > 5)
            return default;

        if (this.cache.TryGetValue(nounParams, out var value))
            return value;

        var output = nounParams.Language switch
        {
            ClientLanguage.Japanese => this.ResolveNounJa(nounParams),
            ClientLanguage.English => this.ResolveNounEn(nounParams),
            ClientLanguage.German => this.ResolveNounDe(nounParams),
            ClientLanguage.French => this.ResolveNounFr(nounParams),
            _ => default,
        };

        this.cache.TryAdd(nounParams, output);

        return output;
    }

    /// <summary>
    /// Resolves noun placeholders in Japanese text.
    /// </summary>
    /// <param name="nounParams">Parameters for processing.</param>
    /// <returns>A ReadOnlySeString representing the processed text.</returns>
    /// <remarks>
    /// This is a C# implementation of Component::Text::Localize::NounJa.Resolve.
    /// </remarks>
    private ReadOnlySeString ResolveNounJa(NounParams nounParams)
    {
        var sheet = this.dataManager.Excel.GetSheet<RawRow>(nounParams.Language.ToLumina(), nounParams.SheetName);
        if (!sheet.TryGetRow(nounParams.RowId, out var row))
        {
            Log.Warning("Sheet {SheetName} does not contain row #{RowId}", nounParams.SheetName, nounParams.RowId);
            return default;
        }

        var attributiveSheet = this.dataManager.Excel.GetSheet<RawRow>(nounParams.Language.ToLumina(), nameof(LSheets.Attributive));

        using var rssb = new RentedSeStringBuilder();

        // Ko-So-A-Do
        var ksad = attributiveSheet.GetRow((uint)nounParams.ArticleType).ReadStringColumn(nounParams.Quantity > 1 ? 1 : 0);
        if (!ksad.IsEmpty)
        {
            rssb.Builder.Append(ksad);

            if (nounParams.Quantity > 1)
            {
                rssb.Builder.ReplaceText("[n]"u8, ReadOnlySeString.FromText(nounParams.Quantity.ToString()));
            }
        }

        if (!nounParams.LinkMarker.IsEmpty)
            rssb.Builder.Append(nounParams.LinkMarker);

        var text = row.ReadStringColumn(nounParams.ColumnOffset);
        if (!text.IsEmpty)
            rssb.Builder.Append(text);

        return rssb.Builder.ToReadOnlySeString();
    }

    /// <summary>
    /// Resolves noun placeholders in English text.
    /// </summary>
    /// <param name="nounParams">Parameters for processing.</param>
    /// <returns>A ReadOnlySeString representing the processed text.</returns>
    /// <remarks>
    /// This is a C# implementation of Component::Text::Localize::NounEn.Resolve.
    /// </remarks>
    private ReadOnlySeString ResolveNounEn(NounParams nounParams)
    {
        /*
          a1->Offsets[0] = SingularColumnIdx
          a1->Offsets[1] = PluralColumnIdx
          a1->Offsets[2] = StartsWithVowelColumnIdx
          a1->Offsets[3] = PossessivePronounColumnIdx
          a1->Offsets[4] = ArticleColumnIdx
        */

        var sheet = this.dataManager.Excel.GetSheet<RawRow>(nounParams.Language.ToLumina(), nounParams.SheetName);
        if (!sheet.TryGetRow(nounParams.RowId, out var row))
        {
            Log.Warning("Sheet {SheetName} does not contain row #{RowId}", nounParams.SheetName, nounParams.RowId);
            return default;
        }

        var attributiveSheet = this.dataManager.Excel.GetSheet<RawRow>(nounParams.Language.ToLumina(), nameof(LSheets.Attributive));

        using var rssb = new RentedSeStringBuilder();

        var isProperNounColumn = nounParams.ColumnOffset + ArticleColumnIdx;
        var isProperNoun = isProperNounColumn >= 0 ? row.ReadInt8Column(isProperNounColumn) : ~isProperNounColumn;
        if (isProperNoun == 0)
        {
            var startsWithVowelColumn = nounParams.ColumnOffset + StartsWithVowelColumnIdx;
            var startsWithVowel = startsWithVowelColumn >= 0
                                      ? row.ReadInt8Column(startsWithVowelColumn)
                                      : ~startsWithVowelColumn;

            var articleColumn = startsWithVowel + (2 * (startsWithVowel + 1));
            var grammaticalNumberColumnOffset = nounParams.Quantity == 1 ? SingularColumnIdx : PluralColumnIdx;
            var article = attributiveSheet.GetRow((uint)nounParams.ArticleType)
                                          .ReadStringColumn(articleColumn + grammaticalNumberColumnOffset);
            if (!article.IsEmpty)
                rssb.Builder.Append(article);

            if (!nounParams.LinkMarker.IsEmpty)
                rssb.Builder.Append(nounParams.LinkMarker);
        }

        var text = row.ReadStringColumn(nounParams.ColumnOffset + (nounParams.Quantity == 1 ? SingularColumnIdx : PluralColumnIdx));
        if (!text.IsEmpty)
            rssb.Builder.Append(text);

        rssb.Builder.ReplaceText("[n]"u8, ReadOnlySeString.FromText(nounParams.Quantity.ToString()));

        return rssb.Builder.ToReadOnlySeString();
    }

    /// <summary>
    /// Resolves noun placeholders in German text.
    /// </summary>
    /// <param name="nounParams">Parameters for processing.</param>
    /// <returns>A ReadOnlySeString representing the processed text.</returns>
    /// <remarks>
    /// This is a C# implementation of Component::Text::Localize::NounDe.Resolve.
    /// </remarks>
    private ReadOnlySeString ResolveNounDe(NounParams nounParams)
    {
        /*
             a1->Offsets[0] = SingularColumnIdx
             a1->Offsets[1] = PluralColumnIdx
             a1->Offsets[2] = PronounColumnIdx
             a1->Offsets[3] = AdjectiveColumnIdx
             a1->Offsets[4] = PossessivePronounColumnIdx
             a1->Offsets[5] = CountabilityColumnIdx
             a1->Offsets[6] = ArticleColumnIdx
         */

        var sheet = this.dataManager.Excel.GetSheet<RawRow>(nounParams.Language.ToLumina(), nounParams.SheetName);
        if (!sheet.TryGetRow(nounParams.RowId, out var row))
        {
            Log.Warning("Sheet {SheetName} does not contain row #{RowId}", nounParams.SheetName, nounParams.RowId);
            return default;
        }

        var attributiveSheet = this.dataManager.Excel.GetSheet<RawRow>(nounParams.Language.ToLumina(), nameof(LSheets.Attributive));

        using var rssb = new RentedSeStringBuilder();

        if (nounParams.IsActionSheet)
        {
            rssb.Builder.Append(row.ReadStringColumn(nounParams.GrammaticalCase));
            rssb.Builder.ReplaceText("[n]"u8, ReadOnlySeString.FromText(nounParams.Quantity.ToString()));
            return rssb.Builder.ToReadOnlySeString();
        }

        var genderIndexColumn = nounParams.ColumnOffset + PronounColumnIdx;
        var genderIndex = genderIndexColumn >= 0 ? row.ReadInt8Column(genderIndexColumn) : ~genderIndexColumn;

        var articleIndexColumn = nounParams.ColumnOffset + ArticleColumnIdx;
        var articleIndex = articleIndexColumn >= 0 ? row.ReadInt8Column(articleIndexColumn) : ~articleIndexColumn;

        var caseColumnOffset = (4 * nounParams.GrammaticalCase) + 8;

        var caseRowOffsetColumn = nounParams.ColumnOffset + (nounParams.Quantity == 1 ? AdjectiveColumnIdx : PossessivePronounColumnIdx);
        var caseRowOffset = caseRowOffsetColumn >= 0
                                ? row.ReadInt8Column(caseRowOffsetColumn)
                                : (sbyte)~caseRowOffsetColumn;

        if (nounParams.Quantity != 1)
            genderIndex = 3;

        var hasT = false;
        var text = row.ReadStringColumn(nounParams.ColumnOffset + (nounParams.Quantity == 1 ? SingularColumnIdx : PluralColumnIdx));
        if (!text.IsEmpty)
        {
            hasT = text.ContainsText("[t]"u8);

            if (articleIndex == 0 && !hasT)
            {
                var grammaticalGender = attributiveSheet.GetRow((uint)nounParams.ArticleType)
                                                        .ReadStringColumn(caseColumnOffset + genderIndex); // Genus
                if (!grammaticalGender.IsEmpty)
                    rssb.Builder.Append(grammaticalGender);
            }

            if (!nounParams.LinkMarker.IsEmpty)
                rssb.Builder.Append(nounParams.LinkMarker);

            rssb.Builder.Append(text);

            var plural = attributiveSheet.GetRow((uint)(caseRowOffset + 26))
                                         .ReadStringColumn(caseColumnOffset + genderIndex);
            if (rssb.Builder.ContainsText("[p]"u8))
                rssb.Builder.ReplaceText("[p]"u8, plural);
            else
                rssb.Builder.Append(plural);

            if (hasT)
            {
                var article =
                    attributiveSheet.GetRow(39).ReadStringColumn(caseColumnOffset + genderIndex); // Definiter Artikel
                rssb.Builder.ReplaceText("[t]"u8, article);
            }
        }

        rssb.Builder.ReplaceText("[pa]"u8, attributiveSheet.GetRow(24).ReadStringColumn(caseColumnOffset + genderIndex));

        var declensionRow = (GermanArticleType)nounParams.ArticleType switch
        {
            // Schwache Flexion eines Adjektivs?!
            GermanArticleType.Possessive or GermanArticleType.Demonstrative => attributiveSheet.GetRow(25),
            _ when hasT => attributiveSheet.GetRow(25),

            // Starke Deklination
            GermanArticleType.ZeroArticle => attributiveSheet.GetRow(38),

            // Gemischte Deklination
            GermanArticleType.Definite => attributiveSheet.GetRow(37),

            // Starke Flexion eines Artikels?!
            GermanArticleType.Indefinite or GermanArticleType.Negative => attributiveSheet.GetRow(26),
            _ => attributiveSheet.GetRow(26),
        };

        rssb.Builder.ReplaceText("[a]"u8, declensionRow.ReadStringColumn(caseColumnOffset + genderIndex));
        rssb.Builder.ReplaceText("[n]"u8, ReadOnlySeString.FromText(nounParams.Quantity.ToString()));

        return rssb.Builder.ToReadOnlySeString();
    }

    /// <summary>
    /// Resolves noun placeholders in French text.
    /// </summary>
    /// <param name="nounParams">Parameters for processing.</param>
    /// <returns>A ReadOnlySeString representing the processed text.</returns>
    /// <remarks>
    /// This is a C# implementation of Component::Text::Localize::NounFr.Resolve.
    /// </remarks>
    private ReadOnlySeString ResolveNounFr(NounParams nounParams)
    {
        /*
            a1->Offsets[0] = SingularColumnIdx
            a1->Offsets[1] = PluralColumnIdx
            a1->Offsets[2] = PronounColumnIdx
            a1->Offsets[3] = AdjectiveColumnIdx
            a1->Offsets[4] = PossessivePronounColumnIdx
            a1->Offsets[5] = CountabilityColumnIdx
            a1->Offsets[6] = ArticleColumnIdx
        */

        var sheet = this.dataManager.Excel.GetSheet<RawRow>(nounParams.Language.ToLumina(), nounParams.SheetName);
        if (!sheet.TryGetRow(nounParams.RowId, out var row))
        {
            Log.Warning("Sheet {SheetName} does not contain row #{RowId}", nounParams.SheetName, nounParams.RowId);
            return default;
        }

        var attributiveSheet = this.dataManager.Excel.GetSheet<RawRow>(nounParams.Language.ToLumina(), nameof(LSheets.Attributive));
        var articleRow = attributiveSheet.GetRow((uint)nounParams.ArticleType);

        using var rssb = new RentedSeStringBuilder();

        var startsWithVowelColumn = nounParams.ColumnOffset + StartsWithVowelColumnIdx;
        var startsWithVowel = startsWithVowelColumn < 0 ? ~startsWithVowelColumn : row.ReadInt8Column(startsWithVowelColumn);

        var pronounColumn = nounParams.ColumnOffset + PronounColumnIdx;
        var pronoun = pronounColumn < 0 ? ~pronounColumn : row.ReadInt8Column(pronounColumn);

        var countabilityColumn = nounParams.ColumnOffset + CountabilityColumnIdx;
        var countability = countabilityColumn < 0 ? ~countabilityColumn : row.ReadInt8Column(countabilityColumn);

        var articleColumn = nounParams.ColumnOffset + ArticleColumnIdx;
        var article = articleColumn < 0 ? ~articleColumn : row.ReadInt8Column(articleColumn);

        var attributiveColumn = 4 * (startsWithVowel + 2 * (pronoun + 3));
        var numerusColumnIndex = SingularColumnIdx;

        if (article != 0)
        {
            var attr = articleRow.ReadStringColumn(attributiveColumn);
            if (!attr.IsEmpty)
                rssb.Builder.Append(attr);

            if (nounParams.Quantity <= 1)
                numerusColumnIndex = SingularColumnIdx;
            else
                numerusColumnIndex = PluralColumnIdx;
        }
        else if (countability != 0) // Countable Nouns
        {
            if (nounParams.Quantity <= 1 && countability != 2) // Plural-only Nouns
            {
                var attr = articleRow.ReadStringColumn(attributiveColumn + 1);
                if (!attr.IsEmpty)
                    rssb.Builder.Append(attr);

                numerusColumnIndex = SingularColumnIdx;
            }
            else
            {
                var attr = articleRow.ReadStringColumn(attributiveColumn + 2);
                if (!attr.IsEmpty)
                    rssb.Builder.Append(attr);

                numerusColumnIndex = PluralColumnIdx;
            }
        }
        else // Mass Nouns / Uncountable Nouns
        {
            var attr = articleRow.ReadStringColumn(attributiveColumn + 3);
            if (!attr.IsEmpty)
                rssb.Builder.Append(attr);

            numerusColumnIndex = SingularColumnIdx;
        }

        if (!nounParams.LinkMarker.IsEmpty)
            rssb.Builder.Append(nounParams.LinkMarker);

        var numerus = row.ReadStringColumn(nounParams.ColumnOffset + numerusColumnIndex);
        if (!numerus.IsEmpty)
            rssb.Builder.Append(numerus);

        rssb.Builder.ReplaceText("[n]"u8, ReadOnlySeString.FromText(nounParams.Quantity.ToString()));

        return rssb.Builder.ToReadOnlySeString();
    }
}
