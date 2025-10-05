namespace Dalamud.Game.PlayerState;

/// <summary>
/// Represents a player's attribute.
/// </summary>
public enum PlayerAttribute
{
    /// <summary>
    /// Strength.
    /// </summary>
    /// <remarks>
    /// Affects physical damage dealt by gladiator's arms, marauder's arms, dark knight's arms, gunbreaker's arms, pugilist's arms, lancer's arms, samurai's arms, reaper's arms, thaumaturge's arms, arcanist's arms, red mage's arms, pictomancer's arms, conjurer's arms, astrologian's arms, sage's arms, and blue mage's arms.
    /// </remarks>
    Strength = 1,

    /// <summary>
    /// Dexterity.
    /// </summary>
    /// <remarks>
    /// Affects physical damage dealt by rogue's arms, viper's arms, archer's arms, machinist's arms, and dancer's arms.
    /// </remarks>
    Dexterity = 2,

    /// <summary>
    /// Vitality.
    /// </summary>
    /// <remarks>
    /// Affects maximum HP.
    /// </remarks>
    Vitality = 3,

    /// <summary>
    /// Intelligence.
    /// </summary>
    /// <remarks>
    /// Affects attack magic potency when role is DPS.
    /// </remarks>
    Intelligence = 4,

    /// <summary>
    /// Mind.
    /// </summary>
    /// <remarks>
    /// Affects healing magic potency. Also affects attack magic potency when role is Healer.
    /// </remarks>
    Mind = 5,

    /// <summary>
    /// Piety.
    /// </summary>
    /// <remarks>
    /// Affects MP regeneration. Regeneration rate is determined by piety. Only applicable when in battle and role is Healer.
    /// </remarks>
    Piety = 6,

    /// <summary>
    /// Health Points.
    /// </summary>
    HP = 7,

    /// <summary>
    /// Mana Points.
    /// </summary>
    MP = 8,

    /// <summary>
    /// Tactical Points.
    /// </summary>
    TP = 9,

    /// <summary>
    /// Gathering Point.
    /// </summary>
    GP = 10,

    /// <summary>
    /// Crafting Points.
    /// </summary>
    CP = 11,

    /// <summary>
    /// Physical Damage.
    /// </summary>
    PhysicalDamage = 12,

    /// <summary>
    /// Magic Damage.
    /// </summary>
    MagicDamage = 13,

    /// <summary>
    /// Delay.
    /// </summary>
    Delay = 14,

    /// <summary>
    /// Additional Effect.
    /// </summary>
    AdditionalEffect = 15,

    /// <summary>
    /// Attack Speed.
    /// </summary>
    AttackSpeed = 16,

    /// <summary>
    /// Block Rate.
    /// </summary>
    BlockRate = 17,

    /// <summary>
    /// Block Strength.
    /// </summary>
    BlockStrength = 18,

    /// <summary>
    /// Tenacity.
    /// </summary>
    /// <remarks>
    /// Affects the amount of physical and magic damage dealt and received, as well as HP restored. The higher the value, the more damage dealt, the more HP restored, and the less damage taken. Only applicable when role is Tank.
    /// </remarks>
    Tenacity = 19,

    /// <summary>
    /// Attack Power.
    /// </summary>
    /// <remarks>
    /// Affects amount of damage dealt by physical attacks. The higher the value, the more damage dealt.
    /// </remarks>
    AttackPower = 20,

    /// <summary>
    /// Defense.
    /// </summary>
    /// <remarks>
    /// Affects the amount of damage taken by physical attacks. The higher the value, the less damage taken.
    /// </remarks>
    Defense = 21,

    /// <summary>
    /// Direct Hit Rate.
    /// </summary>
    /// <remarks>
    /// Affects the rate at which your physical and magic attacks land direct hits, dealing slightly more damage than normal hits. The higher the value, the higher the frequency with which your hits will be direct. Higher values will also result in greater damage for actions which guarantee direct hits.
    /// </remarks>
    DirectHitRate = 22,

    /// <summary>
    /// Evasion.
    /// </summary>
    Evasion = 23,

