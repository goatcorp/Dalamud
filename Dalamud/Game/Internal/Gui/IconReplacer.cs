using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Structs.JobGauge;
using Dalamud.Hooking;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using XIVLauncher.Dalamud;

namespace Dalamud.Game.Internal.Gui {
    public class IconReplacer {
        public delegate ulong OnGetIconDelegate(byte param1, uint param2);
        public delegate ulong OnCheckIsIconReplaceableDelegate(int actionID);

        private Hook<OnGetIconDelegate> iconHook;
        private Hook<OnCheckIsIconReplaceableDelegate> checkerHook;

        private IconReplacerAddressResolver Address;

        private IntPtr comboTimer;
        private IntPtr lastComboMove;
        private IntPtr activeBuffArray = IntPtr.Zero;
        private IntPtr jobInfo;
        private IntPtr byteBase;
        private Dalamud dalamud;

        private Dictionary<uint, bool> UsedIDs;

        public unsafe IconReplacer(Dalamud dalamud, SigScanner scanner) {
            this.dalamud = dalamud;
            this.Address = new IconReplacerAddressResolver();
            this.Address.Setup(scanner);

            this.byteBase = scanner.Module.BaseAddress;
            this.comboTimer = byteBase + 0x1BB5B50;
            this.lastComboMove = byteBase + 0x1BB5B54;

            UsedIDs = new Dictionary<uint, bool>();

            PopulateDict();

            Log.Verbose("===== H O T B A R S =====");
            Log.Verbose("IsIconReplaceable address {IsIconReplaceable}", Address.IsIconReplaceable);
            Log.Verbose("GetIcon address {GetIcon}", Address.GetIcon);

            this.iconHook = new Hook<OnGetIconDelegate>(this.Address.GetIcon, new OnGetIconDelegate(GetIconDetour), this);
            this.checkerHook = new Hook<OnCheckIsIconReplaceableDelegate>(this.Address.IsIconReplaceable, new OnCheckIsIconReplaceableDelegate(CheckIsIconReplaceableDetour), this);
        }

        public void Enable() {
            this.iconHook.Enable();
            this.checkerHook.Enable();
        }

        public void Dispose() {
            this.iconHook.Dispose();
            this.checkerHook.Dispose();
        }

        // I hate this function. This is the dumbest function to exist in the game. Just return 1.
        // Determines which abilities are allowed to have their icons updated.
        private ulong CheckIsIconReplaceableDetour(int actionID) {
            return 1;
        }

        /// <summary>
        ///     Replace an ability with another ability
        ///     actionID is the original ability to be "used"
        ///     Return either actionID (itself) or a new Action table ID as the 
        ///     ability to take its place.
        ///     I tend to make the "combo chain" button be the last move in the combo
        ///     For example, Souleater combo on DRK happens by dragging Souleater
        ///     onto your bar and mashing it.
        /// </summary>
        private ulong GetIconDetour(byte self, uint actionID) {

            // TODO: More jobs, level checking for everything.

            // Check if player is loaded in by trying to get their buffs.
            // If not, skip everything until we are (game will crash cause I'm lazy).
            /*
            if (activeBuffArray == IntPtr.Zero) {
                try {
                    activeBuffArray = FindBuffAddress();
                    Log.Verbose("ActiveBuffArray address: {ActiveBuffArray}", activeBuffArray);
                }
                catch (Exception e) {
                    activeBuffArray = IntPtr.Zero;
                    return this.iconHook.Original(self, actionID);
                }
            }
            */

            if (!this.UsedIDs.ContainsKey(actionID)) return actionID;

            // TODO: this is currently broken
            // As it stands, don't rely on localCharacter.level for anything.
            var localPlayer = this.dalamud.ClientState.LocalPlayer;

            // Don't clutter the spaghetti any worse than it already is.
            var lastMove = Marshal.ReadInt32(this.lastComboMove);
            var comboTime = Marshal.ReadInt32(this.comboTimer);
            var level = 80;

            // DRAGOON
            // TODO: Jump/High Jump into Mirage Dive

            // Replace Coerthan Torment with Coerthan Torment combo chain
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonCoerthanTormentCombo)) {
                if (actionID == 16477) {
                    if (comboTime > 0) {
                        if (lastMove == 86 && level >= 62) return 7397;
                        if (lastMove == 7397 && level >= 72) return 16477;
                    }
                    return 86;
                }
            }
            

