namespace Dalamud.Game.Gui.FlyText;

/// <summary>
/// Enum of FlyTextKind values.
/// </summary>
public enum FlyTextKind : int
{
    /// <summary>
    /// Val1 in serif font, Text2 in sans-serif as subtitle.
    /// </summary>
    AutoAttackOrDot = 0,

    /// <summary>
    /// Val1 in serif font, Text2 in sans-serif as subtitle.
    /// Does a bounce effect on appearance.
    /// </summary>
    AutoAttackOrDotDh = 1,

    /// <summary>
    /// Val1 in larger serif font with exclamation, with Text2 in sans-serif as subtitle.
    /// Does a bigger bounce effect on appearance.
    /// </summary>
    AutoAttackOrDotCrit = 2,

    /// <summary>
    /// Val1 in even larger serif font with 2 exclamations, Text2 in sans-serif as subtitle.
    /// Does a large bounce effect on appearance. Does not scroll up or down the screen.
    /// </summary>
    AutoAttackOrDotCritDh = 3,

    /// <summary>
    /// Val1 in serif font, Text2 in sans-serif as subtitle with sans-serif Text1 to the left of the Val1.
    /// </summary>
    Damage = 4,

    /// <summary>
    /// Val1 in serif font, Text2 in sans-serif as subtitle with sans-serif Text1 to the left of the Val1.
    /// Does a bounce effect on appearance.
    /// </summary>
    DamageDh = 5,

    /// <summary>
    /// Val1 in larger serif font with exclamation, with Text2 in sans-serif as subtitle with sans-serif Text1 to the left of the Val1.
    /// Does a bigger bounce effect on appearance.
    /// </summary>
    DamageCrit = 6,

    /// <summary>
    /// Val1 in even larger serif font with 2 exclamations, Text2 in sans-serif as subtitle with sans-serif Text1 to the left of the Val1.
    /// Does a large bounce effect on appearance. Does not scroll up or down the screen.
    /// </summary>
    DamageCritDh = 7,

    /// <summary>
    /// The text changes to DODGE under certain circumstances.
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
    Buff = 12,

    /// <summary>
    /// Icon next to sans-serif Text1.
    /// </summary>
    Debuff = 13,

    /// <summary>
    /// Serif Val1 with all caps condensed font EXP with Text2 in sans-serif as subtitle.
    /// </summary>
    Exp = 14,

    /// <summary>
    /// Serif Val1 with all caps condensed font ISLAND EXP with Text2 in sans-serif as subtitle.
    /// </summary>
    IslandExp = 15,

    /// <summary>
    /// Val1 in serif font next to all caps condensed font Text1 with Text2 in sans-serif as subtitle.
    /// </summary>
    Dataset = 16,

    /// <summary>
    /// Val1 in serif font, Text2 in sans-serif as subtitle.
    /// </summary>
    Knowledge = 17,

    /// <summary>
    /// Val1 in serif font, Text2 in sans-serif as subtitle.
    /// </summary>
    PhantomExp = 18,

    /// <summary>
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font MP with Text2 in sans-serif as subtitle.
    /// </summary>
    MpDrain = 19,

    /// <summary>
    /// Currently not used by the game.
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font TP with Text2 in sans-serif as subtitle.
    /// </summary>
    NamedTp = 20,

    /// <summary>
    /// Val1 in serif font, Text2 in sans-serif as subtitle with sans-serif Text1 to the left of the Val1.
    /// </summary>
    Healing = 21,

    /// <summary>
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font MP with Text2 in sans-serif as subtitle.
    /// </summary>
    MpRegen = 22,

    /// <summary>
    /// Currently not used by the game.
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font TP with Text2 in sans-serif as subtitle.
    /// </summary>
    NamedTp2 = 23,

    /// <summary>
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font EP with Text2 in sans-serif as subtitle.
    /// </summary>
    EpRegen = 24,

    /// <summary>
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font CP with Text2 in sans-serif as subtitle.
    /// </summary>
    CpRegen = 25,

    /// <summary>
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font GP with Text2 in sans-serif as subtitle.
    /// </summary>
    GpRegen = 26,

    /// <summary>
    /// Displays nothing.
    /// </summary>
    None = 27,

    /// <summary>
    /// All caps serif INVULNERABLE.
    /// </summary>
    Invulnerable = 28,

    /// <summary>
    /// All caps sans-serif condensed font INTERRUPTED!
    /// Does a large bounce effect on appearance.
    /// Does not scroll up or down the screen.
    /// </summary>
    Interrupted = 29,

