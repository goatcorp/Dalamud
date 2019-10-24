using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Structs.JobGauge;
using Dalamud.Hooking;
using Serilog;
using System;
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
        private PlayerCharacter localCharacter = null;

        public unsafe IconReplacer(Dalamud dalamud, SigScanner scanner) {
            this.dalamud = dalamud;
            this.Address = new IconReplacerAddressResolver();
            this.Address.Setup(scanner);

            this.byteBase = scanner.Module.BaseAddress;
            this.comboTimer = byteBase + 0x1AE1B10;
            this.lastComboMove = byteBase + 0x1AE1B14;

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
        private unsafe ulong GetIconDetour(byte self, uint actionID) {

            // TODO: More jobs, level checking for everything.

            // Check if player is loaded in by trying to get their buffs.
            // If not, skip everything until we are (game will crash cause I'm lazy).
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

            // TODO: Update localCharacter without destroying the log with debug messages
            // As it stands, don't rely on localCharacter.level for anything.
            if (localCharacter == null) {
                try {
                    localCharacter = dalamud.ClientState.LocalPlayer;
                }
                catch(Exception e) {
                    localCharacter = null;
                    return this.iconHook.Original(self, actionID);
                }
            }

            // Don't clutter the spaghetti any worse than it already is.
            int lastMove = Marshal.ReadInt32(lastComboMove);
            float comboTime = (float)Marshal.ReadInt32(comboTimer);
            localCharacter = dalamud.ClientState.LocalPlayer;
            byte level = localCharacter.Level;

            // DRAGOON
            // TODO: Jump/High Jump into Mirage Dive

            // Replace Coerthan Torment with Coerthan Torment combo chain
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonCoerthanTormentCombo)) {
                if (actionID == 16477) {
                    if (comboTime > 0) {
                        if (lastMove == 86) return 7397;
                        if (lastMove == 7397) return 16477;
                    }
                    return 86;
                }
            }
            

            // Replace Chaos Thrust with the Chaos Thrust combo chain
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonChaosThrustCombo)) {
                if (actionID == 88) {
                    if (comboTime > 0) {
                        if (lastMove == 75 || lastMove == 16479) return 87;
                        if (lastMove == 87) return 88;
                    }
                    if (SearchBuffArray(802)) return 3554;
                    if (SearchBuffArray(803)) return 3556;
                    if (SearchBuffArray(1863)) return 16479;

                    return 75;
                }
            }
            

            // Replace Full Thrust with the Full Thrust combo chain
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonFullThrustCombo)) {
                if (actionID == 84) {
                    if (comboTime > 0) {
                        if (lastMove == 75 || lastMove == 16479) return 78;
                        if (lastMove == 78) return 84;
                    }
                    if (SearchBuffArray(802)) return 3554;
                    if (SearchBuffArray(803)) return 3556;
                    if (SearchBuffArray(1863)) return 16479;

                    return 75;
                }
            }

            // DARK KNIGHT

            // Replace Souleater with Souleater combo chain
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DarkSouleaterCombo)) {
                if (actionID == 3632) {
                    if (comboTime > 0) {
                        if (lastMove == 3617) return 3623;
                        if (lastMove == 3623) return 3632;
                    }

                    return 3617;
                }
            }

            // Replace Stalwart Soul with Stalwart Soul combo chain
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DarkStalwartSoulCombo)) {
                if (actionID == 16468) {
                    if (comboTime > 0) {
                        if (lastMove == 3621) return 16468;
                    }

                    return 3621;
                }
            }

            // PALADIN

            // Replace Goring Blade with Goring Blade combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinGoringBladeCombo)) {
                if (actionID == 3538) {
                    if (comboTime > 0) {
                        if (lastMove == 9) return 15;
                        if (lastMove == 15) return 3538;
                    }

                    return 9;
                }
            }

            // Replace Royal Authority with Royal Authority combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinRoyalAuthorityCombo)) {
                if (actionID == 3539) {
                    if (comboTime > 0) {
                        if (lastMove == 9) return 15;
                        if (lastMove == 15) return 3539;
                    }

                    return 9;
                }
            }

            // Replace Prominence with Prominence combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinProminenceCombo)) {
                if (actionID == 16457) {
                    if (comboTime > 0) {
                        if (lastMove == 7381) return 16457;
                    }

                    return 7381;
                }
            }

            // WARRIOR

            // Replace Storm's Path with Storm's Path combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.WarriorStormsPathCombo)) {
                if (actionID == 42) {
                    if (comboTime > 0) {
                        if (lastMove == 31) return 37;
                        if (lastMove == 37) return 42;
                    }

                    return 31;
                }
            }

            // Replace Storm's Eye with Storm's Eye combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.WarriorStormsEyeCombo)) {
                if (actionID == 45) {
                    if (comboTime > 0) {
                        if (lastMove == 31) return 37;
                        if (lastMove == 37) return 45;
                    }
                    return 31;
                }
            }

            // Replace Mythril Tempest with Mythril Tempest combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.WarriorMythrilTempestCombo)) {
                if (actionID == 16462) {
                    if (comboTime > 0) {
                        if (lastMove == 41) return 16462;
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
                        if (lastMove == 7477) return 7480;
                    }
                    return 7477;
                }
            }

            // Replace Gekko with Gekko combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiGekkoCombo)) {
                if (actionID == 7481) {
                    if (SearchBuffArray(1233)) return 7481;
                    if (comboTime > 0) {
                        if (lastMove == 7477) return 7478;
                        if (lastMove == 7478) return 7481;
                    }
                    return 7477;
                }
            }

            // Replace Kasha with Kasha combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiKashaCombo)) {
                if (actionID == 7482) {
                    if (SearchBuffArray(1233)) return 7482;
                    if (comboTime > 0) {
                        if (lastMove == 7477) return 7479;
                        if (lastMove == 7479) return 7482;
                    }
                    return 7477;
                }
            }

            // Replace Mangetsu with Mangetsu combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiMangetsuCombo)) {
                if (actionID == 7484) {
                    if (SearchBuffArray(1233)) return 7484;
                    if (comboTime > 0) {
                        if (lastMove == 7483) return 7484;
                    }
                    return 7483;
                }
            }

            // Replace Oka with Oka combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiOkaCombo)) {
                if (actionID == 7485) {
                    if (SearchBuffArray(1233)) return 7485;
                    if (comboTime > 0) {
                        if (lastMove == 7483) return 7485;
                    }
                    return 7483;
                }
            }

            // NINJA

            // Replace Shadow Fang with Shadow Fang combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.NinjaShadowFangCombo)) {
                if (actionID == 2257) {
                    if (comboTime > 0) {
                        if (lastMove == 2240) return 2257;
                    }
                    return 2240;
                }
            }

            // Replace Armor Crush with Armor Crush combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.NinjaArmorCrushCombo)) {
                if (actionID == 3563) {
                    if (comboTime > 0) {
                        if (lastMove == 2240) return 2242;
                        if (lastMove == 2242) return 3563;
                    }
                    return 2240;
                }
            }

            // Replace Aeolian Edge with Aeolian Edge combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.NinjaAeolianEdgeCombo)) {
                if (actionID == 2255) {
                    if (comboTime > 0) {
                        if (lastMove == 2240) return 2242;
                        if (lastMove == 2242) return 2255;
                    }
                    return 2240;
                }
            }

            // Replace Hakke Mujinsatsu with Hakke Mujinsatsu combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.NinjaHakkeMujinsatsuCombo)) {
                if (actionID == 16488) {
                    if (comboTime > 0) {
                        if (lastMove == 2254) return 16488;
                    }
                    return 2254;
                }
            }

            // GUNBREAKER

            // Replace Solid Barrel with Solid Barrel combo
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.GunbreakerSolidBarrelCombo)) {
                if (actionID == 16145) {
                    if (comboTime > 0) {
                        if (lastMove == 16137) return 16139;
                        if (lastMove == 16139) return 16145;
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
                        if (lastMove == 16141) return 16149;
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
                    if (this.dalamud.ClientState.JobGauges.Get<MCHGauge>().IsOverheated() && level >= 35) return 7410;
                    if (comboTime > 0) {
                        if (lastMove == 2866) return 7412;
                        if (lastMove == 2868) return 7413;
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
            // TODO: For some reason this breaks only on my Crystal alt.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.BlackEnochianFeature)) {
                if (actionID == 3575) {
                    BLMGauge jobInfo = this.dalamud.ClientState.JobGauges.Get<BLMGauge>();
                    if (jobInfo.IsEnoActive) {
                        if (jobInfo.InUmbralIce()) return 3576;
                        return 3577;
                    }
                    return 3575;
                }
            }

            // Umbral Soul and Transpose
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.BlackManaFeature)) {
                if (actionID == 149) {
                    if (this.dalamud.ClientState.JobGauges.Get<BLMGauge>().InUmbralIce() && level >= 76) return 16506;
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
                        case CardType.LORD:
                            return 7444;
                        case CardType.LADY:
                            return 7445;
                        default:
                            return 3590;
                    }
                }
            }

            // SUMMONER

            // DWT changes. 
            // Now contains DWT, Deathflare, Summon Bahamut, Enkindle Bahamut, FBT, and Enkindle Phoenix.
            // What a monster of a button.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SummonerDwtCombo)) {
                if (actionID == 3581) {
                    SMNGauge gauge = this.dalamud.ClientState.JobGauges.Get<SMNGauge>();
                    if (gauge.TimerRemaining > 0) {
                        if (gauge.ReturnSummon > 0) {
                            if (gauge.IsPhoenixReady()) return 16516;
                            return 7429;
                        }
                        return 3582;
                    }
                    else {
                        if (gauge.IsBahamutReady()) return 7427;
                        if (gauge.IsPhoenixReady()) return 16513;
                        return 3581;
                    }
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
                        if(level >= 52) return 3578;
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
                        if (lastMove == 15989) return 15990;
                    }
                    return 15989;
                }
            }

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


            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerFanDanceCombo)) {
                // Fan Dance changes into Fan Dance 3 while flourishing.
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
                    if (level > 76) return 16495;
                    return 97;
                }
            }

            // MONK

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
                        if (SearchBuffArray(107)) return 74;
                        if (SearchBuffArray(108)) return 61;
                        if (SearchBuffArray(109)) return 56;
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
                        if (SearchBuffArray(108)) return 54;
                        if (SearchBuffArray(109)) return 66;
                        return 53;
                    }
                }
            }

            // Replace Rockbreaker with AoE combo.
            // During PB, RB (with sub-max stacks) > Twin Snakes (if not applied) > AotD.
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
                        else return 62;
                    }
                    else {
                        if (SearchBuffArray(107)) return 62;
                        if (SearchBuffArray(108)) {
                            if (!SearchBuffArray(101)) return 61;
                            return 16473;
                        }
                        if (SearchBuffArray(109)) return 70;
                        return 62;
                    }
                }
            }

            // RED MAGE

            // Replace Verstone with White Magic spells. Priority order:
            // Scorch > Verholy > Verstone = Veraero (with Dualcast active) > opener Veraero > Jolt
            // Impact is not the first available spell to allow for precast openers.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageWhiteMagicFeature)) {
                if (actionID == 7511) {
                    if ((lastMove == 7526 || lastMove == 7525) && level == 80) return 16530;
                    if (lastMove == 7529) return 7526;
                    if (SearchBuffArray(1249) || SearchBuffArray(167)) return 7507;
                    if (SearchBuffArray(1235)) return 7511;
                    RDMGauge gauge = this.dalamud.ClientState.JobGauges.Get<RDMGauge>();
                    if (gauge.BlackGauge == 0 && gauge.WhiteGauge == 0) return 7507;
                    if (level >= 62) return 7524;
                    return 7503;
                }
            }

            // Replace Verfire with Black Magic spells. Priority order:
            // Scorch > Verflare> Verfire = Verthunder (with Dualcast active) > opener Verthunder > Jolt
            // Impact is not the first available spell to allow for precast openers.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageBlackMagicFeature)) {
                if (actionID == 7510) {
                    if ((lastMove == 7526 || lastMove == 7525) && level == 80) return 16530;
                    if (lastMove == 7529) return 7525;
                    if (SearchBuffArray(1249) || SearchBuffArray(167)) return 7505;
                    if (SearchBuffArray(1234)) return 7510;
                    RDMGauge gauge = this.dalamud.ClientState.JobGauges.Get<RDMGauge>();
                    if (gauge.BlackGauge == 0 && gauge.WhiteGauge == 0) return 7505;
                    if (level >= 62) return 7524;
                    return 7503;
                }
            }

            // Replace Veraero 2 with Impact when Dualcast is active
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageWhiteAoECombo)) {
                if (actionID == 16525) {
                    if (level >= 66 && (SearchBuffArray(1249) || SearchBuffArray(167))) return 16526;
                    return 16525;
                }
            }

            // Replace Verthunder 2 with Impact when Dualcast is active
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageBlackAoECombo)) {
                if (actionID == 16524) {
                    if (level >= 66 && (SearchBuffArray(1249) || SearchBuffArray(167))) return 16526;
                    return 16524;
                }
            }

            // Replace Redoublement with Redoublement combo, Enchanted if possible.
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageMeleeCombo)) {
                if (actionID == 7516) {
                    RDMGauge gauge = this.dalamud.ClientState.JobGauges.Get<RDMGauge>();
                    if (lastMove == 7504 || lastMove == 7527) {
                        if (gauge.BlackGauge >= 25 && gauge.WhiteGauge >= 25) return 7528;
                        return 7512;
                    }
                    if (lastMove == 7512) {
                        if (gauge.BlackGauge >= 25 && gauge.WhiteGauge >= 25) return 7529;
                        return 7516;
                    }
                    if (gauge.BlackGauge >= 30 && gauge.WhiteGauge >= 30) return 7527;
                    return 7516;
                }
            }

            return this.iconHook.Original(self, actionID);
        }

        private bool SearchBuffArray(short needle) {
            for (int i = 0; i < 60; i++) {
                if (Marshal.ReadInt16(activeBuffArray + 4 * i) == needle) return true;
            }
            return false;
        }

        private unsafe delegate int* getArray(long* address);

        private unsafe IntPtr FindBuffAddress() {
            IntPtr randomAddress = byteBase + 0x1b2c970;
            IntPtr num = Marshal.ReadIntPtr(randomAddress);
            IntPtr step2 = (IntPtr)(Marshal.ReadInt64(num) + 0x248);
            IntPtr step3 = Marshal.ReadIntPtr(step2);
            var callback = Marshal.GetDelegateForFunctionPointer<getArray>(step3);
            return (IntPtr)callback((long*)num);
        }
    }
}
