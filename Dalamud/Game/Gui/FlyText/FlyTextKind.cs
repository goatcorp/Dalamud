namespace Dalamud.Game.Gui.FlyText;

/// <summary>
/// Enum of FlyTextKind values. Members suffixed with
/// a number seem to be a duplicate, or perform duplicate behavior.
/// </summary>
public enum FlyTextKind : int
{
    /// <summary>
    /// Val1 in serif font, Text2 in sans-serif as subtitle.
    /// Used for autos and incoming DoTs.
    /// </summary>
    AutoAttack = 0,

    /// <summary>
    /// Val1 in serif font, Text2 in sans-serif as subtitle.
    /// Does a bounce effect on appearance.
    /// </summary>
    DirectHit = 1,

    /// <summary>
    /// Val1 in larger serif font with exclamation, with Text2 in sans-serif as subtitle.
    /// Does a bigger bounce effect on appearance.
    /// </summary>
    CriticalHit = 2,

    /// <summary>
    /// Val1 in even larger serif font with 2 exclamations, Text2 in
    /// sans-serif as subtitle. Does a large bounce effect on appearance.
    /// Does not scroll up or down the screen.
    /// </summary>
    CriticalDirectHit = 3,

    /// <summary>
    /// AutoAttack with sans-serif Text1 to the left of the Val1.
    /// </summary>
    NamedAttack = 4,

    /// <summary>
    /// DirectHit with sans-serif Text1 to the left of the Val1.
    /// </summary>
    NamedDirectHit = 5,

    /// <summary>
    /// CriticalHit with sans-serif Text1 to the left of the Val1.
    /// </summary>
    NamedCriticalHit = 6,

    /// <summary>
    /// CriticalDirectHit with sans-serif Text1 to the left of the Val1.
    /// </summary>
    NamedCriticalDirectHit = 7,

    /// <summary>
    /// All caps, serif MISS.
    /// </summary>
    Miss = 8,

    /// <summary>
    /// Sans-serif Text1 next to all caps serif MISS.
    /// </summary>
    NamedMiss = 9,

    /// <summary>
    /// All caps serif DODGE.
    /// </summary>
    Dodge = 10,

    /// <summary>
    /// Sans-serif Text1 next to all caps serif DODGE.
    /// </summary>
    NamedDodge = 11,

    /// <summary>
    /// Icon next to sans-serif Text1.
    /// </summary>
    NamedIcon = 12,

    /// <summary>
    /// Icon next to sans-serif Text1 (2).
    /// </summary>
    NamedIcon2 = 13,

    /// <summary>
    /// Serif Val1 with all caps condensed font EXP with Text2 in sans-serif as subtitle.
    /// </summary>
    Exp = 14,

    /// <summary>
    /// Serif Val1 with all caps condensed font ISLAND EXP with Text2 in sans-serif as subtitle.
    /// </summary>
    IslandExp = 15,

    /// <summary>
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font MP with Text2 in sans-serif as subtitle.
    /// </summary>
    NamedMp = 16,

    /// <summary>
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font TP with Text2 in sans-serif as subtitle.
    /// </summary>
    NamedTp = 17,

    /// <summary>
    /// AutoAttack with sans-serif Text1 to the left of the Val1 (2).
    /// </summary>
    NamedAttack2 = 18,

    /// <summary>
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font MP with Text2 in sans-serif as subtitle (2).
    /// </summary>
    NamedMp2 = 19,

    /// <summary>
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font TP with Text2 in sans-serif as subtitle (2).
    /// </summary>
    NamedTp2 = 20,

    /// <summary>
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font EP with Text2 in sans-serif as subtitle.
    /// </summary>
    NamedEp = 21,

    /// <summary>
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font CP with Text2 in sans-serif as subtitle.
    /// </summary>
    NamedCp = 22,

    /// <summary>
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font GP with Text2 in sans-serif as subtitle.
    /// </summary>
    NamedGp = 23,

    /// <summary>
    /// Displays nothing.
    /// </summary>
    None = 24,

    /// <summary>
    /// All caps serif INVULNERABLE.
    /// </summary>
    Invulnerable = 25,

    /// <summary>
    /// All caps sans-serif condensed font INTERRUPTED!
    /// Does a large bounce effect on appearance.
    /// Does not scroll up or down the screen.
    /// </summary>
    Interrupted = 26,

    /// <summary>
    /// AutoAttack with no Text2.
    /// </summary>
    AutoAttackNoText = 27,

