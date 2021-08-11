namespace Dalamud.Game.Gui.FlyText
{
    /// <summary>
    /// Enum of FlyTextKind values. Members suffixed with
    /// a number seem to be a duplicate, or perform duplicate behavior.
    /// </summary>
    public enum FlyTextKind
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
        /// Sans-serif Text1 next to serif Val1 with all caps condensed font MP with Text2 in sans-serif as subtitle.
        /// </summary>
        NamedMp = 15,

        /// <summary>
        /// Sans-serif Text1 next to serif Val1 with all caps condensed font TP with Text2 in sans-serif as subtitle.
        /// </summary>
        NamedTp = 16,

        /// <summary>
        /// AutoAttack with sans-serif Text1 to the left of the Val1 (2).
        /// </summary>
        NamedAttack2 = 17,

        /// <summary>
        /// Sans-serif Text1 next to serif Val1 with all caps condensed font MP with Text2 in sans-serif as subtitle (2).
        /// </summary>
        NamedMp2 = 18,

        /// <summary>
        /// Sans-serif Text1 next to serif Val1 with all caps condensed font TP with Text2 in sans-serif as subtitle (2).
        /// </summary>
        NamedTp2 = 19,

        /// <summary>
        /// Sans-serif Text1 next to serif Val1 with all caps condensed font EP with Text2 in sans-serif as subtitle.
        /// </summary>
        NamedEp = 20,

        /// <summary>
        /// Displays nothing.
        /// </summary>
        None = 21,

        /// <summary>
        /// All caps serif INVULNERABLE.
        /// </summary>
        Invulnerable = 22,

        /// <summary>
        /// All caps sans-serif condensed font INTERRUPTED!
        /// Does a large bounce effect on appearance.
        /// Does not scroll up or down the screen.
        /// </summary>
        Interrupted = 23,

        /// <summary>
        /// AutoAttack with no Text2.
        /// </summary>
        AutoAttackNoText = 24,

        /// <summary>
        /// AutoAttack with no Text2 (2).
        /// </summary>
        AutoAttackNoText2 = 25,

        /// <summary>
        /// Val1 in larger serif font with exclamation, with Text2 in sans-serif as subtitle. Does a bigger bounce effect on appearance (2).
        /// </summary>
        CriticalHit2 = 26,

        /// <summary>
        /// AutoAttack with no Text2 (3).
        /// </summary>
        AutoAttackNoText3 = 27,

        /// <summary>
        /// CriticalHit with sans-serif Text1 to the left of the Val1 (2).
        /// </summary>
        NamedCriticalHit2 = 28,

        /// <summary>
        /// Same as NamedCriticalHit with a green (cannot change) MP in condensed font to the right of Val1.
        /// Does a jiggle effect to the right on appearance.
        /// </summary>
        NamedCriticalHitWithMp = 29,

        /// <summary>
        /// Same as NamedCriticalHit with a yellow (cannot change) TP in condensed font to the right of Val1.
        /// Does a jiggle effect to the right on appearance.
        /// </summary>
        NamedCriticalHitWithTp = 30,

        /// <summary>
        /// Same as NamedIcon with sans-serif "has no effect!" to the right.
        /// </summary>
        NamedIconHasNoEffect = 31,

        /// <summary>
        /// Same as NamedIcon but Text1 is slightly faded. Used for buff expiration.
        /// </summary>
        NamedIconFaded = 32,

        /// <summary>
        /// Same as NamedIcon but Text1 is slightly faded (2).
        /// Used for buff expiration.
        /// </summary>
        NamedIconFaded2 = 33,

        /// <summary>
        /// Text1 in sans-serif font.
        /// </summary>
        Named = 34,

        /// <summary>
        /// Same as NamedIcon with sans-serif "(fully resisted)" to the right.
        /// </summary>
        NamedIconFullyResisted = 35,

        /// <summary>
        /// All caps serif 'INCAPACITATED!'.
        /// </summary>
        Incapacitated = 36,

        /// <summary>
        /// Text1 with sans-serif "(fully resisted)" to the right.
        /// </summary>
        NamedFullyResisted = 37,

        /// <summary>
        /// Text1 with sans-serif "has no effect!" to the right.
        /// </summary>
        NamedHasNoEffect = 38,

        /// <summary>
        /// AutoAttack with sans-serif Text1 to the left of the Val1 (3).
        /// </summary>
        NamedAttack3 = 39,

        /// <summary>
        /// Sans-serif Text1 next to serif Val1 with all caps condensed font MP with Text2 in sans-serif as subtitle (3).
        /// </summary>
        NamedMp3 = 40,

        /// <summary>
        /// Sans-serif Text1 next to serif Val1 with all caps condensed font TP with Text2 in sans-serif as subtitle (3).
        /// </summary>
        NamedTp3 = 41,

        /// <summary>
        /// Same as NamedIcon with serif "INVULNERABLE!" beneath the Text1.
        /// </summary>
        NamedIconInvulnerable = 42,

        /// <summary>
        /// All caps serif RESIST.
        /// </summary>
        Resist = 43,

        /// <summary>
        /// Same as NamedIcon but places the given icon in the item icon outline.
        /// </summary>
        NamedIconWithItemOutline = 44,

        /// <summary>
        /// AutoAttack with no Text2 (4).
        /// </summary>
        AutoAttackNoText4 = 45,

        /// <summary>
        /// Val1 in larger serif font with exclamation, with Text2 in sans-serif as subtitle (3).
        /// Does a bigger bounce effect on appearance.
        /// </summary>
        CriticalHit3 = 46,

        /// <summary>
        /// All caps serif REFLECT.
        /// </summary>
        Reflect = 47,

        /// <summary>
        /// All caps serif REFLECTED.
        /// </summary>
        Reflected = 48,

        /// <summary>
        /// Val1 in serif font, Text2 in sans-serif as subtitle (2).
        /// Does a bounce effect on appearance.
        /// </summary>
        DirectHit2 = 49,

        /// <summary>
        /// Val1 in larger serif font with exclamation, with Text2 in sans-serif as subtitle (4).
        /// Does a bigger bounce effect on appearance.
        /// </summary>
        CriticalHit4 = 50,

        /// <summary>
        /// Val1 in even larger serif font with 2 exclamations, Text2 in sans-serif as subtitle (2).
        /// Does a large bounce effect on appearance. Does not scroll up or down the screen.
        /// </summary>
        CriticalDirectHit2 = 51,
    }
}
