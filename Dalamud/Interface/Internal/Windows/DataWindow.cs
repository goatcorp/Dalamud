using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Numerics;

using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Dalamud.Game.ClientState.Structs.JobGauge;
using Dalamud.Game.Internal.Gui.Addon;
using Dalamud.Game.Internal.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// Class responsible for drawing the data/debug window.
    /// </summary>
    internal class DataWindow : Window
    {
        private readonly Dalamud dalamud;
        private readonly string[] dataKindNames = Enum.GetNames(typeof(DataKind)).Select(k => k.Replace("_", " ")).ToArray();

        private bool wasReady;
        private string serverOpString;
        private DataKind currentKind;

        private bool drawActors = false;
        private float maxActorDrawDistance = 20;

        private string inputSig = string.Empty;
        private IntPtr sigResult = IntPtr.Zero;

        private string inputAddonName = string.Empty;
        private int inputAddonIndex;
        private Addon resultAddon;

        private IntPtr findAgentInterfacePtr;

        private bool resolveGameData = false;

        private UIDebug addonInspector = null;

        private string inputTextToast = string.Empty;
        private int toastPosition = 0;
        private int toastSpeed = 0;
        private int questToastPosition = 0;
        private bool questToastSound = false;
        private int questToastIconId = 0;
        private bool questToastCheckmark = false;

        private string inputTexPath = string.Empty;
        private TextureWrap debugTex = null;
        private Vector2 inputTexUv0 = Vector2.Zero;
        private Vector2 inputTexUv1 = Vector2.One;
        private Vector4 inputTintCol = Vector4.One;
        private Vector2 inputTexScale = Vector2.Zero;

        private uint copyButtonIndex = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataWindow"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance to access data of.</param>
        public DataWindow(Dalamud dalamud)
            : base("Dalamud Data")
        {
            this.dalamud = dalamud;

            this.Size = new Vector2(500, 500);
            this.SizeCondition = ImGuiCond.FirstUseEver;

            this.Load();
        }

        private enum DataKind
        {
            Server_OpCode,
            Address,
            Actor_Table,
            Fate_Table,
            Font_Test,
            Party_List,
            Plugin_IPC,
            Condition,
            Gauge,
            Command,
            Addon,
            Addon_Inspector,
            StartInfo,
            Target,
            Toast,
            ImGui,
            Tex,
            Gamepad,
        }

        /// <summary>
        /// Set the DataKind dropdown menu.
        /// </summary>
        /// <param name="dataKind">Data kind name, can be lower and/or without spaces.</param>
        public void SetDataKind(string dataKind)
        {
            if (string.IsNullOrEmpty(dataKind))
                return;

            if (dataKind == "ai")
                dataKind = "Addon Inspector";

            dataKind = dataKind.Replace(" ", string.Empty).ToLower();
            var dataKinds = Enum.GetValues(typeof(DataKind))
                .Cast<DataKind>()
                .Where(k => nameof(k).Replace("_", string.Empty).ToLower() == dataKind)
                .ToList();

            if (dataKinds.Count > 0)
            {
                this.currentKind = dataKinds.First();
            }
            else
            {
                this.dalamud.Framework.Gui.Chat.PrintError("/xldata: Invalid Data Type");
            }
        }

        /// <summary>
        /// Draw the window via ImGui.
        /// </summary>
        public override void Draw()
        {
            this.copyButtonIndex = 0;

            // Main window
            if (ImGui.Button("Force Reload"))
                this.Load();
            ImGui.SameLine();
            var copy = ImGui.Button("Copy all");
            ImGui.SameLine();

            var currentKindIndex = (int)this.currentKind;
            if (ImGui.Combo("Data kind", ref currentKindIndex, this.dataKindNames, this.dataKindNames.Length))
            {
                this.currentKind = (DataKind)currentKindIndex;
            }

            ImGui.Checkbox("Resolve GameData", ref this.resolveGameData);

            ImGui.BeginChild("scrolling", new Vector2(0, 0), false, ImGuiWindowFlags.HorizontalScrollbar);

            if (copy)
                ImGui.LogToClipboard();

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            try
            {
                if (this.wasReady)
                {
                    switch (this.currentKind)
                    {
                        case DataKind.Server_OpCode:
                            this.DrawServerOpCode();
                            break;

                        case DataKind.Address:
                            this.DrawAddress();
                            break;

                        case DataKind.Actor_Table:
                            this.DrawActorTable();
                            break;

                        case DataKind.Fate_Table:
                            this.DrawFateTable();
                            break;

                        case DataKind.Font_Test:
                            this.DrawFontTest();
                            break;

                        case DataKind.Party_List:
                            this.DrawPartyList();
                            break;

                        case DataKind.Plugin_IPC:
                            this.DrawPluginIPC();
                            break;

                        case DataKind.Condition:
                            this.DrawCondition();
                            break;

                        case DataKind.Gauge:
                            this.DrawGauge();
                            break;

                        case DataKind.Command:
                            this.DrawCommand();
                            break;

                        case DataKind.Addon:
                            this.DrawAddon();
                            break;

                        case DataKind.Addon_Inspector:
                            this.DrawAddonInspector();
                            break;

                        case DataKind.StartInfo:
                            this.DrawStartInfo();
                            break;

                        case DataKind.Target:
                            this.DrawTarget();
                            break;

                        case DataKind.Toast:
                            this.DrawToast();
                            break;

                        case DataKind.ImGui:
                            this.DrawImGui();
                            break;

                        case DataKind.Tex:
                            this.DrawTex();
                            break;

                        case DataKind.Gamepad:
                            this.DrawGamepad();
                            break;
                    }
                }
                else
                {
                    ImGui.TextUnformatted("Data not ready.");
                }
            }
            catch (Exception ex)
            {
                ImGui.TextUnformatted(ex.ToString());
            }

            ImGui.PopStyleVar();

            ImGui.EndChild();
        }

        private void DrawServerOpCode()
        {
            ImGui.TextUnformatted(this.serverOpString);
        }

        private void DrawAddress()
        {
            ImGui.InputText(".text sig", ref this.inputSig, 400);
            if (ImGui.Button("Resolve"))
            {
                try
                {
                    this.sigResult = this.dalamud.SigScanner.ScanText(this.inputSig);
                }
                catch (KeyNotFoundException)
                {
                    this.sigResult = new IntPtr(-1);
                }
            }

            ImGui.Text($"Result: {this.sigResult.ToInt64():X}");
            ImGui.SameLine();
            if (ImGui.Button($"C{this.copyButtonIndex++}"))
                ImGui.SetClipboardText(this.sigResult.ToInt64().ToString("x"));

            foreach (var debugScannedValue in BaseAddressResolver.DebugScannedValues)
            {
                ImGui.TextUnformatted($"{debugScannedValue.Key}");
                foreach (var valueTuple in debugScannedValue.Value)
                {
                    ImGui.TextUnformatted(
                        $"      {valueTuple.ClassName} - 0x{valueTuple.Address.ToInt64():x}");
                    ImGui.SameLine();

                    if (ImGui.Button($"C##copyAddress{this.copyButtonIndex++}"))
                        ImGui.SetClipboardText(valueTuple.Address.ToInt64().ToString("x"));
                }
            }
        }

        private void DrawActorTable()
        {
            var stateString = string.Empty;

            // LocalPlayer is null in a number of situations (at least with the current visible-actors list)
            // which would crash here.
            if (this.dalamud.ClientState.Actors.Length == 0)
            {
                ImGui.TextUnformatted("Data not ready.");
            }
            else if (this.dalamud.ClientState.LocalPlayer == null)
            {
                ImGui.TextUnformatted("LocalPlayer null.");
            }
            else
            {
                stateString += $"FrameworkBase: {this.dalamud.Framework.Address.BaseAddress.ToInt64():X}\n";
                stateString += $"ActorTableLen: {this.dalamud.ClientState.Actors.Length}\n";
                stateString += $"LocalPlayerName: {this.dalamud.ClientState.LocalPlayer.Name}\n";
                stateString += $"CurrentWorldName: {(this.resolveGameData ? this.dalamud.ClientState.LocalPlayer.CurrentWorld.GameData.Name : this.dalamud.ClientState.LocalPlayer.CurrentWorld.Id.ToString())}\n";
                stateString += $"HomeWorldName: {(this.resolveGameData ? this.dalamud.ClientState.LocalPlayer.HomeWorld.GameData.Name : this.dalamud.ClientState.LocalPlayer.HomeWorld.Id.ToString())}\n";
                stateString += $"LocalCID: {this.dalamud.ClientState.LocalContentId:X}\n";
                stateString += $"LastLinkedItem: {this.dalamud.Framework.Gui.Chat.LastLinkedItemId}\n";
                stateString += $"TerritoryType: {this.dalamud.ClientState.TerritoryType}\n\n";

                ImGui.TextUnformatted(stateString);

                ImGui.Checkbox("Draw actors on screen", ref this.drawActors);
                ImGui.SliderFloat("Draw Distance", ref this.maxActorDrawDistance, 2f, 40f);

                for (var i = 0; i < this.dalamud.ClientState.Actors.Length; i++)
                {
                    var actor = this.dalamud.ClientState.Actors[i];

                    if (actor == null)
                        continue;

                    this.PrintActor(actor, i.ToString());

                    if (this.drawActors && this.dalamud.Framework.Gui.WorldToScreen(actor.Position, out var screenCoords))
                    {
                        // So, while WorldToScreen will return false if the point is off of game client screen, to
                        // to avoid performance issues, we have to manually determine if creating a window would
                        // produce a new viewport, and skip rendering it if so
                        var actorText = $"{actor.Address.ToInt64():X}:{actor.ActorId:X}[{i}] - {actor.ObjectKind} - {actor.Name}";

                        var screenPos = ImGui.GetMainViewport().Pos;
                        var screenSize = ImGui.GetMainViewport().Size;

                        var windowSize = ImGui.CalcTextSize(actorText);

                        // Add some extra safety padding
                        windowSize.X += ImGui.GetStyle().WindowPadding.X + 10;
                        windowSize.Y += ImGui.GetStyle().WindowPadding.Y + 10;

                        if (screenCoords.X + windowSize.X > screenPos.X + screenSize.X ||
                            screenCoords.Y + windowSize.Y > screenPos.Y + screenSize.Y)
                            continue;

                        if (actor.YalmDistanceX > this.maxActorDrawDistance)
                            continue;

                        ImGui.SetNextWindowPos(new Vector2(screenCoords.X, screenCoords.Y));

                        ImGui.SetNextWindowBgAlpha(Math.Max(1f - (actor.YalmDistanceX / this.maxActorDrawDistance), 0.2f));
                        if (ImGui.Begin(
                                $"Actor{i}##ActorWindow{i}",
                                ImGuiWindowFlags.NoDecoration |
                                ImGuiWindowFlags.AlwaysAutoResize |
                                ImGuiWindowFlags.NoSavedSettings |
                                ImGuiWindowFlags.NoMove |
                                ImGuiWindowFlags.NoMouseInputs |
                                ImGuiWindowFlags.NoDocking |
                                ImGuiWindowFlags.NoFocusOnAppearing |
                                ImGuiWindowFlags.NoNav))
                            ImGui.Text(actorText);
                        ImGui.End();
                    }
                }
            }
        }

        private void DrawFateTable()
        {
            var stateString = string.Empty;
            if (this.dalamud.ClientState.Fates.Length == 0)
            {
                ImGui.TextUnformatted("No fates or data not ready.");
            }
            else
            {
                stateString += $"FrameworkBase: {this.dalamud.Framework.Address.BaseAddress.ToInt64():X}\n";
                stateString += $"FateTableLen: {this.dalamud.ClientState.Fates.Length}\n";

                ImGui.TextUnformatted(stateString);

                for (var i = 0; i < this.dalamud.ClientState.Fates.Length; i++)
                {
                    var fate = this.dalamud.ClientState.Fates[i];
                    if (fate == null)
                        continue;

                    var fateString = $"{fate.Address.ToInt64():X}:[{i}]" +
                        $" - Lv.{fate.Level} {fate.Name} ({fate.Progress}%)" +
                        $" - X{fate.Position.X} Y{fate.Position.Y} Z{fate.Position.Z}" +
                        $" - Territory {(this.resolveGameData ? (fate.TerritoryType.GameData?.Name ?? fate.TerritoryType.Id.ToString()) : fate.TerritoryType.Id.ToString())}\n";

                    fateString += $"       StartTimeEpoch: {fate.StartTimeEpoch}" +
                        $" - Duration: {fate.Duration}" +
                        $" - State: {fate.State}" +
                        $" - GameData name: {(this.resolveGameData ? (fate.GameData?.Name ?? fate.FateId.ToString()) : fate.FateId.ToString())}";

                    ImGui.TextUnformatted(fateString);
                    ImGui.SameLine();
                    if (ImGui.Button("C"))
                    {
                        ImGui.SetClipboardText(fate.Address.ToString("X"));
                    }
                }
            }
        }

        private void DrawFontTest()
        {
            var specialChars = string.Empty;

            for (var i = 0xE020; i <= 0xE0DB; i++)
                specialChars += $"0x{i:X} - {(SeIconChar)i} - {(char)i}\n";

            ImGui.TextUnformatted(specialChars);

            foreach (var fontAwesomeIcon in Enum.GetValues(typeof(FontAwesomeIcon)).Cast<FontAwesomeIcon>())
            {
                ImGui.Text(((int)fontAwesomeIcon.ToIconChar()).ToString("X") + " - ");
                ImGui.SameLine();

                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text(fontAwesomeIcon.ToIconString());
                ImGui.PopFont();
            }
        }

        private void DrawPartyList()
        {
            var partyString = string.Empty;

            if (this.dalamud.ClientState.PartyList.Length == 0)
            {
                ImGui.TextUnformatted("Data not ready.");
            }
            else
            {
                partyString += $"{this.dalamud.ClientState.PartyList.Count} Members\n";
                for (var i = 0; i < this.dalamud.ClientState.PartyList.Count; i++)
                {
                    var member = this.dalamud.ClientState.PartyList[i];
                    if (member == null)
                    {
                        partyString +=
                            $"[{i}] was null\n";
                        continue;
                    }

                    partyString +=
                        $"[{i}] {member.CharacterName} - {member.ObjectKind} - {member.Actor.ActorId}\n";
                }

                ImGui.TextUnformatted(partyString);
            }
        }

        private void DrawPluginIPC()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var i1 = new DalamudPluginInterface(this.dalamud, "DalamudTestSub", PluginLoadReason.Unknown);
            var i2 = new DalamudPluginInterface(this.dalamud, "DalamudTestPub", PluginLoadReason.Unknown);

            if (ImGui.Button("Add test sub"))
            {
                i1.Subscribe("DalamudTestPub", o =>
                {
                    dynamic msg = o;
                    Log.Debug(msg.Expand);
                });
            }

            if (ImGui.Button("Add test sub any"))
            {
                i1.SubscribeAny((o, a) =>
                {
                    dynamic msg = a;
                    Log.Debug($"From {o}: {msg.Expand}");
                });
            }

            if (ImGui.Button("Remove test sub"))
                i1.Unsubscribe("DalamudTestPub");

            if (ImGui.Button("Remove test sub any"))
                i1.UnsubscribeAny();

            if (ImGui.Button("Send test message"))
            {
                dynamic testMsg = new ExpandoObject();
                testMsg.Expand = "dong";
                i2.SendMessage(testMsg);
            }

            // This doesn't actually work, so don't mind it - impl relies on plugins being registered in PluginManager
            if (ImGui.Button("Send test message any"))
            {
                dynamic testMsg = new ExpandoObject();
                testMsg.Expand = "dong";
                i2.SendMessage("DalamudTestSub", testMsg);
            }

            foreach (var ipc in this.dalamud.PluginManager.IpcSubscriptions)
                ImGui.Text($"Source:{ipc.SourcePluginName} Sub:{ipc.SubPluginName}");
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private void DrawCondition()
        {
#if DEBUG
            ImGui.Text($"ptr: 0x{this.dalamud.ClientState.Condition.ConditionArrayBase.ToInt64():X}");
#endif

            ImGui.Text("Current Conditions:");
            ImGui.Separator();

            var didAny = false;

            for (var i = 0; i < Condition.MaxConditionEntries; i++)
            {
                var typedCondition = (ConditionFlag)i;
                var cond = this.dalamud.ClientState.Condition[typedCondition];

                if (!cond) continue;

                didAny = true;

                ImGui.Text($"ID: {i} Enum: {typedCondition}");
            }

            if (!didAny)
                ImGui.Text("None. Talk to a shop NPC or visit a market board to find out more!!!!!!!");
        }

        private void DrawGauge()
        {
            var gauge = this.dalamud.ClientState.JobGauges.Get<ASTGauge>();
            ImGui.Text($"Moon: {gauge.ContainsSeal(SealType.MOON)} Drawn: {gauge.DrawnCard()}");
        }

        private void DrawCommand()
        {
            foreach (var command in this.dalamud.CommandManager.Commands)
                ImGui.Text($"{command.Key}\n    -> {command.Value.HelpMessage}\n    -> In help: {command.Value.ShowInHelp}\n\n");
        }

        private void DrawAddon()
        {
            ImGui.InputText("Addon name", ref this.inputAddonName, 256);
            ImGui.InputInt("Addon Index", ref this.inputAddonIndex);

            if (ImGui.Button("Get Addon"))
            {
                this.resultAddon =
                    this.dalamud.Framework.Gui.GetAddonByName(
                        this.inputAddonName, this.inputAddonIndex);
            }

            if (ImGui.Button("Find Agent"))
                this.findAgentInterfacePtr = this.dalamud.Framework.Gui.FindAgentInterface(this.inputAddonName);

            if (this.resultAddon != null)
            {
                ImGui.TextUnformatted(
                    $"{this.resultAddon.Name} - 0x{this.resultAddon.Address.ToInt64():x}\n    v:{this.resultAddon.Visible} x:{this.resultAddon.X} y:{this.resultAddon.Y} s:{this.resultAddon.Scale}, w:{this.resultAddon.Width}, h:{this.resultAddon.Height}");
            }

            if (this.findAgentInterfacePtr != IntPtr.Zero)
            {
                ImGui.TextUnformatted(
                    $"Agent: 0x{this.findAgentInterfacePtr.ToInt64():x}");
                ImGui.SameLine();

                if (ImGui.Button("C"))
                    ImGui.SetClipboardText(this.findAgentInterfacePtr.ToInt64().ToString("x"));
            }

            if (ImGui.Button("Get Base UI object"))
            {
                var addr = this.dalamud.Framework.Gui.GetBaseUIObject().ToInt64().ToString("x");
                Log.Information("{0}", addr);
                ImGui.SetClipboardText(addr);
            }
        }

        private void DrawAddonInspector()
        {
            this.addonInspector ??= new UIDebug(this.dalamud);
            this.addonInspector.Draw();
        }

        private void DrawStartInfo()
        {
            ImGui.Text(JsonConvert.SerializeObject(this.dalamud.StartInfo, Formatting.Indented));
        }

        private void DrawTarget()
        {
            var targetMgr = this.dalamud.ClientState.Targets;

            if (targetMgr.CurrentTarget != null)
            {
                this.PrintActor(targetMgr.CurrentTarget, "CurrentTarget");
                Util.ShowObject(targetMgr.CurrentTarget);
            }

            if (targetMgr.FocusTarget != null)
                this.PrintActor(targetMgr.FocusTarget, "FocusTarget");

            if (targetMgr.MouseOverTarget != null)
                this.PrintActor(targetMgr.MouseOverTarget, "MouseOverTarget");

            if (targetMgr.PreviousTarget != null)
                this.PrintActor(targetMgr.PreviousTarget, "PreviousTarget");

            if (targetMgr.SoftTarget != null)
                this.PrintActor(targetMgr.SoftTarget, "SoftTarget");

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
            }
            else
            {
                ImGui.Text("LocalPlayer is null.");
            }
        }

        private void DrawToast()
        {
            ImGui.InputText("Toast text", ref this.inputTextToast, 200);

            ImGui.Combo("Toast Position", ref this.toastPosition, new[] { "Bottom", "Top", }, 2);
            ImGui.Combo("Toast Speed", ref this.toastSpeed, new[] { "Slow", "Fast", }, 2);
            ImGui.Combo("Quest Toast Position", ref this.questToastPosition, new[] { "Centre", "Right", "Left" }, 3);
            ImGui.Checkbox("Quest Checkmark", ref this.questToastCheckmark);
            ImGui.Checkbox("Quest Play Sound", ref this.questToastSound);
            ImGui.InputInt("Quest Icon ID", ref this.questToastIconId);

            ImGuiHelpers.ScaledDummy(new Vector2(10, 10));

            if (ImGui.Button("Show toast"))
            {
                this.dalamud.Framework.Gui.Toast.ShowNormal(this.inputTextToast, new ToastOptions
                {
                    Position = (ToastPosition)this.toastPosition,
                    Speed = (ToastSpeed)this.toastSpeed,
                });
            }

            if (ImGui.Button("Show Quest toast"))
            {
                this.dalamud.Framework.Gui.Toast.ShowQuest(this.inputTextToast, new QuestToastOptions
                {
                    Position = (QuestToastPosition)this.questToastPosition,
                    DisplayCheckmark = this.questToastCheckmark,
                    IconId = (uint)this.questToastIconId,
                    PlaySound = this.questToastSound,
                });
            }

            if (ImGui.Button("Show Error toast"))
            {
                this.dalamud.Framework.Gui.Toast.ShowError(this.inputTextToast);
            }
        }

        private void DrawImGui()
        {
            ImGui.Text("Monitor count: " + ImGui.GetPlatformIO().Monitors.Size);
            ImGui.Text("OverrideGameCursor: " + this.dalamud.InterfaceManager.OverrideGameCursor);

            ImGui.Button("THIS IS A BUTTON###hoverTestButton");
            this.dalamud.InterfaceManager.OverrideGameCursor = !ImGui.IsItemHovered();
        }

        private void DrawTex()
        {
            ImGui.InputText("Tex Path", ref this.inputTexPath, 255);
            ImGui.InputFloat2("UV0", ref this.inputTexUv0);
            ImGui.InputFloat2("UV1", ref this.inputTexUv1);
            ImGui.InputFloat4("Tint", ref this.inputTintCol);
            ImGui.InputFloat2("Scale", ref this.inputTexScale);

            if (ImGui.Button("Load Tex"))
            {
                try
                {
                    this.debugTex = this.dalamud.Data.GetImGuiTexture(this.inputTexPath);
                    this.inputTexScale = new Vector2(this.debugTex.Width, this.debugTex.Height);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Could not load tex.");
                }
            }

            ImGuiHelpers.ScaledDummy(10);

            if (this.debugTex != null)
            {
                ImGui.Image(this.debugTex.ImGuiHandle, this.inputTexScale, this.inputTexUv0, this.inputTexUv1, this.inputTintCol);
                ImGuiHelpers.ScaledDummy(5);
                Util.ShowObject(this.debugTex);
            }
        }

        private void DrawGamepad()
        {
            static void DrawHelper(string text, uint mask, Func<GamepadButtons, float> resolve)
            {
                ImGui.Text($"{text} {mask:X4}");
                ImGui.Text($"DPadLeft {resolve(GamepadButtons.DpadLeft)} " +
                           $"DPadUp {resolve(GamepadButtons.DpadUp)} " +
                           $"DPadRight {resolve(GamepadButtons.DpadRight)} " +
                           $"DPadDown {resolve(GamepadButtons.DpadDown)} ");
                ImGui.Text($"West {resolve(GamepadButtons.West)} " +
                           $"North {resolve(GamepadButtons.North)} " +
                           $"East {resolve(GamepadButtons.East)} " +
                           $"South {resolve(GamepadButtons.South)} ");
                ImGui.Text($"L1 {resolve(GamepadButtons.L1)} " +
                           $"L2 {resolve(GamepadButtons.L2)} " +
                           $"R1 {resolve(GamepadButtons.R1)} " +
                           $"R2 {resolve(GamepadButtons.R2)} ");
                ImGui.Text($"Select {resolve(GamepadButtons.Select)} " +
                           $"Start {resolve(GamepadButtons.Start)} " +
                           $"L3 {resolve(GamepadButtons.L3)} " +
                           $"R3 {resolve(GamepadButtons.R3)} ");
            }
#if DEBUG
            ImGui.Text($"GamepadInput 0x{this.dalamud.ClientState.GamepadState.GamepadInput.ToInt64():X}");

            if (ImGui.IsItemHovered())
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            if (ImGui.IsItemClicked())
                ImGui.SetClipboardText($"0x{this.dalamud.ClientState.GamepadState.GamepadInput.ToInt64():X}");
#endif

            DrawHelper(
                "Buttons Raw",
                this.dalamud.ClientState.GamepadState.ButtonsRaw,
                this.dalamud.ClientState.GamepadState.Raw);
            DrawHelper(
                "Buttons Pressed",
                this.dalamud.ClientState.GamepadState.ButtonsPressed,
                this.dalamud.ClientState.GamepadState.Pressed);
            DrawHelper(
                "Buttons Repeat",
                this.dalamud.ClientState.GamepadState.ButtonsRepeat,
                this.dalamud.ClientState.GamepadState.Repeat);
            DrawHelper(
                "Buttons Released",
                this.dalamud.ClientState.GamepadState.ButtonsReleased,
                this.dalamud.ClientState.GamepadState.Released);
            ImGui.Text($"LeftStickLeft {this.dalamud.ClientState.GamepadState.LeftStickLeft:0.00} " +
                       $"LeftStickUp {this.dalamud.ClientState.GamepadState.LeftStickUp:0.00} " +
                       $"LeftStickRight {this.dalamud.ClientState.GamepadState.LeftStickRight:0.00} " +
                       $"LeftStickDown {this.dalamud.ClientState.GamepadState.LeftStickDown:0.00} ");
            ImGui.Text($"RightStickLeft {this.dalamud.ClientState.GamepadState.RightStickLeft:0.00} " +
                       $"RightStickUp {this.dalamud.ClientState.GamepadState.RightStickUp:0.00} " +
                       $"RightStickRight {this.dalamud.ClientState.GamepadState.RightStickRight:0.00} " +
                       $"RightStickDown {this.dalamud.ClientState.GamepadState.RightStickDown:0.00} ");
        }

        private void Load()
        {
            if (this.dalamud.Data.IsDataReady)
            {
                this.serverOpString = JsonConvert.SerializeObject(this.dalamud.Data.ServerOpCodes, Formatting.Indented);
                this.wasReady = true;
            }
        }

        private void PrintActor(Actor actor, string tag)
        {
            var actorString =
                $"{actor.Address.ToInt64():X}:{actor.ActorId:X}[{tag}] - {actor.ObjectKind} - {actor.Name} - X{actor.Position.X} Y{actor.Position.Y} Z{actor.Position.Z} D{actor.YalmDistanceX} R{actor.Rotation} - Target: {actor.TargetActorID:X}\n";

            if (actor is Npc npc)
                actorString += $"       DataId: {npc.BaseId}  NameId:{npc.NameId}\n";

            if (actor is Chara chara)
            {
                actorString +=
                    $"       Level: {chara.Level} ClassJob: {(this.resolveGameData ? chara.ClassJob.GameData.Name : chara.ClassJob.Id.ToString())} CHP: {chara.CurrentHp} MHP: {chara.MaxHp} CMP: {chara.CurrentMp} MMP: {chara.MaxMp}\n       Customize: {BitConverter.ToString(chara.Customize).Replace("-", " ")} StatusFlags: {chara.StatusFlags}\n";
            }

            if (actor is PlayerCharacter pc)
            {
                actorString +=
                    $"       HomeWorld: {(this.resolveGameData ? pc.HomeWorld.GameData.Name : pc.HomeWorld.Id.ToString())} CurrentWorld: {(this.resolveGameData ? pc.CurrentWorld.GameData.Name : pc.CurrentWorld.Id.ToString())} FC: {pc.CompanyTag}\n";
            }

            ImGui.TextUnformatted(actorString);
            ImGui.SameLine();
            if (ImGui.Button($"C##{this.copyButtonIndex++}"))
            {
                ImGui.SetClipboardText(actor.Address.ToInt64().ToString("X"));
            }
        }
    }
}
