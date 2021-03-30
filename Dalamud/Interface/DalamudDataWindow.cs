using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Mime;
using System.Numerics;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Dalamud.Game.ClientState.Structs.JobGauge;
using Dalamud.Game.Internal;
using Dalamud.Game.Internal.Gui.Addon;
using Dalamud.Plugin;
using ImGuiNET;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Serilog;
using SharpDX.Direct3D11;

namespace Dalamud.Interface
{
    class DalamudDataWindow {
        private Dalamud dalamud;

        private bool wasReady;
        private string serverOpString;

        private int currentKind;

        private bool drawActors = false;
        private float maxActorDrawDistance = 20;

        private string inputSig = string.Empty;
        private IntPtr sigResult = IntPtr.Zero;

        private string inputAddonName = string.Empty;
        private int inputAddonIndex;
        private Addon resultAddon;

        private bool resolveGameData = false;

        private UIDebug UIDebug = null;

        private uint copyButtonIndex = 0;
        
        public DalamudDataWindow(Dalamud dalamud) {
            this.dalamud = dalamud;

            Load();
        }

        private void Load() {
            if (this.dalamud.Data.IsDataReady)
            {
                this.serverOpString = JsonConvert.SerializeObject(this.dalamud.Data.ServerOpCodes, Formatting.Indented);
                this.wasReady = true;
            }
        }

        public bool Draw() {
            this.copyButtonIndex = 0;
            ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.FirstUseEver);

            var isOpen = true;

            if (!ImGui.Begin("Dalamud Data", ref isOpen)) {
                ImGui.End();
                return false;
            }

            // Main window
            if (ImGui.Button("Force Reload"))
                Load();
            ImGui.SameLine();
            var copy = ImGui.Button("Copy all");
            ImGui.SameLine();
            ImGui.Combo("Data kind", ref this.currentKind, new[] {"ServerOpCode", "Address", "Actor Table", "Font Test", "Party List", "Plugin IPC", "Condition", "Gauge", "Command", "Addon", "Addon Inspector", "StartInfo", "Target" },
                        13);
            ImGui.Checkbox("Resolve GameData", ref this.resolveGameData);

            ImGui.BeginChild("scrolling", new Vector2(0, 0), false, ImGuiWindowFlags.HorizontalScrollbar);

