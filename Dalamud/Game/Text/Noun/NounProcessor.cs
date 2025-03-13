using System.Collections.Concurrent;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game.Text.Noun.Enums;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

using Lumina.Excel;
using Lumina.Text.ReadOnly;

using LSeStringBuilder = Lumina.Text.SeStringBuilder;
using LSheets = Lumina.Excel.Sheets;

namespace Dalamud.Game.Text.Noun;

/*
Attributive sheet:
  Japanese:
    Unknown0 = Singular Demonstrative
    Unknown1 = Plural Demonstrative
  English:
    Unknown2 = Article before a singular noun beginning with a consonant sound
    Unknown3 = Article before a generic noun beginning with a consonant sound
    Unknown4 = N/A
    Unknown5 = Article before a singular noun beginning with a vowel sound
    Unknown6 = Article before a generic noun beginning with a vowel sound
    Unknown7 = N/A
  German:
    Unknown8 = Nominative Masculine
    Unknown9 = Nominative Feminine
    Unknown10 = Nominative Neutral
    Unknown11 = Nominative Plural
    Unknown12 = Genitive Masculine
    Unknown13 = Genitive Feminine
    Unknown14 = Genitive Neutral
    Unknown15 = Genitive Plural
    Unknown16 = Dative Masculine
    Unknown17 = Dative Feminine
    Unknown18 = Dative Neutral
    Unknown19 = Dative Plural
    Unknown20 = Accusative Masculine
    Unknown21 = Accusative Feminine
    Unknown22 = Accusative Neutral
    Unknown23 = Accusative Plural
  French (unsure):
    Unknown24 = Singular Article
    Unknown25 = Singular Masculine Article
    Unknown26 = Plural Masculine Article
    Unknown27 = ?
    Unknown28 = ?
    Unknown29 = Singular Masculine/Feminine Article, before a noun beginning in a vowel or an h
    Unknown30 = Plural Masculine/Feminine Article, before a noun beginning in a vowel or an h
    Unknown31 = ?
    Unknown32 = ?
    Unknown33 = Singular Feminine Article
    Unknown34 = Plural Feminine Article
    Unknown35 = ?
    Unknown36 = ?
    Unknown37 = Singular Masculine/Feminine Article, before a noun beginning in a vowel or an h
    Unknown38 = Plural Masculine/Feminine Article, before a noun beginning in a vowel or an h
    Unknown39 = ?
    Unknown40 = ?

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
    private const int Unknown5ColumnIdx = 5; // probably used in Chinese texts
    private const int PronounColumnIdx = 6;
    private const int ArticleColumnIdx = 7;

    private static readonly ModuleLog Log = new("NounProcessor");

    [ServiceManager.ServiceDependency]
    private readonly DataManager dataManager = Service<DataManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration dalamudConfiguration = Service<DalamudConfiguration>.Get();

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

        var builder = LSeStringBuilder.SharedPool.Get();

        // Ko-So-A-Do
        var ksad = attributiveSheet.GetRow((uint)nounParams.ArticleType).ReadStringColumn(nounParams.Quantity > 1 ? 1 : 0);
        if (!ksad.IsEmpty)
        {
            builder.Append(ksad);

            if (nounParams.Quantity > 1)
            {
                builder.ReplaceText("[n]"u8, ReadOnlySeString.FromText(nounParams.Quantity.ToString()));
            }
        }

        if (!nounParams.LinkMarker.IsEmpty)
            builder.Append(nounParams.LinkMarker);

        var text = row.ReadStringColumn(nounParams.ColumnOffset);
        if (!text.IsEmpty)
            builder.Append(text);

        var ross = builder.ToReadOnlySeString();
        LSeStringBuilder.SharedPool.Return(builder);
        return ross;
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

        var builder = LSeStringBuilder.SharedPool.Get();

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
                builder.Append(article);

            if (!nounParams.LinkMarker.IsEmpty)
                builder.Append(nounParams.LinkMarker);
        }

        var text = row.ReadStringColumn(nounParams.ColumnOffset + (nounParams.Quantity == 1 ? SingularColumnIdx : PluralColumnIdx));
        if (!text.IsEmpty)
            builder.Append(text);

        builder.ReplaceText("[n]"u8, ReadOnlySeString.FromText(nounParams.Quantity.ToString()));

        var ross = builder.ToReadOnlySeString();
        LSeStringBuilder.SharedPool.Return(builder);
        return ross;
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
             a1->Offsets[5] = Unknown5ColumnIdx
             a1->Offsets[6] = ArticleColumnIdx
         */

        var sheet = this.dataManager.Excel.GetSheet<RawRow>(nounParams.Language.ToLumina(), nounParams.SheetName);
        if (!sheet.TryGetRow(nounParams.RowId, out var row))
        {
            Log.Warning("Sheet {SheetName} does not contain row #{RowId}", nounParams.SheetName, nounParams.RowId);
            return default;
        }

        var attributiveSheet = this.dataManager.Excel.GetSheet<RawRow>(nounParams.Language.ToLumina(), nameof(LSheets.Attributive));

        var builder = LSeStringBuilder.SharedPool.Get();
        ReadOnlySeString ross;

        if (nounParams.IsActionSheet)
        {
            builder.Append(row.ReadStringColumn(nounParams.GrammaticalCase));
            builder.ReplaceText("[n]"u8, ReadOnlySeString.FromText(nounParams.Quantity.ToString()));

            ross = builder.ToReadOnlySeString();
            LSeStringBuilder.SharedPool.Return(builder);
            return ross;
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
                    builder.Append(grammaticalGender);
            }

            if (!nounParams.LinkMarker.IsEmpty)
                builder.Append(nounParams.LinkMarker);

            builder.Append(text);

            var plural = attributiveSheet.GetRow((uint)(caseRowOffset + 26))
                                         .ReadStringColumn(caseColumnOffset + genderIndex);
            if (builder.ContainsText("[p]"u8))
                builder.ReplaceText("[p]"u8, plural);
            else
                builder.Append(plural);

            if (hasT)
            {
                var article =
                    attributiveSheet.GetRow(39).ReadStringColumn(caseColumnOffset + genderIndex); // Definiter Artikel
                builder.ReplaceText("[t]"u8, article);
            }
        }

        var pa = attributiveSheet.GetRow(24).ReadStringColumn(caseColumnOffset + genderIndex);
        builder.ReplaceText("[pa]"u8, pa);

        RawRow declensionRow;

        declensionRow = (GermanArticleType)nounParams.ArticleType switch
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

        var declension = declensionRow.ReadStringColumn(caseColumnOffset + genderIndex);
        builder.ReplaceText("[a]"u8, declension);

        builder.ReplaceText("[n]"u8, ReadOnlySeString.FromText(nounParams.Quantity.ToString()));

        ross = builder.ToReadOnlySeString();
        LSeStringBuilder.SharedPool.Return(builder);
        return ross;
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
            a1->Offsets[2] = StartsWithVowelColumnIdx
            a1->Offsets[3] = PronounColumnIdx
            a1->Offsets[4] = Unknown5ColumnIdx
            a1->Offsets[5] = ArticleColumnIdx
        */

        var sheet = this.dataManager.Excel.GetSheet<RawRow>(nounParams.Language.ToLumina(), nounParams.SheetName);
        if (!sheet.TryGetRow(nounParams.RowId, out var row))
        {
            Log.Warning("Sheet {SheetName} does not contain row #{RowId}", nounParams.SheetName, nounParams.RowId);
            return default;
        }

        var attributiveSheet = this.dataManager.Excel.GetSheet<RawRow>(nounParams.Language.ToLumina(), nameof(LSheets.Attributive));

        var builder = LSeStringBuilder.SharedPool.Get();
        ReadOnlySeString ross;

        var startsWithVowelColumn = nounParams.ColumnOffset + StartsWithVowelColumnIdx;
        var startsWithVowel = startsWithVowelColumn >= 0
                                  ? row.ReadInt8Column(startsWithVowelColumn)
                                  : ~startsWithVowelColumn;

        var pronounColumn = nounParams.ColumnOffset + PronounColumnIdx;
        var pronoun = pronounColumn >= 0 ? row.ReadInt8Column(pronounColumn) : ~pronounColumn;

        var articleColumn = nounParams.ColumnOffset + ArticleColumnIdx;
        var article = articleColumn >= 0 ? row.ReadInt8Column(articleColumn) : ~articleColumn;

        var v20 = 4 * (startsWithVowel + 6 + (2 * pronoun));

        if (article != 0)
        {
            var v21 = attributiveSheet.GetRow((uint)nounParams.ArticleType).ReadStringColumn(v20);
            if (!v21.IsEmpty)
                builder.Append(v21);

            if (!nounParams.LinkMarker.IsEmpty)
                builder.Append(nounParams.LinkMarker);

            var text = row.ReadStringColumn(nounParams.ColumnOffset + (nounParams.Quantity <= 1 ? SingularColumnIdx : PluralColumnIdx));
            if (!text.IsEmpty)
                builder.Append(text);

            if (nounParams.Quantity <= 1)
                builder.ReplaceText("[n]"u8, ReadOnlySeString.FromText(nounParams.Quantity.ToString()));

            ross = builder.ToReadOnlySeString();
            LSeStringBuilder.SharedPool.Return(builder);
            return ross;
        }

        var v17 = row.ReadInt8Column(nounParams.ColumnOffset + Unknown5ColumnIdx);
        if (v17 != 0 && (nounParams.Quantity > 1 || v17 == 2))
        {
            var v29 = attributiveSheet.GetRow((uint)nounParams.ArticleType).ReadStringColumn(v20 + 2);
            if (!v29.IsEmpty)
            {
                builder.Append(v29);

                if (!nounParams.LinkMarker.IsEmpty)
                    builder.Append(nounParams.LinkMarker);

                var text = row.ReadStringColumn(nounParams.ColumnOffset + PluralColumnIdx);
                if (!text.IsEmpty)
                    builder.Append(text);
            }
        }
        else
        {
            var v27 = attributiveSheet.GetRow((uint)nounParams.ArticleType).ReadStringColumn(v20 + (v17 != 0 ? 1 : 3));
            if (!v27.IsEmpty)
                builder.Append(v27);

            if (!nounParams.LinkMarker.IsEmpty)
                builder.Append(nounParams.LinkMarker);

            var text = row.ReadStringColumn(nounParams.ColumnOffset + SingularColumnIdx);
            if (!text.IsEmpty)
                builder.Append(text);
        }

        builder.ReplaceText("[n]"u8, ReadOnlySeString.FromText(nounParams.Quantity.ToString()));

        ross = builder.ToReadOnlySeString();
        LSeStringBuilder.SharedPool.Return(builder);
        return ross;
    }
}
