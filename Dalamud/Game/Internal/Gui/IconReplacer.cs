using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Structs.JobGauge;
using Dalamud.Hooking;
using Serilog;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Dalamud.Game.Internal.Gui {
    public class IconReplacer {
        private IconReplacerAddressResolver address;
        private Hook<OnIconDetour> iconHook;
        private IntPtr comboTimer;
        private IntPtr lastComboMove;
        private IntPtr activeBuffArray = IntPtr.Zero;
        private IntPtr jobInfo;
        private IntPtr byteBase;
        private Dalamud dalamud;
        private PlayerCharacter localCharacter = null;

        public unsafe IconReplacer(Dalamud dalamud, ProcessModule module, SigScanner scanner) {
            this.dalamud = dalamud;
            this.address = new IconReplacerAddressResolver();
            this.address.Setup(scanner);

            this.byteBase = scanner.Module.BaseAddress;
            this.jobInfo = byteBase + 0x1b2d4b4;
            this.comboTimer = byteBase + 0x1AE1B10;
            this.lastComboMove = byteBase + 0x1AE1B14;

            this.iconHook = new Hook<OnIconDetour>(this.address.BaseAddress, (Delegate)new OnIconDetour(this.HandleIconUpdate), (object)this);

        }

        public void Enable() {
            this.iconHook.Enable();
        }

        public void Dispose() {
            this.iconHook.Dispose();
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
        private unsafe ulong HandleIconUpdate(byte self, uint actionID) {

            // TODO: BRD, RDM, level checking for everything.

            // Check if player is loaded in by trying to get their buffs.
            // If not, skip everything until we are (game will crash cause I'm lazy).
            if (activeBuffArray == IntPtr.Zero) {
                try {
                    activeBuffArray = FindBuffAddress();
                    localCharacter = dalamud.ClientState.LocalPlayer;
                }
                catch (Exception e) {
                    activeBuffArray = IntPtr.Zero;
                    return this.iconHook.Original(self, actionID);
                }
            }

            // Don't clutter the spaghetti any worse than it already is.
            int lastMove = Marshal.ReadInt32(lastComboMove);
            float comboTime = (float)Marshal.ReadInt32(comboTimer);
            byte level = localCharacter.Level;

            // DRAGOON
            // TODO: Jump/High Jump into Mirage Dive

            // Replace Coerthan Torment with Coerthan Torment combo chain
            if (actionID == 16477) {
                if (comboTime > 0) {
                    if (Marshal.ReadInt32(lastComboMove) == 86) return 7397;
                    if (Marshal.ReadInt32(lastComboMove) == 7397) return 16477;
                }
                return 86;
            }

            // Replace Chaos Thrust with the Chaos Thrust combo chain
            if (actionID == 88) {
                if (comboTime > 0) {
                    if (lastMove == 75 || lastMove == 16479) return 87;
                    if (lastMove == 87) return 88;

                }
                if (activeBuffArray != IntPtr.Zero) {
                    if (SearchBuffArray(802)) return 3554;
                    if (SearchBuffArray(803)) return 3556;
                    if (SearchBuffArray(1863)) return 16479;
                }
                return 75;
            }

            // Replace Full Thrust with the Full Thrust combo chain
            if (actionID == 84) {
                if (comboTime > 0) {
                    if (lastMove == 75 || lastMove == 16479) return 78;
                    if (lastMove == 78) return 84;

                }
                if (activeBuffArray != IntPtr.Zero) {
                    if (SearchBuffArray(802)) return 3554;
                    if (SearchBuffArray(803)) return 3556;
                    if (SearchBuffArray(1863)) return 16479;
                }
                return 75;
            }

            // DARK KNIGHT

            // Replace Souleater with Souleater combo chain
            if (actionID == 3632) {
                if (comboTime > 0) {
                    if (lastMove == 3617) return 3623;
                    if (lastMove == 3623) return 3632;
                }
                return 3617;
            }

            // Replace Stalwart Soul with Stalwart Soul combo chain
            if (actionID == 16468) {
                if (comboTime > 0) {
                    if (lastMove == 3621) return 16468;
                }
                return 3621;
            }

            // PALADIN

            // Replace Goring Blade with Goring Blade combo
            if (actionID == 3538) {
                if (comboTime > 0) {
                    if (lastMove == 9) return 15;
                    if (lastMove == 15) return 3538;
                }
                return 9;
            }

            // Replace Royal Authority with Royal Authority combo
            if (actionID == 3539) {
                if (comboTime > 0) {
                    if (lastMove == 9) return 15;
                    if (lastMove == 15) return 3539;
                }
                return 9;
            }

            // Replace Prominence with Prominence combo
            if (actionID == 16457) {
                if (comboTime > 0) {
                    if (lastMove == 7381) return 16457;
                }
                return 7381;
            }

            // WARRIOR

            // Replace Storm's Path with Storm's Path combo
            if (actionID == 42) {
                if (comboTime > 0) {
                    if (lastMove == 31) return 37;
                    if (lastMove == 37) return 42;
                }
                return 31;
            }

            // Replace Storm's Eye with Storm's Eye combo
            if (actionID == 45) {
                if (comboTime > 0) {
                    if (lastMove == 31) return 37;
                    if (lastMove == 37) return 45;
                }
                return 31;
            }

            // Replace Mythril Tempest with Mythril Tempest combo
            if (actionID == 16462) {
                if (comboTime > 0) {
                    if (lastMove == 41) return 16462;
                }
                return 41;
            }

            // SAMURAI

            // Replace Yukikaze with Yukikaze combo
            if (actionID == 7480) {
                if (activeBuffArray != IntPtr.Zero) {
                    if (SearchBuffArray(1233)) return 7480;
                }
                if (comboTime > 0) {
                    if (lastMove == 7477) return 7480;
                }
                return 7477;
            }

            // Replace Gekko with Gekko combo
            if (actionID == 7481) {
                if (activeBuffArray != IntPtr.Zero) {
                    if (SearchBuffArray(1233)) return 7481;
                }
                if (comboTime > 0) {
                    if (lastMove == 7477) return 7478;
                    if (lastMove == 7478) return 7481;
                }
                return 7477;
            }

            // Replace Kasha with Kasha combo
            if (actionID == 7482) {
                if (activeBuffArray != null) {
                    if (SearchBuffArray(1233)) return 7482;
                }
                if (comboTime > 0) {
                    if (lastMove == 7477) return 7479;
                    if (lastMove == 7479) return 7482;
                }
                return 7477;
            }

            // Replace Mangetsu with Mangetsu combo
            if (actionID == 7484) {
                if (activeBuffArray != null) {
                    if (SearchBuffArray(1233)) return 7484;
                }
                if (comboTime > 0) {
                    if (lastMove == 7483) return 7484;
                }
                return 7483;
            }

            // Replace Yukikaze with Yukikaze combo
            if (actionID == 7485) {
                if (activeBuffArray != null) {
                    if (SearchBuffArray(1233)) return 7485;
                }
                if (comboTime > 0) {
                    if (lastMove == 7483) return 7485;
                }
                return 7483;
            }

            // NINJA

            // Replace Shadow Fang with Shadow Fang combo
            if (actionID == 2257) {
                if (comboTime > 0) {
                    if (lastMove == 2240) return 2257;
                }
                return 2240;
            }

            // Replace Armor Crush with Armor Crush combo
            if (actionID == 2257) {
                if (comboTime > 0) {
                    if (lastMove == 2240) return 2242;
                    if (lastMove == 2242) return 3563;
                }
                return 2240;
            }

            // Replace Aeolian Edge with Aeolian Edge combo
            if (actionID == 2257) {
                if (comboTime > 0) {
                    if (lastMove == 2240) return 2242;
                    if (lastMove == 2242) return 2255;
                }
                return 2240;
            }

            // Replace Hakke Mujinsatsu with Hakke Mujinsatsu combo
            if (actionID == 16488) {
                if (comboTime > 0) {
                    if (lastMove == 2254) return 16488;
                }
                return 2254;
            }

            // GUNBREAKER

            // Replace Solid Barrel with Solid Barrel combo
            if (actionID == 16145) {
                if (comboTime > 0) {
                    if (lastMove == 16137) return 16139;
                    if (lastMove == 16139) return 16145;
                }
                return 16137;
            }

            // Replace Gnashing Fang with Gnashing Fang combo
            // TODO: Potentially add Contuation moves as well?
            if (actionID == 16146) {
                byte ammoComboState = Marshal.ReadByte(jobInfo, 0x10);
                if (ammoComboState == 1) return 16147;
                if (ammoComboState == 2) return 16150;
                return 16146;
            }

            // Replace Demon Slaughter with Demon Slaughter combo
            if (actionID == 16149) {
                if (comboTime > 0) {
                    if (lastMove == 16141) return 16149;
                }
                return 16141;
            }

            // MACHINIST

            // Replace Heated Clean Shot with Heated Clean Shot combo
            // Or with Heat Blast when overheated.
            // For some reason the shots use their unheated IDs as combo moves
            if (actionID == 7413) {
                if (this.dalamud.ClientState.JobGauges.Get<MCHGauge>().IsOverheated() && level >= 35) return 7410;
                if (comboTime > 0) {
                    if (lastMove == 2866) return 7412;
                    if (lastMove == 2868) return 7413;
                }
                return 7411;
            }

            // Replace Spread Shot with Auto Crossbow when overheated.
            if (actionID == 2870) {
                if (this.dalamud.ClientState.JobGauges.Get<MCHGauge>().IsOverheated() && level >= 52) return 16497;
                return 2870;
            }

            // BLACK MAGE

            // Enochian changes to B4 or F4 depending on stance.
            if (actionID == 3575) {
                BLMGauge jobInfo = this.dalamud.ClientState.JobGauges.Get<BLMGauge>();
                if (jobInfo.IsEnoActive) {
                    if (jobInfo.InUmbralIce()) return 3576;
                    return 3577;
                }
                return 3575;
            }

            // Umbral Soul and Transpose
            if (actionID == 149) {
                if (this.dalamud.ClientState.JobGauges.Get<BLMGauge>().InUmbralIce() && level >= 76) return 16506;
                return 149;
            }

            // ASTROLOGIAN

            // Make cards on the same button as draw
            if (actionID == 17055) {
                byte x = Marshal.ReadByte(jobInfo, 0x10);
                switch (x) {
                    case 1:
                        return 4401;
                    case 2:
                        return 4404;
                    case 3:
                        return 4402;
                    case 4:
                        return 4403;
                    case 5:
                        return 4405;
                    case 6:
                        return 4406;
                    case 0x70:
                        return 7444;
                    case 0x80:
                        return 7445;
                    default:
                        return 3590;
                }
            }

            // SUMMONER

            // DWT changes. 
            // Now contains DWT, Deathflare, Summon Bahamut, Enkindle Bahamut, FBT, and Enkindle Phoenix.
            // What a monster of a button.
            if (actionID == 3581) {
                byte stackState = Marshal.ReadByte(jobInfo, 0x10);
                if (Marshal.ReadInt16(jobInfo, 0xc) > 0) {
                    if (Marshal.ReadInt16(jobInfo, 0xe) > 0) {
                        if (stackState > 0) return 16516;
                        return 7429;
                    }
                    return 3582;
                }
                else {
                    if (stackState == 0) return 3581;
                    if (stackState == 8) return 7427;
                    if (stackState == 0x10) return 16513;
                    return 3581;
                }
            }

            // SCHOLAR

            // Change Fey Blessing into Consolation when Seraph is out.
            if (actionID == 16543) {
                if (Marshal.ReadInt16(jobInfo, 0x10) > 0) return 16546;
                return 16543;
            }

            // DANCER
            
            // Standard Step is one button.
            if (actionID == 15997) {
                DNCGauge gauge = this.dalamud.ClientState.JobGauges.Get<DNCGauge>();
                if (gauge.IsDancing()) {
                    if (gauge.NumCompleteSteps == 2) {
                        return 16192;
                    }
                    else {
                        // C# can't implicitly cast from int to ulong.
                        return (ulong)(15999 + gauge.StepOrder[gauge.NumCompleteSteps] - 1);
                    }
                }
                return 15997;
            }

            // Technical Step is one button.
            if (actionID == 15998) {
                DNCGauge gauge = this.dalamud.ClientState.JobGauges.Get<DNCGauge>();
                if (gauge.IsDancing()) {
                    if (gauge.NumCompleteSteps == 4) {
                        return 16196;
                    }
                    else {
                        // C# can't implicitly cast from int to ulong.
                        return (ulong)(15999 + gauge.StepOrder[gauge.NumCompleteSteps] - 1);
                    }
                }
                return 15998;
            }

            // Fountain changes into Fountain combo, prioritizing procs over combo,
            // and Fountainfall over Reverse Cascade.
            if (actionID == 15990) {
                if (activeBuffArray != null) {
                    if (SearchBuffArray(1815)) return 15992;
                    if (SearchBuffArray(1814)) return 15991;
                }
                if (comboTime > 0) {
                    if (lastMove == 15989) return 15990;
                }
                return 15989;
            }

            // AoE GCDs are split into two buttons, because priority matters
            // differently in different single-target moments. Thanks yoship.
            // Replaces each GCD with its procced version.
            if (actionID == 15994) {
                if (activeBuffArray != null) {
                    if (SearchBuffArray(1817)) return 15996;
                }
                return 15994;
            }

            if (actionID == 15993) {
                if (activeBuffArray != null) {
                    if (SearchBuffArray(1816)) return 15995;
                }
                return 15993;
            }

            // Fan Dance changes into Fan Dance 3 while flourishing.
            if (actionID == 16007) {
                if (activeBuffArray != null) {
                    if (SearchBuffArray(1820)) return 16009;
                }

                return 16007;
            }

            // Fan Dance 2 changes into Fan Dance 3 while flourishing.
            if (actionID == 16008) {
                if (activeBuffArray != null) {
                    if (SearchBuffArray(1820)) return 16009;
                }
                return 16008;
            }


            return this.iconHook.Original(self, actionID);
        }

        private unsafe bool SearchBuffArray(short needle) {
            for (int i = 0; i < 60; i++) {
                if (Marshal.ReadInt16(activeBuffArray + 4 * i) == needle) return true;
            }
            return false;
        }

        private delegate ulong OnIconDetour(byte param1, uint param2);

        public delegate ulong OnIconDelegate(byte param1, uint param2);

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
