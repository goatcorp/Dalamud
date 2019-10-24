using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XIVLauncher.Dalamud
{

    //CURRENT HIGHEST FLAG IS 45
    [Flags]
    public enum CustomComboPreset
    {
        None   = 0,

        // DRAGOON
        [CustomComboInfo("Coerthan Torment Combo", "Replace Coearthan Torment with its combo chain", 22)]
        DragoonCoerthanTormentCombo  = 1 << 0,

        [CustomComboInfo("Chaos Thrust Combo", "Replace Chaos Thrust with its combo chain", 22)]
        DragoonChaosThrustCombo = 1 << 1,

        [CustomComboInfo("Full Thrust Combo", "Replace Full Thrust with its combo chain", 22)]
        DragoonFullThrustCombo  = 1 << 2,

        // DARK KNIGHT
        [CustomComboInfo("Souleater Combo", "Replace Souleater with its combo chain", 32)]
        DarkSouleaterCombo = 1 << 3,

        [CustomComboInfo("Stalwart Soul Combo", "Replace Stalwart Soul with its combo chain", 32)]
        DarkStalwartSoulCombo = 1 << 4,

        // PALADIN
        [CustomComboInfo("Goring Blade Combo", "Replace Goring Blade with its combo chain", 19)]
        PaladinGoringBladeCombo = 1 << 5,

        [CustomComboInfo("Royal Authority Combo", "Replace Royal Authority with its combo chain", 19)]
        PaladinRoyalAuthorityCombo = 1 << 6,

        [CustomComboInfo("Prominence Combo", "Replace Prominence with its combo chain", 19)]
        PaladinProminenceCombo = 1 << 7,

        // WARRIOR
        [CustomComboInfo("Storms Path Combo", "Replace Storms Path with its combo chain", 21)]
        WarriorStormsPathCombo = 1 << 8,

        [CustomComboInfo("Storms Eye Combo", "Replace Storms Eye with its combo chain", 21)]
        WarriorStormsEyeCombo = 1 << 9,

        [CustomComboInfo("Mythril Tempest Combo", "Replace Mythril Tempest with its combo chain", 21)]
        WarriorMythrilTempestCombo = 1 << 10,

        // SAMURAI
        [CustomComboInfo("Yukikaze Combo", "Replace Yukikaze with its combo chain", 34)]
        SamuraiYukikazeCombo = 1 << 11,

        [CustomComboInfo("Gekko Combo", "Replace Gekko with its combo chain", 34)]
        SamuraiGekkoCombo = 1 << 12,

        [CustomComboInfo("Kasha Combo", "Replace Kasha with its combo chain", 34)]
        SamuraiKashaCombo = 1 << 13,

        [CustomComboInfo("Mangetsu Combo", "Replace Mangetsu with its combo chain", 34)]
        SamuraiMangetsuCombo = 1 << 14,

        [CustomComboInfo("Oka Combo", "Replace Oka with its combo chain", 34)]
        SamuraiOkaCombo = 1 << 15,


        // NINJA
        [CustomComboInfo("Shadow Fang Combo", "Replace Shadow Fang with its combo chain", 30)]
        NinjaShadowFangCombo = 1 << 16,

        [CustomComboInfo("Armor Crush Combo", "Replace Armor Crush with its combo chain", 30)]
        NinjaArmorCrushCombo = 1 << 17,

        [CustomComboInfo("Aeolian Edge Combo", "Replace Aeolian Edge with its combo chain", 30)]
        NinjaAeolianEdgeCombo = 1 << 18,

        [CustomComboInfo("Hakke Mujinsatsu Combo", "Replace Hakke Mujinsatsu with its combo chain", 30)]
        NinjaHakkeMujinsatsuCombo = 1 << 19,

        // GUNBREAKER
        [CustomComboInfo("Solid Barrel Combo", "Replace Solid Barrel with its combo chain", 37)]
        GunbreakerSolidBarrelCombo = 1 << 20,

        [CustomComboInfo("Gnashing Fang Combo", "Replace Gnashing Fang with its combo chain", 37)]
        GunbreakerGnashingFangCombo = 1 << 21,

        [CustomComboInfo("Demon Slaughter Combo", "Replace Demon Slaughter with its combo chain", 37)]
        GunbreakerDemonSlaughterCombo = 1 << 22,

        // MACHINIST
        [CustomComboInfo("Heated Clan Shot Combo/Heat", "Replace Heated Clan Shot with its combo chain or with Heat Blast when overheated.", 31)]
        MachinistHeatedClanShotFeature = 1 << 23,

        [CustomComboInfo("Spread Shot Heat", "Replace Spread Shot with Heat Blast when overheated.", 31)]
        MachinistSpreadShotFeature = 1 << 24,

        // BLACK MAGE
        [CustomComboInfo("Enochian Stance Switcher", "Change Enochian to Fire 4 or Blizzard 4 depending on stance.", 25)]
        BlackEnochianFeature = 1 << 25,

        [CustomComboInfo("Umbral Soul/Transpose Switcher", "Change between Umbral Soul and Transpose automatically.", 25)]
        BlackManaFeature = 1 << 26,

        // ASTROLOGIAN
        [CustomComboInfo("Cards on Draw", "Play your Astrologian Cards on Draw.", 33)]
        AstrologianCardsOnDrawFeature = 1 << 27,

        // SUMMONER
        [CustomComboInfo("Dreadwyrm Combiner", "Now comes with Dreadwyrm Trance, Deathflare, Summon Bahamut, Enkindle Bahamut, FBT, and Enkindle Phoenix.", 27)]
        SummonerDwtCombo = 1 << 28,

        [CustomComboInfo("Brand of Purgatory Combo", "Replaces Fountain of Fire with Brand of Purgatory when under the affect of Hellish Conduit.", 27)]
        SummonerBoPCombo = 1 << 38,

        [CustomComboInfo("ED Fester", "Change Energy Drain into Fester while you have Aetherflow stacks.", 27)]
        SummonerEDFesterCombo = 1 << 39,

        [CustomComboInfo("ES Painflare", "Change Energy Siphon into Painflare while you have Aetherflow stacks.", 27)]
        SummonerESPainflareCombo = 1 << 40,

        // SCHOLAR
        [CustomComboInfo("Seraph Fey Blessing/Consolation", "Change Fey Blessing into Consolation when Seraph is out.", 28)]
        ScholarSeraphConsolationFeature = 1 << 29,

        [CustomComboInfo("ED Aetherflow", "Change Energy Drain into Aetherflow when you have no more Aetherflow stacks.", 28)]
        ScholarEnergyDrainFeature = 1 << 37,

        // DANCER
        [CustomComboInfo("Standard Step Combo", "Standard Step on one button.", 38)]
        DancerStandardStepCombo = 1 << 30,

        [CustomComboInfo("Technical Step Combo", "Technical Step on one button.", 38)]
        DancerTechnicalStepCombo = 1 << 31,

        [CustomComboInfo("AoE GCD procs", "Replaces all AoE GCDs with their procced version when available.", 38)]
        DancerAoeGcdFeature = 1 << 32,

        [CustomComboInfo("Fan Dance Combos", "Change Fan Dance and Fan Dance 2 into Fan Dance 3 while flourishing.", 38)]
        DancerFanDanceCombo = 1 << 33,

        [CustomComboInfo("Fountain Combos", "Fountain changes into Fountain combo, prioritizing procs over combo, and Fountainfall over Reverse Cascade.", 38)]
        DancerFountainCombo = 1 << 34,

        // WHITE MAGE
        [CustomComboInfo("Solace into Misery", "Replaces Afflatus Solace with Afflatus Misery when Misery is ready to be used.", 24)]
        WhiteMageSolaceMiseryFeature = 1 << 35,

        [CustomComboInfo("Rapture into Misery", "Replaces Afflatus Rapture with Afflatus Misery when Misery is ready to be used.", 24)]
        WhiteMageRaptureMiseryFeature = 1 << 36,

        // BARD
        [CustomComboInfo("Wanderer's into Pitch Perfect", "Replaces Wanderer's Minuet with Pitch Perfect while in WM.", 23)]
        BardWandererPPFeature = 1 << 41,

        [CustomComboInfo("Heavy Shot into Straight Shot", "Replaces Heavy Shot/Burst Shot with Straight Shot/Refulgent Arrow when procced.", 23)]
        BardStraightShotUpgradeFeature = 1 << 42,

        // MONK
        [CustomComboInfo("Flank Positional Combo", "Replaces Snap Punch with flank positionals according to stance. When PB is active, prefer Snao (with non-max GL) > Twin (with no TS active) > Bootshine.", 20)]
        MonkFlankCombo = 1 << 43,

        [CustomComboInfo("Rear Positional Combo", "Replaces Demolish with rear positionals according to stance. When PB is active, prefer Demo (with non-max GL) > Bootshine.", 20)]
        MonkRearCombo = 1 << 44,

        [CustomComboInfo("Rockbreaker Combo", "Replaces Rockbreaker according to stance. When PB is active, prefer RB (with non-max GL) > Twin Snakes (with no TS active) > AotD.", 20)]
        MonkAoECombo = 1 << 45,

        // RED MAGE
        [CustomComboInfo("One-button White Magic", "Replaces Verstone with the following priority: Scorch > Verholy > Dual/Swiftcast Veraero > Verstone proc > Opener Veraero > Jolt.", 35)]
        RedMageWhiteMagicFeature = 1 << 46,

        [CustomComboInfo("One-button Black Magic", "Replaces Verfire with the following priority: Scorch > Verflare > Dual/Swiftcast Verthunder > Verfire proc > Opener Verthunder > Jolt.", 35)]
        RedMageBlackMagicFeature = 1 << 47,

        [CustomComboInfo("White Magic AoE", "Replaces Veraero 2 with Impact when Dualcast or Swiftcast are active.", 35)]
        RedMageWhiteAoECombo = 1 << 48,

        [CustomComboInfo("White Magic AoE", "Replaces Verthunder 2 with Impact when Dualcast or Swiftcast are active.", 35)]
        RedMageBlackAoECombo = 1 << 49,

        [CustomComboInfo("Redoublement combo", "Replaces Redoublement with its combo chain, following enchantment rules.", 35)]
        RedMageMeleeCombo = 1 << 50

    }

    public class CustomComboInfoAttribute : Attribute
    {
        internal CustomComboInfoAttribute(string fancyName, string description, byte classJob)
        {
            FancyName = fancyName;
            Description = description;
            ClassJob = classJob;
        }

        public string FancyName { get; }
        public string Description { get; }
        public byte ClassJob { get; }
    }
}