    /// <summary>
    /// AutoAttack with no Text2 (2).
    /// </summary>
    AutoAttackNoText2 = 28,

    /// <summary>
    /// Val1 in larger serif font with exclamation, with Text2 in sans-serif as subtitle. Does a bigger bounce effect on appearance (2).
    /// </summary>
    CriticalHit2 = 29,

    /// <summary>
    /// AutoAttack with no Text2 (3).
    /// </summary>
    AutoAttackNoText3 = 30,

    /// <summary>
    /// CriticalHit with sans-serif Text1 to the left of the Val1 (2).
    /// </summary>
    NamedCriticalHit2 = 31,

    /// <summary>
    /// Same as NamedCriticalHit with a green (cannot change) MP in condensed font to the right of Val1.
    /// Does a jiggle effect to the right on appearance.
    /// </summary>
    NamedCriticalHitWithMp = 32,

    /// <summary>
    /// Same as NamedCriticalHit with a yellow (cannot change) TP in condensed font to the right of Val1.
    /// Does a jiggle effect to the right on appearance.
    /// </summary>
    NamedCriticalHitWithTp = 33,

    /// <summary>
    /// Same as NamedIcon with sans-serif "has no effect!" to the right.
    /// </summary>
    NamedIconHasNoEffect = 34,

    /// <summary>
    /// Same as NamedIcon but Text1 is slightly faded. Used for buff expiration.
    /// </summary>
    NamedIconFaded = 35,

    /// <summary>
    /// Same as NamedIcon but Text1 is slightly faded (2).
    /// Used for buff expiration.
    /// </summary>
    NamedIconFaded2 = 36,

    /// <summary>
    /// Text1 in sans-serif font.
    /// </summary>
    Named = 37,

    /// <summary>
    /// Same as NamedIcon with sans-serif "(fully resisted)" to the right.
    /// </summary>
    NamedIconFullyResisted = 38,

    /// <summary>
    /// All caps serif 'INCAPACITATED!'.
    /// </summary>
    Incapacitated = 39,

    /// <summary>
    /// Text1 with sans-serif "(fully resisted)" to the right.
    /// </summary>
    NamedFullyResisted = 40,

    /// <summary>
    /// Text1 with sans-serif "has no effect!" to the right.
    /// </summary>
    NamedHasNoEffect = 41,

    /// <summary>
    /// AutoAttack with sans-serif Text1 to the left of the Val1 (3).
    /// </summary>
    NamedAttack3 = 42,

    /// <summary>
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font MP with Text2 in sans-serif as subtitle (3).
    /// </summary>
    NamedMp3 = 43,

    /// <summary>
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font TP with Text2 in sans-serif as subtitle (3).
    /// </summary>
    NamedTp3 = 44,

    /// <summary>
    /// Same as NamedIcon with serif "INVULNERABLE!" beneath the Text1.
    /// </summary>
    NamedIconInvulnerable = 45,

    /// <summary>
    /// All caps serif RESIST.
    /// </summary>
    Resist = 46,

    /// <summary>
    /// Same as NamedIcon but places the given icon in the item icon outline.
    /// </summary>
    NamedIconWithItemOutline = 47,

    /// <summary>
    /// AutoAttack with no Text2 (4).
    /// </summary>
    AutoAttackNoText4 = 48,

    /// <summary>
    /// Val1 in larger serif font with exclamation, with Text2 in sans-serif as subtitle (3).
    /// Does a bigger bounce effect on appearance.
    /// </summary>
    CriticalHit3 = 49,

    /// <summary>
    /// All caps serif REFLECT.
    /// </summary>
    Reflect = 50,

    /// <summary>
    /// All caps serif REFLECTED.
    /// </summary>
    Reflected = 51,

    /// <summary>
    /// Val1 in serif font, Text2 in sans-serif as subtitle (2).
    /// Does a bounce effect on appearance.
    /// </summary>
    DirectHit2 = 52,

    /// <summary>
    /// Val1 in larger serif font with exclamation, with Text2 in sans-serif as subtitle (4).
    /// Does a bigger bounce effect on appearance.
    /// </summary>
    CriticalHit4 = 53,

    /// <summary>
    /// Val1 in even larger serif font with 2 exclamations, Text2 in sans-serif as subtitle (2).
    /// Does a large bounce effect on appearance. Does not scroll up or down the screen.
    /// </summary>
    CriticalDirectHit2 = 54,
}
