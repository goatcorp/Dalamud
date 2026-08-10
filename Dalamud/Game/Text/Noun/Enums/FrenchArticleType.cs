namespace Dalamud.Game.Text.Noun.Enums;

/// <summary>
/// Article types for <see cref="ClientLanguage.French"/>.
/// </summary>
public enum FrenchArticleType
{
    /// <summary>
    /// Indefinite Article (e.g., un, une, des, du).
    /// </summary>
    Indefinite = 1,

    /// <summary>
    /// Definite Article (e.g., le, la, les, l').
    /// </summary>
    Definite = 2,

    /// <summary>
    /// 1st Person Singular Possessive Determiner (e.g., mon, ma, mes).
    /// </summary>
    PossessiveFirstPerson = 3,

    /// <summary>
    /// 2nd Person Singular Possessive Determiners (e.g., ton, ta, tes).
    /// </summary>
    PossessiveSecondPerson = 4,

    /// <summary>
    /// 3rd Person Singular Possessive Determiners (e.g., son, sa, ses).
    /// </summary>
    PossessiveThirdPerson = 5,

    /// <summary>
    /// Demonstrative Determiners (e.g., ce, cet, cette, ces).
    /// </summary>
    Demonstrative = 6,

    /// <summary>
    /// Preposition à Contractions (e.g., à, au, aux, à la, à l').
    /// </summary>
    Prepositionà = 7,

    /// <summary>
    /// Preposition de (e.g., de, d').
    /// </summary>
    Prepositionde = 8,

    /// <summary>
    /// Partitive Articles / Preposition de Contractions (e.g., du, de la, de l', des).
    /// </summary>
    Partitive = 9,

    /// <summary>
    /// Conjunction que (e.g., que, qu').
    /// </summary>
    Conjunction = 10,

    /// <summary>
    /// Article zéro.
    /// </summary>
    ZeroArticle = 12,
}