    /// <summary>
    /// Magic Defense.
    /// </summary>
    /// <remarks>
    /// Affects the amount of damage taken by magic attacks. The higher the value, the less damage taken.
    /// </remarks>
    MagicDefense = 24,

    /// <summary>
    /// Critical Hit Power.
    /// </summary>
    CriticalHitPower = 25,

    /// <summary>
    /// Critical Hit Resilience.
    /// </summary>
    CriticalHitResilience = 26,

    /// <summary>
    /// Critical Hit.
    /// </summary>
    /// <remarks>
    /// Affects the amount of physical and magic damage dealt, as well as HP restored. The higher the value, the higher the frequency with which your hits will be critical/higher the potency of critical hits.
    /// </remarks>
    CriticalHit = 27,

    /// <summary>
    /// Critical Hit Evasion.
    /// </summary>
    CriticalHitEvasion = 28,

    /// <summary>
    /// Slashing Resistance.
    /// </summary>
    /// <remarks>
    /// Decreases damage done by slashing attacks.
    /// </remarks>
    SlashingResistance = 29,

    /// <summary>
    /// Piercing Resistance.
    /// </summary>
    /// <remarks>
    /// Decreases damage done by piercing attacks.
    /// </remarks>
    PiercingResistance = 30,

    /// <summary>
    /// Blunt Resistance.
    /// </summary>
    /// <remarks>
    /// Decreases damage done by blunt attacks.
    /// </remarks>
    BluntResistance = 31,

    /// <summary>
    /// Projectile Resistance.
    /// </summary>
    ProjectileResistance = 32,

    /// <summary>
    /// Attack Magic Potency.
    /// </summary>
    /// <remarks>
    /// Affects the amount of damage dealt by magic attacks.
    /// </remarks>
    AttackMagicPotency = 33,

    /// <summary>
    /// Healing Magic Potency.
    /// </summary>
    /// <remarks>
    /// Affects the amount of HP restored via healing magic.
    /// </remarks>
    HealingMagicPotency = 34,

    /// <summary>
    /// Enhancement Magic Potency.
    /// </summary>
    EnhancementMagicPotency = 35,

    /// <summary>
    /// Elemental Bonus.
    /// </summary>
    ElementalBonus = 36,

    /// <summary>
    /// Fire Resistance.
    /// </summary>
    /// <remarks>
    /// Decreases fire-aspected damage.
    /// </remarks>
    FireResistance = 37,

    /// <summary>
    /// Ice Resistance.
    /// </summary>
    /// <remarks>
    /// Decreases ice-aspected damage.
    /// </remarks>
    IceResistance = 38,

    /// <summary>
    /// Wind Resistance.
    /// </summary>
    /// <remarks>
    /// Decreases wind-aspected damage.
    /// </remarks>
    WindResistance = 39,

    /// <summary>
    /// Earth Resistance.
    /// </summary>
    /// <remarks>
    /// Decreases earth-aspected damage.
    /// </remarks>
    EarthResistance = 40,

    /// <summary>
    /// Lightning Resistance.
    /// </summary>
    /// <remarks>
    /// Decreases lightning-aspected damage.
    /// </remarks>
    LightningResistance = 41,

    /// <summary>
    /// Water Resistance.
    /// </summary>
    /// <remarks>
    /// Decreases water-aspected damage.
    /// </remarks>
    WaterResistance = 42,

    /// <summary>
    /// Magic Resistance.
    /// </summary>
    MagicResistance = 43,

    /// <summary>
    /// Determination.
    /// </summary>
    /// <remarks>
    /// Affects the amount of damage dealt by both physical and magic attacks, as well as the amount of HP restored by healing spells.
    /// </remarks>
    Determination = 44,

    /// <summary>
    /// Skill Speed.
    /// </summary>
    /// <remarks>
    /// Affects both the casting and recast timers, as well as the damage over time potency for weaponskills and auto-attacks. The higher the value, the shorter the timers/higher the potency.
    /// </remarks>
    SkillSpeed = 45,

    /// <summary>
    /// Spell Speed.
    /// </summary>
    /// <remarks>
    /// Affects both the casting and recast timers for spells. The higher the value, the shorter the timers. Also affects a spell's damage over time or healing over time potency.
    /// </remarks>
    SpellSpeed = 46,