            if (copy)
                ImGui.LogToClipboard();

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            try {
                if (this.wasReady)
                    switch (this.currentKind) {
                        case 0:
                            ImGui.TextUnformatted(this.serverOpString);
                            break;
                        case 1:

                            ImGui.InputText(".text sig", ref this.inputSig, 400);
                            if (ImGui.Button("Resolve")) {
                                try {
                                    this.sigResult = this.dalamud.SigScanner.ScanText(this.inputSig);
                                } catch (KeyNotFoundException) {
                                    this.sigResult = new IntPtr(-1);
                                }
                            }
                            ImGui.Text($"Result: {this.sigResult.ToInt64():X}");
                            ImGui.SameLine();
                            if (ImGui.Button($"C{this.copyButtonIndex++}")) {
                                ImGui.SetClipboardText(this.sigResult.ToInt64().ToString("x"));
                            }

                            foreach (var debugScannedValue in BaseAddressResolver.DebugScannedValues) {
                                ImGui.TextUnformatted($"{debugScannedValue.Key}");
                                foreach (var valueTuple in debugScannedValue.Value) {
                                    ImGui.TextUnformatted(
                                        $"      {valueTuple.Item1} - 0x{valueTuple.Item2.ToInt64():x}");
                                    ImGui.SameLine();

                                    if (ImGui.Button($"C##copyAddress{copyButtonIndex++}")) {
                                        ImGui.SetClipboardText(valueTuple.Item2.ToInt64().ToString("x"));
                                    }
                                }
                            }

                            break;

                        // AT
                        case 2: {
                            var stateString = string.Empty;
                            // LocalPlayer is null in a number of situations (at least with the current visible-actors list)
                            // which would crash here.
                            if (this.dalamud.ClientState.Actors.Length == 0) {
                                ImGui.TextUnformatted("Data not ready.");
                            } else if (this.dalamud.ClientState.LocalPlayer == null) {
                                ImGui.TextUnformatted("LocalPlayer null.");
                            } else {
                                stateString +=
                                    $"FrameworkBase: {this.dalamud.Framework.Address.BaseAddress.ToInt64():X}\n";

                                stateString += $"ActorTableLen: {this.dalamud.ClientState.Actors.Length}\n";
                                stateString += $"LocalPlayerName: {this.dalamud.ClientState.LocalPlayer.Name}\n";
                                stateString +=
                                    $"CurrentWorldName: {(this.resolveGameData ? this.dalamud.ClientState.LocalPlayer.CurrentWorld.GameData.Name : this.dalamud.ClientState.LocalPlayer.CurrentWorld.Id.ToString())}\n";
                                stateString +=
                                    $"HomeWorldName: {(this.resolveGameData ? this.dalamud.ClientState.LocalPlayer.HomeWorld.GameData.Name : this.dalamud.ClientState.LocalPlayer.HomeWorld.Id.ToString())}\n";
                                stateString += $"LocalCID: {this.dalamud.ClientState.LocalContentId:X}\n";
                                stateString +=
                                    $"LastLinkedItem: {this.dalamud.Framework.Gui.Chat.LastLinkedItemId.ToString()}\n";
                                stateString += $"TerritoryType: {this.dalamud.ClientState.TerritoryType}\n\n";

                                ImGui.TextUnformatted(stateString);

                                ImGui.Checkbox("Draw actors on screen", ref this.drawActors);
                                ImGui.SliderFloat("Draw Distance", ref this.maxActorDrawDistance, 2f, 40f);

                                for (var i = 0; i < this.dalamud.ClientState.Actors.Length; i++) {
                                    var actor = this.dalamud.ClientState.Actors[i];

                                    if (actor == null)
                                        continue;

                                    PrintActor(actor, i.ToString());

                                    if (this.drawActors &&
                                        this.dalamud.Framework.Gui.WorldToScreen(actor.Position, out var screenCoords)
                                    ) {
                                        ImGui.PushID("ActorWindow" + i);
                                        ImGui.SetNextWindowPos(new Vector2(screenCoords.X, screenCoords.Y));

                                        if (actor.YalmDistanceX > this.maxActorDrawDistance)
                                            continue;

                                        ImGui.SetNextWindowBgAlpha(
                                            Math.Max(1f - (actor.YalmDistanceX / this.maxActorDrawDistance), 0.2f));
                                        if (ImGui.Begin("Actor" + i,
                                                        ImGuiWindowFlags.NoDecoration |
                                                        ImGuiWindowFlags.AlwaysAutoResize |
                                                        ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoMove |
                                                        ImGuiWindowFlags.NoMouseInputs |
                                                        ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav)) {
                                            ImGui.Text(
                                                $"{actor.Address.ToInt64():X}:{actor.ActorId:X}[{i}] - {actor.ObjectKind} - {actor.Name}");
                                            ImGui.End();
                                        }

                                        ImGui.PopID();
                                    }
                                }
                            }
                        }
                            break;

                        // Font
                        case 3:
                            var specialChars = string.Empty;
                            for (var i = 0xE020; i <= 0xE0DB; i++) {
                                specialChars += $"0x{i:X} - {(SeIconChar) i} - {(char) i}\n";
                            }

                            ImGui.TextUnformatted(specialChars);

                            foreach (var fontAwesomeIcon in Enum.GetValues(typeof(FontAwesomeIcon))
                                                                .Cast<FontAwesomeIcon>()) {
                                ImGui.Text(((int) fontAwesomeIcon.ToIconChar()).ToString("X") + " - ");
                                ImGui.SameLine();

                                ImGui.PushFont(InterfaceManager.IconFont);
                                ImGui.Text(fontAwesomeIcon.ToIconString());
                                ImGui.PopFont();
                            }

                            break;

                        // Party
                        case 4:
                            var partyString = string.Empty;

                            if (this.dalamud.ClientState.PartyList.Length == 0) {
                                ImGui.TextUnformatted("Data not ready.");
                            } else {

                                partyString += $"{this.dalamud.ClientState.PartyList.Count} Members\n";
                                for (var i = 0; i < this.dalamud.ClientState.PartyList.Count; i++) {
                                    var member = this.dalamud.ClientState.PartyList[i];
                                    if (member == null) {
                                        partyString +=
                                            $"[{i}] was null\n";
                                        continue;
                                    }

                                    partyString +=
                                        $"[{i}] {member.CharacterName} - {member.ObjectKind} - {member.Actor.ActorId}\n";
                                }

                                ImGui.TextUnformatted(partyString);
                            }

                            break;

                        // Subscriptions
                        case 5:
                            var i1 = new DalamudPluginInterface(this.dalamud, "DalamudTestSub", null,
                                                                PluginLoadReason.Boot);
                            var i2 = new DalamudPluginInterface(this.dalamud, "DalamudTestPub", null,
                                                                PluginLoadReason.Boot);

                            if (ImGui.Button("Add test sub"))
                                i1.Subscribe("DalamudTestPub", o => {
                                    dynamic msg = o;
                                    Log.Debug(msg.Expand);
                                });

                            if (ImGui.Button("Add test sub any"))
                                i1.SubscribeAny((o, a) => {
                                    dynamic msg = a;
                                    Log.Debug($"From {o}: {msg.Expand}");
                                });

                            if (ImGui.Button("Remove test sub")) i1.Unsubscribe("DalamudTestPub");

                            if (ImGui.Button("Remove test sub any")) i1.UnsubscribeAny();

                            if (ImGui.Button("Send test message")) {
                                dynamic testMsg = new ExpandoObject();
                                testMsg.Expand = "dong";
                                i2.SendMessage(testMsg);
                            }

                            // This doesn't actually work, so don't mind it - impl relies on plugins being registered in PluginManager
                            if (ImGui.Button("Send test message any")) {
                                dynamic testMsg = new ExpandoObject();
                                testMsg.Expand = "dong";
                                i2.SendMessage("DalamudTestSub", testMsg);
                            }

                            foreach (var sub in this.dalamud.PluginManager.IpcSubscriptions) {
                                ImGui.Text($"Source:{sub.SourcePluginName} Sub:{sub.SubPluginName}");
                            }

                            break;

                        // Condition
                        case 6:
#if DEBUG
                            ImGui.Text($"ptr: {this.dalamud.ClientState.Condition.conditionArrayBase.ToString("X16")}");
#endif

                            ImGui.Text("Current Conditions:");
                            ImGui.Separator();

                            var didAny = false;

                            for (var i = 0; i < Condition.MaxConditionEntries; i++) {
                                var typedCondition = (ConditionFlag) i;
                                var cond = this.dalamud.ClientState.Condition[typedCondition];

                                if (!cond) {
                                    continue;
                                }

                                didAny = true;

                                ImGui.Text($"ID: {i} Enum: {typedCondition}");
                            }

                            if (!didAny) {
                                ImGui.Text("None. Talk to a shop NPC or visit a market board to find out more!!!!!!!");
                            }

                            break;

                        // Gauge
                        case 7:
                            var gauge = this.dalamud.ClientState.JobGauges.Get<ASTGauge>();
                            ImGui.Text($"Moon: {gauge.ContainsSeal(SealType.MOON)} Drawn: {gauge.DrawnCard()}");

                            break;

                        // Command
                        case 8:
                            foreach (var command in this.dalamud.CommandManager.Commands) {
                                ImGui.Text(
                                    $"{command.Key}\n    -> {command.Value.HelpMessage}\n    -> In help: {command.Value.ShowInHelp}\n\n");
                            }

                            break;

                        // Addon
                        case 9:
                            ImGui.InputText("Addon name", ref this.inputAddonName, 256);
                            ImGui.InputInt("Addon Index", ref this.inputAddonIndex);

                            if (ImGui.Button("Get Addon")) {
                                this.resultAddon =
                                    this.dalamud.Framework.Gui.GetAddonByName(
                                        this.inputAddonName, this.inputAddonIndex);
                            }

                            if (ImGui.Button("Find Agent")) {
                                this.findAgentInterfacePtr = FindAgentInterface(this.inputAddonName);
                            }

                            if (this.resultAddon != null) {
                                ImGui.TextUnformatted(
                                    $"{this.resultAddon.Name} - 0x{this.resultAddon.Address.ToInt64():x}\n    v:{this.resultAddon.Visible} x:{this.resultAddon.X} y:{this.resultAddon.Y} s:{this.resultAddon.Scale}, w:{this.resultAddon.Width}, h:{this.resultAddon.Height}");
                            }

                            if (this.findAgentInterfacePtr != IntPtr.Zero) {
                                ImGui.TextUnformatted(
                                    $"Agent: 0x{this.findAgentInterfacePtr.ToInt64():x}");
                                ImGui.SameLine();

                                if (ImGui.Button("C"))
                                {
                                    ImGui.SetClipboardText(this.findAgentInterfacePtr.ToInt64().ToString("x"));
                                }
                            }

                            if (ImGui.Button("Get Base UI object")) {
                                var addr = this.dalamud.Framework.Gui.GetBaseUIObject().ToInt64().ToString("x");
                                Log.Information("{0}", addr);
                                ImGui.SetClipboardText(addr);
                            }

                            break;

                        // Addon Inspector
                        case 10:
                        {
                            this.UIDebug ??= new UIDebug(this.dalamud);
                            this.UIDebug.Draw();
                            break;
                        }

                        // StartInfo
                        case 11:
                            ImGui.Text(JsonConvert.SerializeObject(this.dalamud.StartInfo, Formatting.Indented));
                            break;

                        // Target
                        case 12:
                            var targetMgr = this.dalamud.ClientState.Targets;

                            if (targetMgr.CurrentTarget != null)
                                PrintActor(targetMgr.CurrentTarget, "CurrentTarget");

                            if (targetMgr.FocusTarget != null)
                                PrintActor(targetMgr.FocusTarget, "FocusTarget");

                            if (targetMgr.MouseOverTarget != null)
                                PrintActor(targetMgr.MouseOverTarget, "MouseOverTarget");

                            if (targetMgr.PreviousTarget != null)
                                PrintActor(targetMgr.PreviousTarget, "PreviousTarget");

                            if (ImGui.Button("Clear CT"))
                                targetMgr.ClearCurrentTarget();

                            if (ImGui.Button("Clear FT"))
                                targetMgr.ClearFocusTarget();

                            var localPlayer = this.dalamud.ClientState.LocalPlayer;

                            if (localPlayer != null)
                            {
                                if (ImGui.Button("Set CT"))
                                    targetMgr.SetCurrentTarget(localPlayer);

                                if (ImGui.Button("Set FT"))
                                    targetMgr.SetFocusTarget(localPlayer);
                            } else {
                                ImGui.Text("LocalPlayer is null.");
                            }

                            break;
                    }
                else
                    ImGui.TextUnformatted("Data not ready.");
            } catch (Exception ex) {
                ImGui.TextUnformatted(ex.ToString());
            }

