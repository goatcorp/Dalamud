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
        private IntPtr playerLevel;
        private Dalamud dalamud;

        private HashSet<uint> CustomIDs;
        private HashSet<uint> VanillaIDs;

        public unsafe IconReplacer(Dalamud dalamud, SigScanner scanner) {
            this.dalamud = dalamud;
            this.Address = new IconReplacerAddressResolver();
            this.Address.Setup(scanner);

            this.byteBase = scanner.Module.BaseAddress;
            this.comboTimer = byteBase + 0x1BB3B50;
            //this.comboTimer = scanner.ScanText("E8 ?? ?? ?? ?? 80 7E 21 00") + 0x178;
            this.lastComboMove = this.comboTimer + 0x4;

            this.playerLevel = byteBase + 0x1C2BFA8 + 0x78;
            //this.playerLevel = scanner.ScanText("E8 ?? ?? ?? ?? 88 45 EF") + 0x4D;

            CustomIDs = new HashSet<uint>();
            VanillaIDs = new HashSet<uint>();

            PopulateDict();

            Log.Verbose("===== H O T B A R S =====");
            Log.Verbose("IsIconReplaceable address {IsIconReplaceable}", Address.IsIconReplaceable);
            Log.Verbose("GetIcon address {GetIcon}", Address.GetIcon);
            Log.Verbose("ComboTimer address {ComboTimer}", this.comboTimer);
            Log.Verbose("LastComboMove address {LastComboMove}", this.lastComboMove);
            Log.Verbose("PlayerLevel address {PlayerLevel}", this.playerLevel);

            this.iconHook = new Hook<OnGetIconDelegate>(this.Address.GetIcon, new OnGetIconDelegate(GetIconDetour), this);
            this.checkerHook = new Hook<OnCheckIsIconReplaceableDelegate>(this.Address.IsIconReplaceable, new OnCheckIsIconReplaceableDelegate(CheckIsIconReplaceableDetour), this);
        }

        public void Enable() {
            this.iconHook.Enable();
            this.checkerHook.Enable();
            Log.Verbose("IconReplacer hooked");
        }

        public void Dispose() {
            this.iconHook.Dispose();
            this.checkerHook.Dispose();
            Log.Verbose("IconReplacer unhooked");
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

            if (this.VanillaIDs.Contains(actionID)) return this.iconHook.Original(self, actionID);
            if (!this.CustomIDs.Contains(actionID)) return actionID;
            if (this.activeBuffArray == IntPtr.Zero) {
                try {
                    this.activeBuffArray = FindBuffAddress();
                }
                catch(Exception e) { //Before you're loaded in
                    this.activeBuffArray = IntPtr.Zero;
                    return this.iconHook.Original(self, actionID);
                }
            }
            // Don't clutter the spaghetti any worse than it already is.
            var lastMove = Marshal.ReadInt32(this.lastComboMove);
            var comboTime = Marshal.ReadInt32(this.comboTimer);
            var level = Marshal.ReadByte(this.playerLevel);

            // DRAGOON
            
            // Change Jump/High Jump into Mirage Dive when Dive Ready
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonJumpFeature)) {
                if (actionID == 92) {
                    if (SearchBuffArray(1243)) {
                        return 7399;
                    }
                    if (level >= 74) return 16478;
                    return 92;
                }
            }

            // Change Blood of the Dragon into Stardiver when in Life of the Dragon
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonBOTDFeature)) {
                if (actionID == 3553) {
                    if (level >= 80) {
                        if (this.dalamud.ClientState.JobGauges.Get<DRGGauge>().BOTDState == BOTDState.LOTD) {
                            return 16480;
                        }
                    }
                    return 3553;
                }
            }

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

            //Replace Dream Within a Dream with Assassinate when Assassinate Ready
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.NinjaAssassinateFeature)) {
                if(actionID == 3566) {
                    if (SearchBuffArray(1955)) return 2246;
                    return 3566;
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

            // Replace Wicked Talon with Gnashing Fang combo
            // TODO: Potentially add Contuation moves as well?
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.GunbreakerGnashingFangCombo)) {
                if (actionID == 16150) {
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

            // Replace Clean Shot with Heated Clean Shot combo
            // Or with Heat Blast when overheated.
            // For some reason the shots use their unheated IDs as combo moves
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.MachinistMainCombo)) {
                if (actionID == 2873) {
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
                    if (level >= 54) return 7411;
                    return 2866;
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
                        if (gauge.IsPhoenixReady()) {
                            if (level == 80) return 16549;
                            return 16513;
                        }
                        return 3581;
                    }
                }
            }

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

            // Change Fester into Energy Drain
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SummonerEDFesterCombo)) {
                if (actionID == 181) {
                    if (!this.dalamud.ClientState.JobGauges.Get<SMNGauge>().HasAetherflowStacks())
                        return 16508;
                    return 181;
                }
            }

            //Change Painflare into Energy Syphon
            if (this.dalamud.Configuration.ComboPresets.HasFlag(CustomComboPreset.SummonerESPainflareCombo)) {
                if (actionID == 3578) {
                    if (!this.dalamud.ClientState.JobGauges.Get<SMNGauge>().HasAetherflowStacks())
                        return 16510;
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

        private bool SearchBuffArray(short needle) {
            for (int i = 0; i < 60; i++) {
                if (Marshal.ReadInt16(activeBuffArray + 4 * i) == needle) return true;
            }           
            return false;
        }

        private unsafe delegate int* getArray(long* address);

        private unsafe IntPtr FindBuffAddress() {
            IntPtr randomAddress = byteBase + 0x1C02BE0;
            IntPtr num = Marshal.ReadIntPtr(randomAddress);
            IntPtr step2 = (IntPtr)(Marshal.ReadInt64(num) + 0x248);
            IntPtr step3 = Marshal.ReadIntPtr(step2);
            var callback = Marshal.GetDelegateForFunctionPointer<getArray>(step3);
            return (IntPtr)callback((long*)num);
        }

        private void PopulateDict() {

            CustomIDs.Add(16477);
            CustomIDs.Add(88);
            CustomIDs.Add(84);
            CustomIDs.Add(3632);
            CustomIDs.Add(16468);
            CustomIDs.Add(3538);
            CustomIDs.Add(3539);
            CustomIDs.Add(16457);
            CustomIDs.Add(42);
            CustomIDs.Add(45);
            CustomIDs.Add(16462);
            CustomIDs.Add(7480);
            CustomIDs.Add(7481);
            CustomIDs.Add(7482);
            CustomIDs.Add(7484);
            CustomIDs.Add(7485);
            CustomIDs.Add(3563);
            CustomIDs.Add(2255);
            CustomIDs.Add(16488);
            CustomIDs.Add(16145);
            CustomIDs.Add(16150);
            CustomIDs.Add(16149);
            CustomIDs.Add(7413);
            CustomIDs.Add(2870);
            CustomIDs.Add(3575);
            CustomIDs.Add(149);
            CustomIDs.Add(17055);
            CustomIDs.Add(3582);
            CustomIDs.Add(3581);
            CustomIDs.Add(163);
            CustomIDs.Add(181);
            CustomIDs.Add(3578);
            CustomIDs.Add(16543);
            CustomIDs.Add(167);
            CustomIDs.Add(15994);
            CustomIDs.Add(15993);
            CustomIDs.Add(16007);
            CustomIDs.Add(16008);
            CustomIDs.Add(16531);
            CustomIDs.Add(16534);
            CustomIDs.Add(3559);
            CustomIDs.Add(97);
            CustomIDs.Add(16525);
            CustomIDs.Add(16524);
            CustomIDs.Add(7516);
            CustomIDs.Add(3566);
            CustomIDs.Add(92);
            CustomIDs.Add(3553);
            CustomIDs.Add(2873);
            VanillaIDs.Add(0x3e75);
            VanillaIDs.Add(0x3e76);
            VanillaIDs.Add(0x3e77);
            VanillaIDs.Add(0x3e78);
            VanillaIDs.Add(0x3e7d);
            VanillaIDs.Add(0x3e7e);
            VanillaIDs.Add(0x3e86);
            VanillaIDs.Add(0x3f10);
            VanillaIDs.Add(0x3f25);
            VanillaIDs.Add(0x3f1b);
            VanillaIDs.Add(0x3f1c);
            VanillaIDs.Add(0x3f1d);
            VanillaIDs.Add(0x3f1e);
            VanillaIDs.Add(0x451f);
            VanillaIDs.Add(0x42ff);
            VanillaIDs.Add(0x4300);
            VanillaIDs.Add(0x49d4);
            VanillaIDs.Add(0x49d5);
            VanillaIDs.Add(0x49e9);
            VanillaIDs.Add(0x49ea);
            VanillaIDs.Add(0x49f4);
            VanillaIDs.Add(0x49f7);
            VanillaIDs.Add(0x49f9);
            VanillaIDs.Add(0x4a06);
            VanillaIDs.Add(0x4a31);
            VanillaIDs.Add(0x4a32);
            VanillaIDs.Add(0x4a35);
            VanillaIDs.Add(0x4792);
            VanillaIDs.Add(0x452f);
            VanillaIDs.Add(0x453f);
            VanillaIDs.Add(0x454c);
            VanillaIDs.Add(0x455c);
            VanillaIDs.Add(0x455d);
            VanillaIDs.Add(0x4561);
            VanillaIDs.Add(0x4565);
            VanillaIDs.Add(0x4566);
            VanillaIDs.Add(0x45a0);
            VanillaIDs.Add(0x45c8);
            VanillaIDs.Add(0x45c9);
            VanillaIDs.Add(0x45cd);
            VanillaIDs.Add(0x4197);
            VanillaIDs.Add(0x4199);
            VanillaIDs.Add(0x419b);
            VanillaIDs.Add(0x419d);
            VanillaIDs.Add(0x419f);
            VanillaIDs.Add(0x4198);
            VanillaIDs.Add(0x419a);
            VanillaIDs.Add(0x419c);
            VanillaIDs.Add(0x419e);
            VanillaIDs.Add(0x41a0);
            VanillaIDs.Add(0x41a1);
            VanillaIDs.Add(0x41a2);
            VanillaIDs.Add(0x41a3);
            VanillaIDs.Add(0x417e);
            VanillaIDs.Add(0x404f);
            VanillaIDs.Add(0x4051);
            VanillaIDs.Add(0x4052);
            VanillaIDs.Add(0x4055);
            VanillaIDs.Add(0x4053);
            VanillaIDs.Add(0x4056);
            VanillaIDs.Add(0x405e);
            VanillaIDs.Add(0x405f);
            VanillaIDs.Add(0x4063);
            VanillaIDs.Add(0x406f);
            VanillaIDs.Add(0x4074);
            VanillaIDs.Add(0x4075);
            VanillaIDs.Add(0x4076);
            VanillaIDs.Add(0x407d);
            VanillaIDs.Add(0x407f);
            VanillaIDs.Add(0x4083);
            VanillaIDs.Add(0x4080);
            VanillaIDs.Add(0x4081);
            VanillaIDs.Add(0x4082);
            VanillaIDs.Add(0x4084);
            VanillaIDs.Add(0x408e);
            VanillaIDs.Add(0x4091);
            VanillaIDs.Add(0x4092);
            VanillaIDs.Add(0x4094);
            VanillaIDs.Add(0x4095);
            VanillaIDs.Add(0x409c);
            VanillaIDs.Add(0x409d);
            VanillaIDs.Add(0x40aa);
            VanillaIDs.Add(0x40ab);
            VanillaIDs.Add(0x40ad);
            VanillaIDs.Add(0x40ae);
            VanillaIDs.Add(0x272b);
            VanillaIDs.Add(0x222a);
            VanillaIDs.Add(0x222d);
            VanillaIDs.Add(0x222e);
            VanillaIDs.Add(0x223b);
            VanillaIDs.Add(0x2265);
            VanillaIDs.Add(0x2267);
            VanillaIDs.Add(0x2268);
            VanillaIDs.Add(0x2269);
            VanillaIDs.Add(0x2274);
            VanillaIDs.Add(0x2290);
            VanillaIDs.Add(0x2291);
            VanillaIDs.Add(0x2292);
            VanillaIDs.Add(0x229c);
            VanillaIDs.Add(0x229e);
            VanillaIDs.Add(0x22a8);
            VanillaIDs.Add(0x22b3);
            VanillaIDs.Add(0x22b5);
            VanillaIDs.Add(0x22b7);
            VanillaIDs.Add(0x22d1);
            VanillaIDs.Add(0x4575);
            VanillaIDs.Add(0x2335);
            VanillaIDs.Add(0x1ebb);
            VanillaIDs.Add(0x1cdd);
            VanillaIDs.Add(0x1cee);
            VanillaIDs.Add(0x1cef);
            VanillaIDs.Add(0x1cf1);
            VanillaIDs.Add(0x1cf3);
            VanillaIDs.Add(0x1cf4);
            VanillaIDs.Add(0x1cf7);
            VanillaIDs.Add(0x1cfc);
            VanillaIDs.Add(0x1d17);
            VanillaIDs.Add(0x1d00);
            VanillaIDs.Add(0x1d01);
            VanillaIDs.Add(0x1d05);
            VanillaIDs.Add(0x1d07);
            VanillaIDs.Add(0x1d0b);
            VanillaIDs.Add(0x1d0d);
            VanillaIDs.Add(0x1d0f);
            VanillaIDs.Add(0x1d12);
            VanillaIDs.Add(0x1d13);
            VanillaIDs.Add(0x1d4f);
            VanillaIDs.Add(0x1d64);
            VanillaIDs.Add(0x1d50);
            VanillaIDs.Add(0x1d58);
            VanillaIDs.Add(0x1d59);
            VanillaIDs.Add(0x1d51);
            VanillaIDs.Add(0x1d53);
            VanillaIDs.Add(0x1d66);
            VanillaIDs.Add(0x1d55);
            VanillaIDs.Add(0xdda);
            VanillaIDs.Add(0xddd);
            VanillaIDs.Add(0xdde);
            VanillaIDs.Add(0xde3);
            VanillaIDs.Add(0xdf0);
            VanillaIDs.Add(0xdfb);
            VanillaIDs.Add(0xe00);
            VanillaIDs.Add(0xe0b);
            VanillaIDs.Add(0xe0c);
            VanillaIDs.Add(0xe0e);
            VanillaIDs.Add(0xe0f);
            VanillaIDs.Add(0xe11);
            VanillaIDs.Add(0xfed);
            VanillaIDs.Add(0xff7);
            VanillaIDs.Add(0xffb);
            VanillaIDs.Add(0xfe9);
            VanillaIDs.Add(0xb30);
            VanillaIDs.Add(0x12e);
            VanillaIDs.Add(0x8d3);
            VanillaIDs.Add(0x8d4);
            VanillaIDs.Add(0x8d5);
            VanillaIDs.Add(0x8d7);
            VanillaIDs.Add(0xb32);
            VanillaIDs.Add(0xb34);
            VanillaIDs.Add(0xb38);
            VanillaIDs.Add(0xb3e);
            VanillaIDs.Add(0x12d);
            VanillaIDs.Add(0x15);
            VanillaIDs.Add(0x26);
            VanillaIDs.Add(0x31);
            VanillaIDs.Add(0x33);
            VanillaIDs.Add(0x4b);
            VanillaIDs.Add(0x62);
            VanillaIDs.Add(0x64);
            VanillaIDs.Add(0x71);
            VanillaIDs.Add(0x77);
            VanillaIDs.Add(0x7f);
            VanillaIDs.Add(0x79);
            VanillaIDs.Add(0x84);
            VanillaIDs.Add(0x90);
            VanillaIDs.Add(0x99);
            VanillaIDs.Add(0xa4);
            VanillaIDs.Add(0xb2);
            VanillaIDs.Add(0xa8);
            VanillaIDs.Add(0xac);
            VanillaIDs.Add(0xb8);
            VanillaIDs.Add(0xe2);
            VanillaIDs.Add(0x10f);
            VanillaIDs.Add(0xf3);
            VanillaIDs.Add(0x10e);
            VanillaIDs.Add(0x110);
            VanillaIDs.Add(0x111);
        }
    }
}
