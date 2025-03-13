namespace Dalamud.Game.Text.Noun.Enums;

/// <summary>
/// Article types for <see cref="ClientLanguage.German"/>.
/// </summary>
public enum GermanArticleType
{
    /// <summary>
    /// Unbestimmter Artikel (ein, eine, etc.).
    /// </summary>
    Indefinite = 1,

    /// <summary>
    /// Bestimmter Artikel (der, die, das, etc.).
    /// </summary>
    Definite = 2,

    /// <summary>
    /// Possessivartikel "dein" (dein, deine, etc.).
    /// </summary>
    Possessive = 3,

    /// <summary>
    /// Negativartikel "kein" (kein, keine, etc.).
    /// </summary>
    Negative = 4,

    /// <summary>
    /// Nullartikel.
    /// </summary>
    ZeroArticle = 5,

    /// <summary>
    /// Demonstrativpronomen "dieser" (dieser, diese, etc.).
    /// </summary>
    Demonstrative = 6,
}