    /// <summary>
    /// Haste.
    /// </summary>
    Haste = 47,

    /// <summary>
    /// Morale.
    /// </summary>
    /// <remarks>
    /// In PvP, replaces physical and magical defense in determining damage inflicted by other players. Also influences the amount of damage dealt to other players.
    /// </remarks>
    Morale = 48,

    /// <summary>
    /// Enmity.
    /// </summary>
    Enmity = 49,

    /// <summary>
    /// Enmity Reduction.
    /// </summary>
    EnmityReduction = 50,

    /// <summary>
    /// Desynthesis Skill Gain.
    /// </summary>
    DesynthesisSkillGain = 51,

    /// <summary>
    /// EXP Bonus.
    /// </summary>
    EXPBonus = 52,

    /// <summary>
    /// Regen.
    /// </summary>
    Regen = 53,

    /// <summary>
    /// Special Attribute.
    /// </summary>
    SpecialAttribute = 54,

    /// <summary>
    /// Main Attribute.
    /// </summary>
    MainAttribute = 55,

    /// <summary>
    /// Secondary Attribute.
    /// </summary>
    SecondaryAttribute = 56,

    /// <summary>
    /// Slow Resistance.
    /// </summary>
    /// <remarks>
    /// Shortens the duration of slow.
    /// </remarks>
    SlowResistance = 57,

    /// <summary>
    /// Petrification Resistance.
    /// </summary>
    PetrificationResistance = 58,

    /// <summary>
    /// Paralysis Resistance.
    /// </summary>
    ParalysisResistance = 59,

    /// <summary>
    /// Silence Resistance.
    /// </summary>
    /// <remarks>
    /// Shortens the duration of silence.
    /// </remarks>
    SilenceResistance = 60,

    /// <summary>
    /// Blind Resistance.
    /// </summary>
    /// <remarks>
    /// Shortens the duration of blind.
    /// </remarks>
    BlindResistance = 61,

    /// <summary>
    /// Poison Resistance.
    /// </summary>
    /// <remarks>
    /// Shortens the duration of poison.
    /// </remarks>
    PoisonResistance = 62,

    /// <summary>
    /// Stun Resistance.
    /// </summary>
    /// <remarks>
    /// Shortens the duration of stun.
    /// </remarks>
    StunResistance = 63,

    /// <summary>
    /// Sleep Resistance.
    /// </summary>
    /// <remarks>
    /// Shortens the duration of sleep.
    /// </remarks>
    SleepResistance = 64,

    /// <summary>
    /// Bind Resistance.
    /// </summary>
    /// <remarks>
    /// Shortens the duration of bind.
    /// </remarks>
    BindResistance = 65,

    /// <summary>
    /// Heavy Resistance.
    /// </summary>
    /// <remarks>
    /// Shortens the duration of heavy.
    /// </remarks>
    HeavyResistance = 66,

    /// <summary>
    /// Doom Resistance.
    /// </summary>
    DoomResistance = 67,

    /// <summary>
    /// Reduced Durability Loss.
    /// </summary>
    ReducedDurabilityLoss = 68,

    /// <summary>
    /// Increased Spiritbond Gain.
    /// </summary>
    IncreasedSpiritbondGain = 69,

    /// <summary>
    /// Craftsmanship.
    /// </summary>
    /// <remarks>
    /// Affects the amount of progress achieved in a single synthesis step.
    /// </remarks>
    Craftsmanship = 70,

    /// <summary>
    /// Control.
    /// </summary>
    /// <remarks>
    /// Affects the amount of quality improved in a single synthesis step.
    /// </remarks>
    Control = 71,

    /// <summary>
    /// Gathering.
    /// </summary>
    /// <remarks>
    /// Affects the rate at which items are gathered.
    /// </remarks>
    Gathering = 72,

    /// <summary>
    /// Perception.
    /// </summary>
    /// <remarks>
    /// Affects item yield when gathering as a botanist or miner, and the size of fish when fishing or spearfishing.
    /// </remarks>
    Perception = 73,
}