            ImGui.PopStyleVar();

            ImGui.EndChild();
            ImGui.End();

            return isOpen;
        }

        private IntPtr findAgentInterfacePtr;

        private unsafe IntPtr FindAgentInterface(string addonName)
        {
            var addon = this.dalamud.Framework.Gui.GetUiObjectByName(addonName, 1);
            if (addon == IntPtr.Zero) return IntPtr.Zero;
            SafeMemory.Read<short>(addon + 0x1CE, out var id);

            if (id == 0)
                _ = SafeMemory.Read(addon + 0x1CC, out id);

            var framework = this.dalamud.Framework.Address.BaseAddress;
            var uiModule = *(IntPtr*)(framework + 0x29F8);
            var agentModule = uiModule + 0xC3E78;
            for (var i = 0; i < 379; i++)
            {
                var agent = *(IntPtr*)(agentModule + 0x20 + i * 8);
                if (agent == IntPtr.Zero)
                    continue;
                if (*(short*)(agent + 0x20) == id)
                    return agent;
            }
            return IntPtr.Zero;
        }

        private void PrintActor(Actor actor, string tag) {
            var actorString =
                $"{actor.Address.ToInt64():X}:{actor.ActorId:X}[{tag}] - {actor.ObjectKind} - {actor.Name} - X{actor.Position.X} Y{actor.Position.Y} Z{actor.Position.Z} D{actor.YalmDistanceX} R{actor.Rotation} - Target: {actor.TargetActorID:X}\n";

            if (actor is Npc npc)
                actorString += $"       DataId: {npc.DataId}  NameId:{npc.NameId}\n";

            if (actor is Chara chara)
                actorString +=
                    $"       Level: {chara.Level} ClassJob: {(this.resolveGameData ? chara.ClassJob.GameData.Name : chara.ClassJob.Id.ToString())} CHP: {chara.CurrentHp} MHP: {chara.MaxHp} CMP: {chara.CurrentMp} MMP: {chara.MaxMp}\n       Customize: {BitConverter.ToString(chara.Customize).Replace("-", " ")}\n";

            if (actor is PlayerCharacter pc)
                actorString +=
                    $"       HomeWorld: {(this.resolveGameData ? pc.HomeWorld.GameData.Name : pc.HomeWorld.Id.ToString())} CurrentWorld: {(this.resolveGameData ? pc.CurrentWorld.GameData.Name : pc.CurrentWorld.Id.ToString())} FC: {pc.CompanyTag}\n";


            ImGui.TextUnformatted(actorString);
            ImGui.SameLine();
            if (ImGui.Button($"C##{this.copyButtonIndex++}")) {
                ImGui.SetClipboardText(actor.Address.ToInt64().ToString("X"));
            }

        }
    }
}
