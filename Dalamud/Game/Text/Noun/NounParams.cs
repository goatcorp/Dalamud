using Dalamud.Game.Text.Noun.Enums;

using Lumina.Text.ReadOnly;

using LSheets = Lumina.Excel.Sheets;

namespace Dalamud.Game.Text.Noun;

/// <summary>
/// Parameters for noun processing.
/// </summary>
internal record struct NounParams()
{
    /// <summary>
    /// The language of the sheet to be processed.
    /// </summary>
    public required ClientLanguage Language;

    /// <summary>
    /// The name of the sheet containing the row to process.
    /// </summary>
    public required string SheetName = string.Empty;

    /// <summary>
    /// The row id within the sheet to process.
    /// </summary>
    public required uint RowId;

    /// <summary>
    /// The quantity of the entity (default is 1). Used to determine grammatical number (e.g., singular or plural).
    /// </summary>
    public int Quantity = 1;

    /// <summary>
    /// The article type.
    /// </summary>
    /// <remarks>
    /// Depending on the <see cref="Language"/>, this has different meanings.<br/>
    /// See <see cref="JapaneseArticleType"/>, <see cref="GermanArticleType"/>, <see cref="FrenchArticleType"/>, <see cref="EnglishArticleType"/>.
    /// </remarks>
    public int ArticleType = 1;

    /// <summary>
    /// The grammatical case (e.g., Nominative, Genitive, Dative, Accusative) used for German texts (default is 0).
    /// </summary>
    public int GrammaticalCase = 0;

    /// <summary>
    /// An optional string that is placed in front of the text that should be linked, such as item names (default is an empty string; the game uses "//").
    /// </summary>
    public ReadOnlySeString LinkMarker = default;

    /// <summary>
    /// An indicator that this noun will be processed from an Action sheet. Only used for German texts.
    /// </summary>
    public bool IsActionSheet;

    /// <summary>
    /// Gets the column offset.
    /// </summary>
    public readonly int ColumnOffset => this.SheetName switch
    {
        // See "E8 ?? ?? ?? ?? 44 8B 6B 08"
        nameof(LSheets.BeastTribe) => 10,
        nameof(LSheets.DeepDungeonItem) => 1,
        nameof(LSheets.DeepDungeonEquipment) => 1,
        nameof(LSheets.DeepDungeonMagicStone) => 1,
        nameof(LSheets.DeepDungeonDemiclone) => 1,
        nameof(LSheets.Glasses) => 4,
        nameof(LSheets.GlassesStyle) => 15,
        nameof(LSheets.Ornament) => 8, // not part of that function, but still shifted
        _ => 0,
    };
}