    /// <summary>
    /// Val1 in serif font.
    /// </summary>
    CraftingProgress = 30,

    /// <summary>
    /// Val1 in serif font.
    /// </summary>
    CraftingQuality = 31,

    /// <summary>
    /// Val1 in larger serif font with exclamation, with Text2 in sans-serif as subtitle. Does a bigger bounce effect on appearance.
    /// </summary>
    CraftingQualityCrit = 32,

    /// <summary>
    /// Currently not used by the game.
    /// Val1 in serif font.
    /// </summary>
    AutoAttackNoText3 = 33,

    /// <summary>
    /// CriticalHit with sans-serif Text1 to the left of the Val1 (2).
    /// </summary>
    HealingCrit = 34,

    /// <summary>
    /// Currently not used by the game.
    /// Same as DamageCrit with a MP in condensed font to the right of Val1.
    /// Does a jiggle effect to the right on appearance.
    /// </summary>
    NamedCriticalHitWithMp = 35,

    /// <summary>
    /// Currently not used by the game.
    /// Same as DamageCrit with a TP in condensed font to the right of Val1.
    /// Does a jiggle effect to the right on appearance.
    /// </summary>
    NamedCriticalHitWithTp = 36,

    /// <summary>
    /// Icon next to sans-serif Text1 with sans-serif "has no effect!" to the right.
    /// </summary>
    DebuffNoEffect = 37,

    /// <summary>
    /// Icon next to sans-serif slightly faded Text1.
    /// </summary>
    BuffFading = 38,

    /// <summary>
    /// Icon next to sans-serif slightly faded Text1.
    /// </summary>
    DebuffFading = 39,

    /// <summary>
    /// Text1 in sans-serif font.
    /// </summary>
    Named = 40,

    /// <summary>
    /// Icon next to sans-serif Text1 with sans-serif "(fully resisted)" to the right.
    /// </summary>
    DebuffResisted = 41,

    /// <summary>
    /// All caps serif 'INCAPACITATED!'.
    /// </summary>
    Incapacitated = 42,

    /// <summary>
    /// Text1 with sans-serif "(fully resisted)" to the right.
    /// </summary>
    FullyResisted = 43,

    /// <summary>
    /// Text1 with sans-serif "has no effect!" to the right.
    /// </summary>
    HasNoEffect = 44,

    /// <summary>
    /// Val1 in serif font, Text2 in sans-serif as subtitle with sans-serif Text1 to the left of the Val1.
    /// </summary>
    HpDrain = 45,

    /// <summary>
    /// Currently not used by the game.
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font MP with Text2 in sans-serif as subtitle.
    /// </summary>
    NamedMp3 = 46,

    /// <summary>
    /// Currently not used by the game.
    /// Sans-serif Text1 next to serif Val1 with all caps condensed font TP with Text2 in sans-serif as subtitle.
    /// </summary>
    NamedTp3 = 47,

    /// <summary>
    /// Icon next to sans-serif Text1 with serif "INVULNERABLE!" beneath the Text1.
    /// </summary>
    DebuffInvulnerable = 48,

    /// <summary>
    /// All caps serif RESIST.
    /// </summary>
    Resist = 49,

    /// <summary>
    /// Icon with an item icon outline next to sans-serif Text1.
    /// </summary>
    LootedItem = 50,

    /// <summary>
    /// Val1 in serif font.
    /// </summary>
    Collectability = 51,

    /// <summary>
    /// Val1 in larger serif font with exclamation, with Text2 in sans-serif as subtitle.
    /// Does a bigger bounce effect on appearance.
    /// </summary>
    CollectabilityCrit = 52,

    /// <summary>
    /// All caps serif REFLECT.
    /// </summary>
    Reflect = 53,

    /// <summary>
    /// All caps serif REFLECTED.
    /// </summary>
    Reflected = 54,

    /// <summary>
    /// Val1 in serif font, Text2 in sans-serif as subtitle.
    /// Does a bounce effect on appearance.
    /// </summary>
    CraftingQualityDh = 55,

    /// <summary>
    /// Currently not used by the game.
    /// Val1 in larger serif font with exclamation, with Text2 in sans-serif as subtitle.
    /// Does a bigger bounce effect on appearance.
    /// </summary>
    CriticalHit4 = 56,

    /// <summary>
    /// Val1 in even larger serif font with 2 exclamations, Text2 in sans-serif as subtitle.
    /// Does a large bounce effect on appearance. Does not scroll up or down the screen.
    /// </summary>
    CraftingQualityCritDh = 57,
}