            // Replace Chaos Thrust with the Chaos Thrust combo chain
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonChaosThrustCombo)) {
                if (actionID == 88) {
                    if (comboTime > 0) {
                        if ((lastMove == 75 || lastMove == 16479) && level >= 18) return 87;
                        if (lastMove == 87 && level >= 50) return 88;
                    }
                    if (SearchBuffArray(802) && level >= 56) return 3554;
                    if (SearchBuffArray(803) && level >= 58) return 3556;
                    if (SearchBuffArray(1863) && level >= 76) return 16479;

                    return 75;
                }
            }
            

            // Replace Full Thrust with the Full Thrust combo chain
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonFullThrustCombo)) {
                if (actionID == 84) {
                    if (comboTime > 0) {
                        if ((lastMove == 75 || lastMove == 16479) && level >= 4) return 78;
                        if (lastMove == 78 && level >= 26) return 84;
                    }
                    if (SearchBuffArray(802) && level >= 56) return 3554;
                    if (SearchBuffArray(803) && level >= 58) return 3556;
                    if (SearchBuffArray(1863) && level >= 76) return 16479;

                    return 75;
                }
            }

            // DARK KNIGHT

            // Replace Souleater with Souleater combo chain
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DarkSouleaterCombo)) {
                if (actionID == 3632) {
                    if (comboTime > 0) {
                        if (lastMove == 3617 && level >= 2) return 3623;
                        if (lastMove == 3623 && level >= 26) return 3632;
                    }

                    return 3617;
                }
            }

            // Replace Stalwart Soul with Stalwart Soul combo chain
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DarkStalwartSoulCombo)) {
                if (actionID == 16468) {
                    if (comboTime > 0) {
                        if (lastMove == 3621 && level >= 72) return 16468;
                    }

                    return 3621;
                }
            }

            // PALADIN

            // Replace Goring Blade with Goring Blade combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinGoringBladeCombo)) {
                if (actionID == 3538) {
                    if (comboTime > 0) {
                        if (lastMove == 9 && level >= 4) return 15;
                        if (lastMove == 15 && level >= 54) return 3538;
                    }

                    return 9;
                }
            }

            // Replace Royal Authority with Royal Authority combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinRoyalAuthorityCombo)) {
                if (actionID == 3539) {
                    if (comboTime > 0) {
                        if (lastMove == 9 && level >= 4) return 15;
                        if (lastMove == 15) {
                            if (level >= 60) return 3539;
                            if (level >= 26) return 21;
                        }
                    }

                    return 9;
                }
            }

            // Replace Prominence with Prominence combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinProminenceCombo)) {
                if (actionID == 16457) {
                    if (comboTime > 0) {
                        if (lastMove == 7381 && level >= 40) return 16457;
                    }

                    return 7381;
                }
            }

            // WARRIOR

            // Replace Storm's Path with Storm's Path combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.WarriorStormsPathCombo)) {
                if (actionID == 42) {
                    if (comboTime > 0) {
                        if (lastMove == 31 && level >= 4) return 37;
                        if (lastMove == 37 && level >= 26) return 42;
                    }

                    return 31;
                }
            }

            // Replace Storm's Eye with Storm's Eye combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.WarriorStormsEyeCombo)) {
                if (actionID == 45) {
                    if (comboTime > 0) {
                        if (lastMove == 31 && level >= 4) return 37;
                        if (lastMove == 37 && level >= 50) return 45;
                    }
                    return 31;
                }
            }

            // Replace Mythril Tempest with Mythril Tempest combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.WarriorMythrilTempestCombo)) {
                if (actionID == 16462) {
                    if (comboTime > 0) {
                        if (lastMove == 41 && level >= 40) return 16462;
                    }
                    return 41;
                }
            }

            // SAMURAI

            // Replace Yukikaze with Yukikaze combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiYukikazeCombo)) {
                if (actionID == 7480) {
                    if (SearchBuffArray(1233)) return 7480;
                    if (comboTime > 0) {
                        if (lastMove == 7477 && level >= 50) return 7480;
                    }
                    return 7477;
                }
            }

            // Replace Gekko with Gekko combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiGekkoCombo)) {
                if (actionID == 7481) {
                    if (SearchBuffArray(1233)) return 7481;
                    if (comboTime > 0) {
                        if (lastMove == 7477 && level >= 4) return 7478;
                        if (lastMove == 7478 && level >= 30) return 7481;
                    }
                    return 7477;
                }
            }

            // Replace Kasha with Kasha combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiKashaCombo)) {
                if (actionID == 7482) {
                    if (SearchBuffArray(1233)) return 7482;
                    if (comboTime > 0) {
                        if (lastMove == 7477 && level >= 18) return 7479;
                        if (lastMove == 7479 && level >= 40) return 7482;
                    }
                    return 7477;
                }
            }

            // Replace Mangetsu with Mangetsu combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiMangetsuCombo)) {
                if (actionID == 7484) {
                    if (SearchBuffArray(1233)) return 7484;
                    if (comboTime > 0) {
                        if (lastMove == 7483 && level >= 35) return 7484;
                    }
                    return 7483;
                }
            }

            // Replace Oka with Oka combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiOkaCombo)) {
                if (actionID == 7485) {
                    if (SearchBuffArray(1233)) return 7485;
                    if (comboTime > 0) {
                        if (lastMove == 7483 && level >= 45) return 7485;
                    }
                    return 7483;
                }
            }

            // NINJA

            // Replace Armor Crush with Armor Crush combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.NinjaArmorCrushCombo)) {
                if (actionID == 3563) {
                    if (comboTime > 0) {
                        if (lastMove == 2240 && level >= 4) return 2242;
                        if (lastMove == 2242 && level >= 54) return 3563;
                    }
                    return 2240;
                }
            }

            // Replace Aeolian Edge with Aeolian Edge combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.NinjaAeolianEdgeCombo)) {
                if (actionID == 2255) {
                    if (comboTime > 0) {
                        if (lastMove == 2240 && level >= 4) return 2242;
                        if (lastMove == 2242 && level >= 26) return 2255;
                    }
                    return 2240;
                }
            }

            // Replace Hakke Mujinsatsu with Hakke Mujinsatsu combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.NinjaHakkeMujinsatsuCombo)) {
                if (actionID == 16488) {
                    if (comboTime > 0) {
                        if (lastMove == 2254 && level >= 52) return 16488;
                    }
                    return 2254;
                }
            }

            // GUNBREAKER

            // Replace Solid Barrel with Solid Barrel combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.GunbreakerSolidBarrelCombo)) {
                if (actionID == 16145) {
                    if (comboTime > 0) {
                        if (lastMove == 16137 && level >= 4) return 16139;
                        if (lastMove == 16139 && level >= 26) return 16145;
                    }
                    return 16137;
                }
            }

            // Replace Gnashing Fang with Gnashing Fang combo
            // TODO: Potentially add Contuation moves as well?
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.GunbreakerGnashingFangCombo)) {
                if (actionID == 16146) {
                    byte ammoComboState = this.dalamud.ClientState.JobGauges.Get<GNBGauge>().AmmoComboStepNumber;
                    if (ammoComboState == 1) return 16147;
                    if (ammoComboState == 2) return 16150;
                    return 16146;
                }
            }

            // Replace Demon Slaughter with Demon Slaughter combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.GunbreakerDemonSlaughterCombo)) {
                if (actionID == 16149) {
                    if (comboTime > 0) {
                        if (lastMove == 16141 && level >= 40) return 16149;
                    }
                    return 16141;
                }
            }

            // MACHINIST

            // Replace Heated Clean Shot with Heated Clean Shot combo
            // Or with Heat Blast when overheated.
            // For some reason the shots use their unheated IDs as combo moves
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.MachinistHeatedClanShotFeature)) {
                if (actionID == 7413) {
                    MCHGauge gauge = this.dalamud.ClientState.JobGauges.Get<MCHGauge>();
                    // End overheat slightly early to prevent eager button mashing clipping your gcd with a fake 6th HB.
                    if (gauge.IsOverheated() && level >= 35 && gauge.OverheatTimeRemaining > 30) return 7410;
                    if (comboTime > 0) {
                        if (lastMove == 2866) {
                            if (level >= 60) return 7412;
                            if (level >= 2) return 2868;
                        }
                        if (lastMove == 2868) {
                            if (level >= 64) return 7413;
                            if (level >= 26) return 2873;
                        }
                    }
                    return 7411;
                }
            }

            // Replace Spread Shot with Auto Crossbow when overheated.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.MachinistSpreadShotFeature)) {
                if (actionID == 2870) {
                    if (this.dalamud.ClientState.JobGauges.Get<MCHGauge>().IsOverheated() && level >= 52) return 16497;
                    return 2870;
                }
            }

            // BLACK MAGE

            // Enochian changes to B4 or F4 depending on stance.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.BlackEnochianFeature)) {
                if (actionID == 3575) {
                    BLMGauge jobInfo = this.dalamud.ClientState.JobGauges.Get<BLMGauge>();
                    if (jobInfo.IsEnoActive()) {
                        if (jobInfo.InUmbralIce() && level >= 58) return 3576;
                        if (level >= 60) return 3577;
                    }
                    return 3575;
                }
            }

            // Umbral Soul and Transpose
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.BlackManaFeature)) {
                if (actionID == 149) {
                    BLMGauge gauge = this.dalamud.ClientState.JobGauges.Get<BLMGauge>();
                    if (gauge.InUmbralIce() && gauge.IsEnoActive() && level >= 76) return 16506;
                    return 149;
                }
            }

            // ASTROLOGIAN

            // Make cards on the same button as play
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.AstrologianCardsOnDrawFeature)) {
                if (actionID == 17055) {
                    ASTGauge gauge = this.dalamud.ClientState.JobGauges.Get<ASTGauge>();
                    switch (gauge.DrawnCard()) {
                        case CardType.BALANCE:
                            return 4401;
                        case CardType.BOLE:
                            return 4404;
                        case CardType.ARROW:
                            return 4402;
                        case CardType.SPEAR:
                            return 4403;
                        case CardType.EWER:
                            return 4405;
                        case CardType.SPIRE:
                            return 4406;
                        /*
                        case CardType.LORD:
                            return 7444;
                        case CardType.LADY:
                            return 7445;
                        */
                        default:
                            return 3590;
                    }
                }
            }

            // SUMMONER

            // DWT changes. 
            // Now contains DWT, Deathflare, Summon Bahamut, Enkindle Bahamut, FBT, and Enkindle Phoenix.
            // What a monster of a button.
            /*
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SummonerDwtCombo)) {
                if (actionID == 3581) {
                    SMNGauge gauge = this.dalamud.ClientState.JobGauges.Get<SMNGauge>();
                    if (gauge.TimerRemaining > 0) {
                        if (gauge.ReturnSummon > 0) {
                            if (gauge.IsPhoenixReady()) return 16516;
                            return 7429;
                        }
                        if (level >= 60) return 3582;
                    }
                    else {
                        if (gauge.IsBahamutReady()) return 7427;
                        if (gauge.IsPhoenixReady()) return 16513;
                        return 3581;
                    }
                }
            }
            */

            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SummonerDemiCombo)) {
                // Replace Deathflare with demi enkindles
                if (actionID == 3582) {
                    SMNGauge gauge = this.dalamud.ClientState.JobGauges.Get<SMNGauge>();
                    if (gauge.IsPhoenixReady()) return 16516;
                    if (gauge.TimerRemaining > 0 && gauge.ReturnSummon != SummonPet.NONE) return 7429;
                    return 3582;
                }

                //Replace DWT with demi summons
                if (actionID == 3581) {
                    SMNGauge gauge = this.dalamud.ClientState.JobGauges.Get<SMNGauge>();
                    if (gauge.IsBahamutReady()) return 7427;
                    if (gauge.IsPhoenixReady() || 
                        (gauge.TimerRemaining > 0 && gauge.ReturnSummon != SummonPet.NONE)) return 16513;
                    return 3581;
                }
            }

            // Ruin 1 now upgrades to Brand of Purgatory in addition to Ruin 3 and Fountain of Fire
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SummonerBoPCombo)) {
                if (actionID == 163) {
                    SMNGauge gauge = this.dalamud.ClientState.JobGauges.Get<SMNGauge>();
                    if (gauge.TimerRemaining > 0) {
                        if (gauge.IsPhoenixReady()) {
                            if (SearchBuffArray(1867)) {
                                return 16515;
                            }
                            return 16514;
                        }
                    }
                    if (level >= 54) return 3579;
                    return 163;
                }
            }

            // Change Energy Drain into Fester
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SummonerEDFesterCombo)) {
                if (actionID == 16508) {
                    if (this.dalamud.ClientState.JobGauges.Get<SMNGauge>().HasAetherflowStacks())
                        return 181;
                    return 16508;
                }
            }

            //Change Energy Siphon into Painflare
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SummonerESPainflareCombo)) {
                if (actionID == 16510) {
                    if (this.dalamud.ClientState.JobGauges.Get<SMNGauge>().HasAetherflowStacks())
                        if (level >= 52) return 3578;
                    return 16510;
                }
            }

            // SCHOLAR

            // Change Fey Blessing into Consolation when Seraph is out.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.ScholarSeraphConsolationFeature)) {
                if (actionID == 16543) {
                    if (this.dalamud.ClientState.JobGauges.Get<SCHGauge>().SeraphTimer > 0) return 16546;
                    return 16543;
                }
            }

            // Change Energy Drain into Aetherflow when you have no more Aetherflow stacks.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.ScholarEnergyDrainFeature)) {
                if (actionID == 167) {
                    if (this.dalamud.ClientState.JobGauges.Get<SCHGauge>().NumAetherflowStacks == 0) return 166;
                    return 167;
                }
            }

            // DANCER

            /*

            // Standard Step is one button.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerStandardStepCombo)) {
                if (actionID == 15997) {
                    DNCGauge gauge = this.dalamud.ClientState.JobGauges.Get<DNCGauge>();
                    if (gauge.IsDancing()) {
                        if (gauge.NumCompleteSteps == 2) {
                            return 16192;
                        }
                        else {
                            // C# can't implicitly cast from int to ulong.
                            return gauge.NextStep();
                        }
                    }
                    return 15997;
                }
            }

            // Technical Step is one button.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerTechnicalStepCombo)) {
                if (actionID == 15998) {
                    DNCGauge gauge = this.dalamud.ClientState.JobGauges.Get<DNCGauge>();
                    if (gauge.IsDancing()) {
                        if (gauge.NumCompleteSteps == 4) {
                            return 16196;
                        }
                        else {
                            // C# can't implicitly cast from int to ulong.
                            return gauge.NextStep();
                        }
                    }
                    return 15998;
                }
            }

            // Fountain changes into Fountain combo, prioritizing procs over combo,
            // and Fountainfall over Reverse Cascade.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerFountainCombo)) {
                if (actionID == 15990) {
                    if (this.dalamud.ClientState.JobGauges.Get<DNCGauge>().IsDancing()) return 15999;
                    if (SearchBuffArray(1815)) return 15992;
                    if (SearchBuffArray(1814)) return 15991;
                    if (comboTime > 0) {
                        if (lastMove == 15989 && level >= 2) return 15990;
                    }
                    return 15989;
                }
            }

            */

            // AoE GCDs are split into two buttons, because priority matters
            // differently in different single-target moments. Thanks yoship.
            // Replaces each GCD with its procced version.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerAoeGcdFeature)) {
                if (actionID == 15994) {
                    if (SearchBuffArray(1817)) return 15996;
                    return 15994;
                }

                if (actionID == 15993) {
                    if (SearchBuffArray(1816)) return 15995;
                    return 15993;
                }
            }

            // Fan Dance changes into Fan Dance 3 while flourishing.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerFanDanceCombo)) {
                if (actionID == 16007) {
                    if (SearchBuffArray(1820)) return 16009;

                    return 16007;
                }

                // Fan Dance 2 changes into Fan Dance 3 while flourishing.
                if (actionID == 16008) {
                    if (SearchBuffArray(1820)) return 16009;
                    return 16008;
                }
            }

            // WHM

            // Replace Solace with Misery when full blood lily
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.WhiteMageSolaceMiseryFeature)) {
                if (actionID == 16531) {
                    if (this.dalamud.ClientState.JobGauges.Get<WHMGauge>().NumBloodLily == 3)
                        return 16535;
                    return 16531;
                }
            }

            // Replace Solace with Misery when full blood lily
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.WhiteMageRaptureMiseryFeature)) {
                if (actionID == 16534) {
                    if (this.dalamud.ClientState.JobGauges.Get<WHMGauge>().NumBloodLily == 3)
                        return 16535;
                    return 16534;
                }
            }

            // BARD

            // Replace Wanderer's Minuet with PP when in WM.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.BardWandererPPFeature)) {
                if (actionID == 3559) {
                    if (this.dalamud.ClientState.JobGauges.Get<BRDGauge>().ActiveSong == CurrentSong.WANDERER) {
                        return 7404;
                    }
                    return 3559;
                }
            }

            // Replace HS/BS with SS/RA when procced.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.BardStraightShotUpgradeFeature)) {
                if (actionID == 97) {
                    if (SearchBuffArray(122)) {
                        if (level >= 70) return 7409;
                        return 98;
                    }
                    if (level >= 76) return 16495;
                    return 97;
                }
            }

            // MONK

            /*

            // Replace Snap Punch with flank positional combo.
            // During PB, Snap (with sub-max stacks) > Twin (with no active Twin) > DK
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.MonkFlankCombo)) {
                if (actionID == 56) {
                    if (SearchBuffArray(110)) {
                        MNKGauge gauge = this.dalamud.ClientState.JobGauges.Get<MNKGauge>();
                        if ((gauge.NumGLStacks < 3 && level < 76) || SearchBuffArray(103)) {
                            return 56;
                        }
                        else if (gauge.NumGLStacks < 4 && level >= 76 && SearchBuffArray(105)) {
                            return 56;
                        }
                        else if (!SearchBuffArray(101)) return 61;
                        else return 74;
                    }
                    else {
                        if (SearchBuffArray(107) && level >= 50) return 74;
                        if (SearchBuffArray(108) && level >= 18) return 61;
                        if (SearchBuffArray(109) && level >= 6) return 56;
                        return 74;
                    }
                }
            }

            // Replace Demolish with rear positional combo.
            // During PB, Demo (with sub-max stacks) > Bootshine.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.MonkRearCombo)) {
                if (actionID == 66) {
                    if (SearchBuffArray(110)) {
                        MNKGauge gauge = this.dalamud.ClientState.JobGauges.Get<MNKGauge>();
                        if ((gauge.NumGLStacks < 3 && level < 76) || SearchBuffArray(103)) {
                            return 66;
                        }
                        else if (gauge.NumGLStacks < 4 && level >= 76 && SearchBuffArray(105)) {
                            return 66;
                        }
                        else return 53;
                    }
                    else {
                        if (SearchBuffArray(107)) return 53;
                        if (SearchBuffArray(108) && level >= 4) return 54;
                        if (SearchBuffArray(109) && level >= 30) return 66;
                        return 53;
                    }
                }
            }

            // Replace Rockbreaker with AoE combo.
            // During PB, RB (with sub-max stacks) > Twin Snakes (if not applied) > RB.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.MonkAoECombo)) {
                if (actionID == 70) {
                    if (SearchBuffArray(110)) {
                        MNKGauge gauge = this.dalamud.ClientState.JobGauges.Get<MNKGauge>();
                        if ((gauge.NumGLStacks < 3 && level < 76) || SearchBuffArray(103)) {
                            return 70;
                        }
                        else if (gauge.NumGLStacks < 4 && level >= 76 && SearchBuffArray(105)) {
                            return 70;
                        }
                        else if (!SearchBuffArray(101)) return 61;
                        else return 70;
                    }
                    else {
                        if (SearchBuffArray(107)) return 62;
                        if (SearchBuffArray(108)) {
                            if (!SearchBuffArray(101)) return 61;
                            if (level >= 45) return 16473;
                        }
                        if (SearchBuffArray(109) && level >= 30) return 70;
                        return 62;
                    }
                }
            }

            */

            // RED MAGE

            /*

            // Replace Verstone with White Magic spells. Priority order:
            // Scorch > Verholy > Verstone = Veraero (with Dualcast active) > opener Veraero > Jolt
            // Impact is not the first available spell to allow for precast openers.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageWhiteMagicFeature)) {
                if (actionID == 7511) {
                    if ((lastMove == 7526 || lastMove == 7525) && level >= 80) return 16530;
                    if (lastMove == 7529 && level >= 70) return 7526;
                    if ((SearchBuffArray(1249) || SearchBuffArray(167)) && level >= 10) return 7507;
                    if (SearchBuffArray(1235) && level >= 30) return 7511;
                    RDMGauge gauge = this.dalamud.ClientState.JobGauges.Get<RDMGauge>();
                    if ((gauge.BlackGauge == 0 && gauge.WhiteGauge == 0) && level >= 10) return 7507;
                    if (level >= 62) return 7524;
                    return 7503;
                }
            }

            // Replace Verfire with Black Magic spells. Priority order:
            // Scorch > Verflare> Verfire = Verthunder (with Dualcast active) > opener Verthunder > Jolt
            // Impact is not the first available spell to allow for precast openers.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageBlackMagicFeature)) {
                if (actionID == 7510) {
                    if ((lastMove == 7526 || lastMove == 7525) && level >= 80) return 16530;
                    if (lastMove == 7529 && level >= 68) return 7525;
                    if ((SearchBuffArray(1249) || SearchBuffArray(167)) && level >= 4) return 7505;
                    if (SearchBuffArray(1234) && level >= 26) return 7510;
                    RDMGauge gauge = this.dalamud.ClientState.JobGauges.Get<RDMGauge>();
                    if ((gauge.BlackGauge == 0 && gauge.WhiteGauge == 0) && level >= 4) return 7505;
                    if (level >= 62) return 7524;
                    return 7503;
                }
            }
            */
            // Replace Veraero/thunder 2 with Impact when Dualcast is active
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageAoECombo)) {
                if (actionID == 16525) {
                    if (level >= 66 && (SearchBuffArray(1249) || SearchBuffArray(167))) return 16526;
                    return 16525;
                }

                if (actionID == 16524) {
                    if (level >= 66 && (SearchBuffArray(1249) || SearchBuffArray(167))) return 16526;
                    return 16524;
                }
            }

            

            // Replace Redoublement with Redoublement combo, Enchanted if possible.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageMeleeCombo)) {
                if (actionID == 7516) {
                    RDMGauge gauge = this.dalamud.ClientState.JobGauges.Get<RDMGauge>();
                    if ((lastMove == 7504 || lastMove == 7527) && level >= 35) {
                        if (gauge.BlackGauge >= 25 && gauge.WhiteGauge >= 25) return 7528;
                        return 7512;
                    }
                    if (lastMove == 7512 && level >= 50) {
                        if (gauge.BlackGauge >= 25 && gauge.WhiteGauge >= 25) return 7529;
                        return 7516;
                    }
                    if (gauge.BlackGauge >= 30 && gauge.WhiteGauge >= 30) return 7527;
                    return 7516;
                }
            }

            return this.iconHook.Original(self, actionID);
        }

        private bool SearchBuffArray(short needle) {/*
            for (int i = 0; i < 60; i++) {
                if (Marshal.ReadInt16(activeBuffArray + 4 * i) == needle) return true;
            }
            */
            return false;
        }

        private unsafe delegate int* getArray(long* address);

        private unsafe IntPtr FindBuffAddress() {
            IntPtr randomAddress = byteBase + 0x1c04be0;
            IntPtr num = Marshal.ReadIntPtr(randomAddress);
            IntPtr step2 = (IntPtr)(Marshal.ReadInt64(num) + 0x248);
            IntPtr step3 = Marshal.ReadIntPtr(step2);
            var callback = Marshal.GetDelegateForFunctionPointer<getArray>(step3);
            return (IntPtr)callback((long*)num);
        }

        private void PopulateDict() {

            UsedIDs.Add(16477, true);
            UsedIDs.Add(88, true);
            UsedIDs.Add(84, true);
            UsedIDs.Add(3632, true);
            UsedIDs.Add(16468, true);
            UsedIDs.Add(3538, true);
            UsedIDs.Add(3539, true);
            UsedIDs.Add(16457, true);
            UsedIDs.Add(42, true);
            UsedIDs.Add(45, true);
            UsedIDs.Add(16462, true);
            UsedIDs.Add(7480, true);
            UsedIDs.Add(7481, true);
            UsedIDs.Add(7482, true);
            UsedIDs.Add(7484, true);
            UsedIDs.Add(7485, true);
            UsedIDs.Add(3563, true);
            UsedIDs.Add(2255, true);
            UsedIDs.Add(16488, true);
            UsedIDs.Add(16145, true);
            UsedIDs.Add(16146, true);
            UsedIDs.Add(16149, true);
            UsedIDs.Add(7413, true);
            UsedIDs.Add(2870, true);
            UsedIDs.Add(3575, true);
            UsedIDs.Add(149, true);
            UsedIDs.Add(17055, true);
            UsedIDs.Add(3582, true);
            UsedIDs.Add(3581, true);
            UsedIDs.Add(163, true);
            UsedIDs.Add(16508, true);
            UsedIDs.Add(16510, true);
            UsedIDs.Add(16543, true);
            UsedIDs.Add(167, true);
            UsedIDs.Add(15994, true);
            UsedIDs.Add(15993, true);
            UsedIDs.Add(16007, true);
            UsedIDs.Add(16008, true);
            UsedIDs.Add(16531, true);
            UsedIDs.Add(16534, true);
            UsedIDs.Add(3559, true);
            UsedIDs.Add(97, true);
            UsedIDs.Add(16525, true);
            UsedIDs.Add(16524, true);
            UsedIDs.Add(7516, true);
            UsedIDs.Add(0x3e75, true);
            UsedIDs.Add(0x3e76, true);
            UsedIDs.Add(0x3e77, true);
            UsedIDs.Add(0x3e78, true);
            UsedIDs.Add(0x3e7d, true);
            UsedIDs.Add(0x3e7e, true);
            UsedIDs.Add(0x3e86, true);
            UsedIDs.Add(0x3f10, true);
            UsedIDs.Add(0x3f25, true);
            UsedIDs.Add(0x3f1b, true);
            UsedIDs.Add(0x3f1c, true);
            UsedIDs.Add(0x3f1d, true);
            UsedIDs.Add(0x3f1e, true);
            UsedIDs.Add(0x451f, true);
            UsedIDs.Add(0x42ff, true);
            UsedIDs.Add(0x4300, true);
            UsedIDs.Add(0x49d4, true);
            UsedIDs.Add(0x49d5, true);
            UsedIDs.Add(0x49e9, true);
            UsedIDs.Add(0x49ea, true);
            UsedIDs.Add(0x49f4, true);
            UsedIDs.Add(0x49f7, true);
            UsedIDs.Add(0x49f9, true);
            UsedIDs.Add(0x4a06, true);
            UsedIDs.Add(0x4a31, true);
            UsedIDs.Add(0x4a32, true);
            UsedIDs.Add(0x4a35, true);
            UsedIDs.Add(0x4792, true);
            UsedIDs.Add(0x452f, true);
            UsedIDs.Add(0x453f, true);
            UsedIDs.Add(0x454c, true);
            UsedIDs.Add(0x455c, true);
            UsedIDs.Add(0x455d, true);
            UsedIDs.Add(0x4561, true);
            UsedIDs.Add(0x4565, true);
            UsedIDs.Add(0x4566, true);
            UsedIDs.Add(0x45a0, true);
            UsedIDs.Add(0x45c8, true);
            UsedIDs.Add(0x45c9, true);
            UsedIDs.Add(0x45cd, true);
            UsedIDs.Add(0x4197, true);
            UsedIDs.Add(0x4199, true);
            UsedIDs.Add(0x419b, true);
            UsedIDs.Add(0x419d, true);
            UsedIDs.Add(0x419f, true);
            UsedIDs.Add(0x4198, true);
            UsedIDs.Add(0x419a, true);
            UsedIDs.Add(0x419c, true);
            UsedIDs.Add(0x419e, true);
            UsedIDs.Add(0x41a0, true);
            UsedIDs.Add(0x41a1, true);
            UsedIDs.Add(0x41a2, true);
            UsedIDs.Add(0x41a3, true);
            UsedIDs.Add(0x417e, true);
            UsedIDs.Add(0x404f, true);
            UsedIDs.Add(0x4051, true);
            UsedIDs.Add(0x4052, true);
            UsedIDs.Add(0x4055, true);
            UsedIDs.Add(0x4053, true);
            UsedIDs.Add(0x4056, true);
            UsedIDs.Add(0x405e, true);
            UsedIDs.Add(0x405f, true);
            UsedIDs.Add(0x4063, true);
            UsedIDs.Add(0x406f, true);
            UsedIDs.Add(0x4074, true);
            UsedIDs.Add(0x4075, true);
            UsedIDs.Add(0x4076, true);
            UsedIDs.Add(0x407d, true);
            UsedIDs.Add(0x407f, true);
            UsedIDs.Add(0x4083, true);
            UsedIDs.Add(0x4080, true);
            UsedIDs.Add(0x4081, true);
            UsedIDs.Add(0x4082, true);
            UsedIDs.Add(0x4084, true);
            UsedIDs.Add(0x408e, true);
            UsedIDs.Add(0x4091, true);
            UsedIDs.Add(0x4092, true);
            UsedIDs.Add(0x4094, true);
            UsedIDs.Add(0x4095, true);
            UsedIDs.Add(0x409c, true);
            UsedIDs.Add(0x409d, true);
            UsedIDs.Add(0x40aa, true);
            UsedIDs.Add(0x40ab, true);
            UsedIDs.Add(0x40ad, true);
            UsedIDs.Add(0x40ae, true);
            UsedIDs.Add(0x272b, true);
            UsedIDs.Add(0x222a, true);
            UsedIDs.Add(0x222d, true);
            UsedIDs.Add(0x222e, true);
            UsedIDs.Add(0x223b, true);
            UsedIDs.Add(0x2265, true);
            UsedIDs.Add(0x2267, true);
            UsedIDs.Add(0x2268, true);
            UsedIDs.Add(0x2269, true);
            UsedIDs.Add(0x2274, true);
            UsedIDs.Add(0x2290, true);
            UsedIDs.Add(0x2291, true);
            UsedIDs.Add(0x2292, true);
            UsedIDs.Add(0x229c, true);
            UsedIDs.Add(0x229e, true);
            UsedIDs.Add(0x22a8, true);
            UsedIDs.Add(0x22b3, true);
            UsedIDs.Add(0x22b5, true);
            UsedIDs.Add(0x22b7, true);
            UsedIDs.Add(0x22d1, true);
            UsedIDs.Add(0x4575, true);
            UsedIDs.Add(0x2335, true);
            UsedIDs.Add(0x1ebb, true);
            UsedIDs.Add(0x1cdd, true);
            UsedIDs.Add(0x1cee, true);
            UsedIDs.Add(0x1cef, true);
            UsedIDs.Add(0x1cf1, true);
            UsedIDs.Add(0x1cf3, true);
            UsedIDs.Add(0x1cf4, true);
            UsedIDs.Add(0x1cf7, true);
            UsedIDs.Add(0x1cfc, true);
            UsedIDs.Add(0x1d17, true);
            UsedIDs.Add(0x1d00, true);
            UsedIDs.Add(0x1d01, true);
            UsedIDs.Add(0x1d05, true);
            UsedIDs.Add(0x1d07, true);
            UsedIDs.Add(0x1d0b, true);
            UsedIDs.Add(0x1d0d, true);
            UsedIDs.Add(0x1d0f, true);
            UsedIDs.Add(0x1d12, true);
            UsedIDs.Add(0x1d13, true);
            UsedIDs.Add(0x1d4f, true);
            UsedIDs.Add(0x1d64, true);
            UsedIDs.Add(0x1d50, true);
            UsedIDs.Add(0x1d58, true);
            UsedIDs.Add(0x1d59, true);
            UsedIDs.Add(0x1d51, true);
            UsedIDs.Add(0x1d53, true);
            UsedIDs.Add(0x1d66, true);
            UsedIDs.Add(0x1d55, true);
            UsedIDs.Add(0xdda, true);
            UsedIDs.Add(0xddd, true);
            UsedIDs.Add(0xdde, true);
            UsedIDs.Add(0xde3, true);
            UsedIDs.Add(0xdf0, true);
            UsedIDs.Add(0xdfb, true);
            UsedIDs.Add(0xe00, true);
            UsedIDs.Add(0xe0b, true);
            UsedIDs.Add(0xe0c, true);
            UsedIDs.Add(0xe0e, true);
            UsedIDs.Add(0xe0f, true);
            UsedIDs.Add(0xe11, true);
            UsedIDs.Add(0xfed, true);
            UsedIDs.Add(0xff7, true);
            UsedIDs.Add(0xffb, true);
            UsedIDs.Add(0xfe9, true);
            UsedIDs.Add(0xb30, true);
            UsedIDs.Add(0x12e, true);
            UsedIDs.Add(0x8d3, true);
            UsedIDs.Add(0x8d4, true);
            UsedIDs.Add(0x8d5, true);
            UsedIDs.Add(0x8d7, true);
            UsedIDs.Add(0xb32, true);
            UsedIDs.Add(0xb34, true);
            UsedIDs.Add(0xb38, true);
            UsedIDs.Add(0xb39, true);
            UsedIDs.Add(0xb3e, true);
            UsedIDs.Add(0x12d, true);
            UsedIDs.Add(0x15, true);
            UsedIDs.Add(0x26, true);
            UsedIDs.Add(0x31, true);
            UsedIDs.Add(0x33, true);
            UsedIDs.Add(0x4b, true);
            UsedIDs.Add(0x5c, true);
            UsedIDs.Add(0x62, true);
            UsedIDs.Add(0x64, true);
            UsedIDs.Add(0x71, true);
            UsedIDs.Add(0x77, true);
            UsedIDs.Add(0x7f, true);
            UsedIDs.Add(0x79, true);
            UsedIDs.Add(0x84, true);
            UsedIDs.Add(0x90, true);
            UsedIDs.Add(0x99, true);
            UsedIDs.Add(0xa4, true);
            UsedIDs.Add(0xb2, true);
            UsedIDs.Add(0xa8, true);
            UsedIDs.Add(0xac, true);
            UsedIDs.Add(0xb8, true);
            UsedIDs.Add(0xe2, true);
            UsedIDs.Add(0x10f, true);
            UsedIDs.Add(0xf3, true);
            UsedIDs.Add(0x10e, true);
            UsedIDs.Add(0x110, true);
            UsedIDs.Add(0x111, true);
        }
    }
}
