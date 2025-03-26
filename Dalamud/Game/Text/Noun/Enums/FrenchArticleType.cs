namespace Dalamud.Game.Text.Noun.Enums;

/// <summary>
/// Article types for <see cref="ClientLanguage.French"/>.
/// </summary>
public enum FrenchArticleType
{
    /// <summary>
    /// Indefinite article (une, des).
    /// </summary>
    Indefinite = 1,

    /// <summary>
    /// Definite article (le, la, les).
    /// </summary>
    Definite = 2,

    /// <summary>
    /// Possessive article (mon, mes).
    /// </summary>
    PossessiveFirstPerson = 3,

    /// <summary>
    /// Possessive article (ton, tes).
    /// </summary>
    PossessiveSecondPerson = 4,

    /// <summary>
    /// Possessive article (son, ses).
    /// </summary>
    PossessiveThirdPerson = 5,
}
