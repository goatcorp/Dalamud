using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using Dalamud.Memory;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Internal;
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
        private readonly string[] dataKindNames = Enum.GetNames(typeof(DataKind)).Select(k => k.Replace("_", " ")).ToArray();

        private bool wasReady;
        private string serverOpString;
        private DataKind currentKind;

        private bool drawCharas = false;
        private float maxCharaDrawDistance = 20;

        private string inputSig = string.Empty;
        private IntPtr sigResult = IntPtr.Zero;

        private string inputAddonName = string.Empty;
        private int inputAddonIndex;

        private IntPtr findAgentInterfacePtr;

        private bool resolveGameData = false;
        private bool resolveObjects = false;

        private UIDebug addonInspector = null;

        // IPC
        private ICallGateProvider<string, string> ipcPub;
        private ICallGateSubscriber<string, string> ipcSub;
        private string callGateResponse = string.Empty;

        // Toast fields
        private string inputTextToast = string.Empty;
        private int toastPosition = 0;
        private int toastSpeed = 0;
        private int questToastPosition = 0;
        private bool questToastSound = false;
        private int questToastIconId = 0;
        private bool questToastCheckmark = false;

        // Fly text fields
        private int flyActor;
        private FlyTextKind flyKind;
        private int flyVal1;
        private int flyVal2;
        private string flyText1 = string.Empty;
        private string flyText2 = string.Empty;
        private int flyIcon;
        private Vector4 flyColor = new(1, 0, 0, 1);

        // ImGui fields
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
        public DataWindow()
            : base("Dalamud Data")
        {
            this.Size = new Vector2(500, 500);
            this.SizeCondition = ImGuiCond.FirstUseEver;

            this.RespectCloseHotkey = false;

            this.Load();
        }

        private enum DataKind
        {
            Server_OpCode,
            Address,
            Object_Table,
            Fate_Table,
            Font_Test,
            Party_List,
            Buddy_List,
            Plugin_IPC,
            Condition,
            Gauge,
            Command,
            Addon,
            Addon_Inspector,
            StartInfo,
            Target,
            Toast,
            FlyText,
            ImGui,
            Tex,
            KeyState,
            Gamepad,
        }

        /// <inheritdoc/>
        public override void OnOpen()
        {
        }

        /// <inheritdoc/>
        public override void OnClose()
        {
        }

        /// <summary>
        /// Set the DataKind dropdown menu.
        /// </summary>
        /// <param name="dataKind">Data kind name, can be lower and/or without spaces.</param>
        public void SetDataKind(string dataKind)
        {
            if (string.IsNullOrEmpty(dataKind))
                return;

            dataKind = dataKind switch
            {
                "ai" => "Addon Inspector",
                "at" => "Object Table",  // Actor Table
                "ot" => "Object Table",
                _ => dataKind,
            };

            dataKind = dataKind.Replace(" ", string.Empty).ToLower();

            var matched = Enum.GetValues<DataKind>()
                .Where(kind => Enum.GetName(kind).Replace("_", string.Empty).ToLower() == dataKind)
                .FirstOrDefault();

            if (matched != default)
            {
                this.currentKind = matched;
            }
            else
            {
                Service<ChatGui>.Get().PrintError($"/xldata: Invalid data type {dataKind}");
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

                        case DataKind.Object_Table:
                            this.DrawObjectTable();
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

                        case DataKind.Buddy_List:
                            this.DrawBuddyList();
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

                        case DataKind.FlyText:
                            this.DrawFlyText();
                            break;

                        case DataKind.ImGui:
                            this.DrawImGui();
                            break;

                        case DataKind.Tex:
                            this.DrawTex();
                            break;

                        case DataKind.KeyState:
                            this.DrawKeyState();
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
                    var sigScanner = Service<SigScanner>.Get();
                    this.sigResult = sigScanner.ScanText(this.inputSig);
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

        private void DrawObjectTable()
        {
            var chatGui = Service<ChatGui>.Get();
            var clientState = Service<ClientState>.Get();
            var framework = Service<Framework>.Get();
            var gameGui = Service<GameGui>.Get();
            var objectTable = Service<ObjectTable>.Get();

            var stateString = string.Empty;

            if (clientState.LocalPlayer == null)
            {
                ImGui.TextUnformatted("LocalPlayer null.");
            }
            else
            {
                stateString += $"FrameworkBase: {framework.Address.BaseAddress.ToInt64():X}\n";
                stateString += $"ObjectTableLen: {objectTable.Length}\n";
                stateString += $"LocalPlayerName: {clientState.LocalPlayer.Name}\n";
                stateString += $"CurrentWorldName: {(this.resolveGameData ? clientState.LocalPlayer.CurrentWorld.GameData.Name : clientState.LocalPlayer.CurrentWorld.Id.ToString())}\n";
                stateString += $"HomeWorldName: {(this.resolveGameData ? clientState.LocalPlayer.HomeWorld.GameData.Name : clientState.LocalPlayer.HomeWorld.Id.ToString())}\n";
                stateString += $"LocalCID: {clientState.LocalContentId:X}\n";
                stateString += $"LastLinkedItem: {chatGui.LastLinkedItemId}\n";
                stateString += $"TerritoryType: {clientState.TerritoryType}\n\n";

                ImGui.TextUnformatted(stateString);

                ImGui.Checkbox("Draw characters on screen", ref this.drawCharas);
                ImGui.SliderFloat("Draw Distance", ref this.maxCharaDrawDistance, 2f, 40f);

                for (var i = 0; i < objectTable.Length; i++)
                {
                    var obj = objectTable[i];

                    if (obj == null)
                        continue;

                    this.PrintGameObject(obj, i.ToString());

                    if (this.drawCharas && gameGui.WorldToScreen(obj.Position, out var screenCoords))
                    {
                        // So, while WorldToScreen will return false if the point is off of game client screen, to
                        // to avoid performance issues, we have to manually determine if creating a window would
                        // produce a new viewport, and skip rendering it if so
                        var objectText = $"{obj.Address.ToInt64():X}:{obj.ObjectId:X}[{i}] - {obj.ObjectKind} - {obj.Name}";

                        var screenPos = ImGui.GetMainViewport().Pos;
                        var screenSize = ImGui.GetMainViewport().Size;

                        var windowSize = ImGui.CalcTextSize(objectText);

                        // Add some extra safety padding
                        windowSize.X += ImGui.GetStyle().WindowPadding.X + 10;
                        windowSize.Y += ImGui.GetStyle().WindowPadding.Y + 10;

                        if (screenCoords.X + windowSize.X > screenPos.X + screenSize.X ||
                            screenCoords.Y + windowSize.Y > screenPos.Y + screenSize.Y)
                            continue;

                        if (obj.YalmDistanceX > this.maxCharaDrawDistance)
                            continue;

                        ImGui.SetNextWindowPos(new Vector2(screenCoords.X, screenCoords.Y));

                        ImGui.SetNextWindowBgAlpha(Math.Max(1f - (obj.YalmDistanceX / this.maxCharaDrawDistance), 0.2f));
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
                            ImGui.Text(objectText);
                        ImGui.End();
                    }
                }
            }
        }

        private void DrawFateTable()
        {
            var fateTable = Service<FateTable>.Get();
            var framework = Service<Framework>.Get();

            var stateString = string.Empty;
            if (fateTable.Length == 0)
            {
                ImGui.TextUnformatted("No fates or data not ready.");
            }
            else
            {
                stateString += $"FrameworkBase: {framework.Address.BaseAddress.ToInt64():X}\n";
                stateString += $"FateTableLen: {fateTable.Length}\n";

                ImGui.TextUnformatted(stateString);

                for (var i = 0; i < fateTable.Length; i++)
                {
                    var fate = fateTable[i];
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
            var partyList = Service<PartyList>.Get();

            ImGui.Checkbox("Resolve Actors", ref this.resolveObjects);

            ImGui.Text($"GroupManager: {partyList.GroupManagerAddress.ToInt64():X}");
            ImGui.Text($"GroupList: {partyList.GroupListAddress.ToInt64():X}");
            ImGui.Text($"AllianceList: {partyList.AllianceListAddress.ToInt64():X}");

            ImGui.Text($"{partyList.Length} Members");

            for (var i = 0; i < partyList.Length; i++)
            {
                var member = partyList[i];
                if (member == null)
                {
                    ImGui.Text($"[{i}] was null");
                    continue;
                }

                ImGui.Text($"[{i}] {member.Address.ToInt64():X} - {member.Name} - {member.GameObject.ObjectId}");
                if (this.resolveObjects)
                {
                    var actor = member.GameObject;
                    if (actor == null)
                    {
                        ImGui.Text("Actor was null");
                    }
                    else
                    {
                        this.PrintGameObject(actor, "-");
                    }
                }
            }
        }

        private void DrawBuddyList()
        {
            var buddyList = Service<BuddyList>.Get();

            ImGui.Checkbox("Resolve Actors", ref this.resolveObjects);

            ImGui.Text($"BuddyList: {buddyList.BuddyListAddress.ToInt64():X}");
            {
                var member = buddyList.CompanionBuddy;
                if (member == null)
                {
                    ImGui.Text("[Companion] null");
                }
                else
                {
                    ImGui.Text($"[Companion] {member.Address.ToInt64():X} - {member.ObjectId} - {member.DataID}");
                    if (this.resolveObjects)
                    {
                        var gameObject = member.GameObject;
                        if (gameObject == null)
                        {
                            ImGui.Text("GameObject was null");
                        }
                        else
                        {
                            this.PrintGameObject(gameObject, "-");
                        }
                    }
                }
            }

            {
                var member = buddyList.PetBuddy;
                if (member == null)
                {
                    ImGui.Text("[Pet] null");
                }
                else
                {
                    ImGui.Text($"[Pet] {member.Address.ToInt64():X} - {member.ObjectId} - {member.DataID}");
                    if (this.resolveObjects)
                    {
                        var gameObject = member.GameObject;
                        if (gameObject == null)
                        {
                            ImGui.Text("GameObject was null");
                        }
                        else
                        {
                            this.PrintGameObject(gameObject, "-");
                        }
                    }
                }
            }

            {
                var count = buddyList.Length;
                if (count == 0)
                {
                    ImGui.Text("[BattleBuddy] None present");
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        var member = buddyList[i];
                        ImGui.Text($"[BattleBuddy] [{i}] {member.Address.ToInt64():X} - {member.ObjectId} - {member.DataID}");
                        if (this.resolveObjects)
                        {
                            var gameObject = member.GameObject;
                            if (gameObject == null)
                            {
                                ImGui.Text("GameObject was null");
                            }
                            else
                            {
                                this.PrintGameObject(gameObject, "-");
                            }
                        }
                    }
                }
            }
        }

        private void DrawPluginIPC()
        {
            if (this.ipcPub == null)
            {
                this.ipcPub = new CallGatePubSub<string, string>("dataDemo1");

                this.ipcPub.RegisterAction((msg) =>
                {
                    Log.Information($"Data action was called: {msg}");
                });

                this.ipcPub.RegisterFunc((msg) =>
                {
                    Log.Information($"Data func was called: {msg}");
                    return Guid.NewGuid().ToString();
                });
            }

            if (this.ipcSub == null)
            {
                this.ipcSub = new CallGatePubSub<string, string>("dataDemo1");
                this.ipcSub.Subscribe((msg) =>
                {
                    Log.Information("PONG1");
                });
                this.ipcSub.Subscribe((msg) =>
                {
                    Log.Information("PONG2");
                });
                this.ipcSub.Subscribe((msg) =>
                {
                    throw new Exception("PONG3");
                });
            }

            if (ImGui.Button("PING"))
            {
                this.ipcPub.SendMessage("PING");
            }

            if (ImGui.Button("Action"))
            {
                this.ipcSub.InvokeAction("button1");
            }

            if (ImGui.Button("Func"))
            {
                this.callGateResponse = this.ipcSub.InvokeFunc("button2");
            }

            if (!this.callGateResponse.IsNullOrEmpty())
                ImGui.Text($"Response: {this.callGateResponse}");
        }

        private void DrawCondition()
        {
            var condition = Service<Condition>.Get();

#if DEBUG
            ImGui.Text($"ptr: 0x{condition.Address.ToInt64():X}");
#endif

            ImGui.Text("Current Conditions:");
            ImGui.Separator();

            var didAny = false;

            for (var i = 0; i < Condition.MaxConditionEntries; i++)
            {
                var typedCondition = (ConditionFlag)i;
                var cond = condition[typedCondition];

                if (!cond) continue;

                didAny = true;

                ImGui.Text($"ID: {i} Enum: {typedCondition}");
            }

            if (!didAny)
                ImGui.Text("None. Talk to a shop NPC or visit a market board to find out more!!!!!!!");
        }

        private void DrawGauge()
        {
            var clientState = Service<ClientState>.Get();
            var jobGauges = Service<JobGauges>.Get();

            var player = clientState.LocalPlayer;
            if (player == null)
            {
                ImGui.Text("Player is not present");
                return;
            }

            var jobID = player.ClassJob.Id;
            if (jobID == 19)
            {
                var gauge = jobGauges.Get<PLDGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.OathGauge)}: {gauge.OathGauge}");
            }
            else if (jobID == 20)
            {
                var gauge = jobGauges.Get<MNKGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.Chakra)}: {gauge.Chakra}");
            }
            else if (jobID == 21)
            {
                var gauge = jobGauges.Get<WARGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.BeastGauge)}: {gauge.BeastGauge}");
            }
            else if (jobID == 22)
            {
                var gauge = jobGauges.Get<DRGGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.BOTDTimer)}: {gauge.BOTDTimer}");
                ImGui.Text($"{nameof(gauge.BOTDState)}: {gauge.BOTDState}");
                ImGui.Text($"{nameof(gauge.EyeCount)}: {gauge.EyeCount}");
            }
            else if (jobID == 23)
            {
                var gauge = jobGauges.Get<BRDGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.SongTimer)}: {gauge.SongTimer}");
                ImGui.Text($"{nameof(gauge.Repertoire)}: {gauge.Repertoire}");
                ImGui.Text($"{nameof(gauge.SoulVoice)}: {gauge.SoulVoice}");
                ImGui.Text($"{nameof(gauge.Song)}: {gauge.Song}");
            }
            else if (jobID == 24)
            {
                var gauge = jobGauges.Get<WHMGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.LilyTimer)}: {gauge.LilyTimer}");
                ImGui.Text($"{nameof(gauge.Lily)}: {gauge.Lily}");
                ImGui.Text($"{nameof(gauge.BloodLily)}: {gauge.BloodLily}");
            }
            else if (jobID == 25)
            {
                var gauge = jobGauges.Get<BLMGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.EnochianTimer)}: {gauge.EnochianTimer}");
                ImGui.Text($"{nameof(gauge.ElementTimeRemaining)}: {gauge.ElementTimeRemaining}");
                ImGui.Text($"{nameof(gauge.PolyglotStacks)}: {gauge.PolyglotStacks}");
                ImGui.Text($"{nameof(gauge.UmbralHearts)}: {gauge.UmbralHearts}");
                ImGui.Text($"{nameof(gauge.UmbralIceStacks)}: {gauge.UmbralIceStacks}");
                ImGui.Text($"{nameof(gauge.AstralFireStacks)}: {gauge.AstralFireStacks}");
                ImGui.Text($"{nameof(gauge.InUmbralIce)}: {gauge.InUmbralIce}");
                ImGui.Text($"{nameof(gauge.InAstralFire)}: {gauge.InAstralFire}");
                ImGui.Text($"{nameof(gauge.IsEnochianActive)}: {gauge.IsEnochianActive}");
            }
            else if (jobID == 27)
            {
                var gauge = jobGauges.Get<SMNGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.TimerRemaining)}: {gauge.TimerRemaining}");
                ImGui.Text($"{nameof(gauge.ReturnSummon)}: {gauge.ReturnSummon}");
                ImGui.Text($"{nameof(gauge.ReturnSummonGlam)}: {gauge.ReturnSummonGlam}");
                ImGui.Text($"{nameof(gauge.AetherFlags)}: {gauge.AetherFlags}");
                ImGui.Text($"{nameof(gauge.IsPhoenixReady)}: {gauge.IsPhoenixReady}");
                ImGui.Text($"{nameof(gauge.IsBahamutReady)}: {gauge.IsBahamutReady}");
                ImGui.Text($"{nameof(gauge.HasAetherflowStacks)}: {gauge.HasAetherflowStacks}");
            }
            else if (jobID == 28)
            {
                var gauge = jobGauges.Get<SCHGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.Aetherflow)}: {gauge.Aetherflow}");
                ImGui.Text($"{nameof(gauge.FairyGauge)}: {gauge.FairyGauge}");
                ImGui.Text($"{nameof(gauge.SeraphTimer)}: {gauge.SeraphTimer}");
                ImGui.Text($"{nameof(gauge.DismissedFairy)}: {gauge.DismissedFairy}");
            }
            else if (jobID == 30)
            {
                var gauge = jobGauges.Get<NINGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.HutonTimer)}: {gauge.HutonTimer}");
                ImGui.Text($"{nameof(gauge.Ninki)}: {gauge.Ninki}");
                ImGui.Text($"{nameof(gauge.HutonManualCasts)}: {gauge.HutonManualCasts}");
            }
            else if (jobID == 31)
            {
                var gauge = jobGauges.Get<MCHGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.OverheatTimeRemaining)}: {gauge.OverheatTimeRemaining}");
                ImGui.Text($"{nameof(gauge.SummonTimeRemaining)}: {gauge.SummonTimeRemaining}");
                ImGui.Text($"{nameof(gauge.Heat)}: {gauge.Heat}");
                ImGui.Text($"{nameof(gauge.Battery)}: {gauge.Battery}");
                ImGui.Text($"{nameof(gauge.LastSummonBatteryPower)}: {gauge.LastSummonBatteryPower}");
                ImGui.Text($"{nameof(gauge.IsOverheated)}: {gauge.IsOverheated}");
                ImGui.Text($"{nameof(gauge.IsRobotActive)}: {gauge.IsRobotActive}");
            }
            else if (jobID == 32)
            {
                var gauge = jobGauges.Get<DRKGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.Blood)}: {gauge.Blood}");
                ImGui.Text($"{nameof(gauge.DarksideTimeRemaining)}: {gauge.DarksideTimeRemaining}");
                ImGui.Text($"{nameof(gauge.ShadowTimeRemaining)}: {gauge.ShadowTimeRemaining}");
                ImGui.Text($"{nameof(gauge.HasDarkArts)}: {gauge.HasDarkArts}");
            }
            else if (jobID == 33)
            {
                var gauge = jobGauges.Get<ASTGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.DrawnCard)}: {gauge.DrawnCard}");
                foreach (var seal in Enum.GetValues(typeof(SealType)).Cast<SealType>())
                {
                    var sealName = Enum.GetName(typeof(SealType), seal);
                    ImGui.Text($"{nameof(gauge.ContainsSeal)}({sealName}): {gauge.ContainsSeal(seal)}");
                }
            }
            else if (jobID == 34)
            {
                var gauge = jobGauges.Get<SAMGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.Kenki)}: {gauge.Kenki}");
                ImGui.Text($"{nameof(gauge.MeditationStacks)}: {gauge.MeditationStacks}");
                ImGui.Text($"{nameof(gauge.Sen)}: {gauge.Sen}");
                ImGui.Text($"{nameof(gauge.HasSetsu)}: {gauge.HasSetsu}");
                ImGui.Text($"{nameof(gauge.HasGetsu)}: {gauge.HasGetsu}");
                ImGui.Text($"{nameof(gauge.HasKa)}: {gauge.HasKa}");
            }
            else if (jobID == 35)
            {
                var gauge = jobGauges.Get<RDMGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.WhiteMana)}: {gauge.WhiteMana}");
                ImGui.Text($"{nameof(gauge.BlackMana)}: {gauge.BlackMana}");
            }
            else if (jobID == 37)
            {
                var gauge = jobGauges.Get<GNBGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.Ammo)}: {gauge.Ammo}");
                ImGui.Text($"{nameof(gauge.MaxTimerDuration)}: {gauge.MaxTimerDuration}");
                ImGui.Text($"{nameof(gauge.AmmoComboStep)}: {gauge.AmmoComboStep}");
            }
            else if (jobID == 38)
            {
                var gauge = jobGauges.Get<DNCGauge>();
                ImGui.Text($"Address: 0x{gauge.Address.ToInt64():X}");
                ImGui.Text($"{nameof(gauge.Feathers)}: {gauge.Feathers}");
                ImGui.Text($"{nameof(gauge.Esprit)}: {gauge.Esprit}");
                ImGui.Text($"{nameof(gauge.CompletedSteps)}: {gauge.CompletedSteps}");
                ImGui.Text($"{nameof(gauge.NextStep)}: {gauge.NextStep}");
                ImGui.Text($"{nameof(gauge.IsDancing)}: {gauge.IsDancing}");
            }
            else
            {
                ImGui.Text("No supported gauge exists for this job.");
            }
        }

        private void DrawCommand()
        {
            var commandManager = Service<CommandManager>.Get();

            foreach (var command in commandManager.Commands)
            {
                ImGui.Text($"{command.Key}\n    -> {command.Value.HelpMessage}\n    -> In help: {command.Value.ShowInHelp}\n\n");
            }
        }

        private unsafe void DrawAddon()
        {
            var gameGui = Service<GameGui>.Get();

            ImGui.InputText("Addon name", ref this.inputAddonName, 256);
            ImGui.InputInt("Addon Index", ref this.inputAddonIndex);

            if (this.inputAddonName.IsNullOrEmpty())
                return;

            var address = gameGui.GetAddonByName(this.inputAddonName, this.inputAddonIndex);

            if (address == IntPtr.Zero)
            {
                ImGui.Text("Null");
                return;
            }

            var addon = (FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase*)address;
            var name = MemoryHelper.ReadStringNullTerminated((IntPtr)addon->Name);
            ImGui.TextUnformatted($"{name} - 0x{address.ToInt64():x}\n    v:{addon->IsVisible} x:{addon->X} y:{addon->Y} s:{addon->Scale}, w:{addon->RootNode->Width}, h:{addon->RootNode->Height}");

            if (ImGui.Button("Find Agent"))
            {
                this.findAgentInterfacePtr = gameGui.FindAgentInterface(address);
            }

            if (this.findAgentInterfacePtr != IntPtr.Zero)
            {
                ImGui.TextUnformatted($"Agent: 0x{this.findAgentInterfacePtr.ToInt64():x}");
                ImGui.SameLine();

                if (ImGui.Button("C"))
                    ImGui.SetClipboardText(this.findAgentInterfacePtr.ToInt64().ToString("x"));
            }
        }

        private void DrawAddonInspector()
        {
            this.addonInspector ??= new UIDebug();
            this.addonInspector.Draw();
        }

        private void DrawStartInfo()
        {
            var startInfo = Service<DalamudStartInfo>.Get();

            ImGui.Text(JsonConvert.SerializeObject(startInfo, Formatting.Indented));
        }

        private void DrawTarget()
        {
            var clientState = Service<ClientState>.Get();
            var targetMgr = Service<TargetManager>.Get();

            if (targetMgr.Target != null)
            {
                this.PrintGameObject(targetMgr.Target, "CurrentTarget");

                ImGui.Text("Target");
                Util.ShowObject(targetMgr.Target);

                var tot = targetMgr.Target.TargetObject;
                if (tot != null)
                {
                    ImGuiHelpers.ScaledDummy(10);

                    ImGui.Text("ToT");
                    Util.ShowObject(tot);
                }

                ImGuiHelpers.ScaledDummy(10);
            }

            if (targetMgr.FocusTarget != null)
                this.PrintGameObject(targetMgr.FocusTarget, "FocusTarget");

            if (targetMgr.MouseOverTarget != null)
                this.PrintGameObject(targetMgr.MouseOverTarget, "MouseOverTarget");

            if (targetMgr.PreviousTarget != null)
                this.PrintGameObject(targetMgr.PreviousTarget, "PreviousTarget");

            if (targetMgr.SoftTarget != null)
                this.PrintGameObject(targetMgr.SoftTarget, "SoftTarget");

            if (ImGui.Button("Clear CT"))
                targetMgr.ClearTarget();

            if (ImGui.Button("Clear FT"))
                targetMgr.ClearFocusTarget();

            var localPlayer = clientState.LocalPlayer;

            if (localPlayer != null)
            {
                if (ImGui.Button("Set CT"))
                    targetMgr.SetTarget(localPlayer);

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
            var toastGui = Service<ToastGui>.Get();

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
                toastGui.ShowNormal(this.inputTextToast, new ToastOptions
                {
                    Position = (ToastPosition)this.toastPosition,
                    Speed = (ToastSpeed)this.toastSpeed,
                });
            }

            if (ImGui.Button("Show Quest toast"))
            {
                toastGui.ShowQuest(this.inputTextToast, new QuestToastOptions
                {
                    Position = (QuestToastPosition)this.questToastPosition,
                    DisplayCheckmark = this.questToastCheckmark,
                    IconId = (uint)this.questToastIconId,
                    PlaySound = this.questToastSound,
                });
            }

            if (ImGui.Button("Show Error toast"))
            {
                toastGui.ShowError(this.inputTextToast);
            }
        }

        private void DrawFlyText()
        {
            if (ImGui.BeginCombo("Kind", this.flyKind.ToString()))
            {
                var names = Enum.GetNames(typeof(FlyTextKind));
                for (var i = 0; i < names.Length; i++)
                {
                    if (ImGui.Selectable($"{names[i]} ({i})"))
                        this.flyKind = (FlyTextKind)i;
                }

                ImGui.EndCombo();
            }

            ImGui.InputText("Text1", ref this.flyText1, 200);
            ImGui.InputText("Text2", ref this.flyText2, 200);

            ImGui.InputInt("Val1", ref this.flyVal1);
            ImGui.InputInt("Val2", ref this.flyVal2);

            ImGui.InputInt("Icon ID", ref this.flyIcon);
            ImGui.ColorEdit4("Color", ref this.flyColor);
            ImGui.InputInt("Actor Index", ref this.flyActor);
            var sendColor = ImGui.ColorConvertFloat4ToU32(this.flyColor);

            if (ImGui.Button("Send"))
            {
                Service<FlyTextGui>.Get().AddFlyText(
                    this.flyKind,
                    unchecked((uint)this.flyActor),
                    unchecked((uint)this.flyVal1),
                    unchecked((uint)this.flyVal2),
                    this.flyText1,
                    this.flyText2,
                    sendColor,
                    unchecked((uint)this.flyIcon));
            }
        }

        private void DrawImGui()
        {
            var interfaceManager = Service<InterfaceManager>.Get();
            var notifications = Service<NotificationManager>.Get();

            ImGui.Text("Monitor count: " + ImGui.GetPlatformIO().Monitors.Size);
            ImGui.Text("OverrideGameCursor: " + interfaceManager.OverrideGameCursor);

            ImGui.Button("THIS IS A BUTTON###hoverTestButton");
            interfaceManager.OverrideGameCursor = !ImGui.IsItemHovered();

            ImGui.Separator();

            ImGui.TextUnformatted($"WindowSystem.TimeSinceLastAnyFocus: {WindowSystem.TimeSinceLastAnyFocus.TotalMilliseconds:0}ms");

            ImGui.Separator();

            if (ImGui.Button("Add random notification"))
            {
                var rand = new Random();

                var title = rand.Next(0, 5) switch
                {
                    0 => "This is a toast",
                    1 => "Truly, a toast",
                    2 => "I am testing this toast",
                    3 => "I hope this looks right",
                    4 => "Good stuff",
                    5 => "Nice",
                    _ => null,
                };

                var type = rand.Next(0, 4) switch
                {
                    0 => Notifications.NotificationType.Error,
                    1 => Notifications.NotificationType.Warning,
                    2 => Notifications.NotificationType.Info,
                    3 => Notifications.NotificationType.Success,
                    4 => Notifications.NotificationType.None,
                    _ => Notifications.NotificationType.None,
                };

                var text = "Bla bla bla bla bla bla bla bla bla bla bla.\nBla bla bla bla bla bla bla bla bla bla bla bla bla bla.";

                notifications.AddNotification(text, title, type);
            }
        }

        private void DrawTex()
        {
            var dataManager = Service<DataManager>.Get();

            ImGui.InputText("Tex Path", ref this.inputTexPath, 255);
            ImGui.InputFloat2("UV0", ref this.inputTexUv0);
            ImGui.InputFloat2("UV1", ref this.inputTexUv1);
            ImGui.InputFloat4("Tint", ref this.inputTintCol);
            ImGui.InputFloat2("Scale", ref this.inputTexScale);

            if (ImGui.Button("Load Tex"))
            {
                try
                {
                    this.debugTex = dataManager.GetImGuiTexture(this.inputTexPath);
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

        private void DrawKeyState()
        {
            var keyState = Service<KeyState>.Get();

            ImGui.Columns(4);

            var i = 0;
            foreach (var vkCode in keyState.GetValidVirtualKeys())
            {
                var code = (int)vkCode;
                var value = keyState[code];

                ImGui.PushStyleColor(ImGuiCol.Text, value ? ImGuiColors.HealerGreen : ImGuiColors.DPSRed);

                ImGui.Text($"{vkCode} ({code})");

                ImGui.PopStyleColor();

                i++;
                if (i % 24 == 0)
                    ImGui.NextColumn();
            }

            ImGui.Columns(1);
        }

        private void DrawGamepad()
        {
            var gamepadState = Service<GamepadState>.Get();

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

            ImGui.Text($"GamepadInput 0x{gamepadState.GamepadInputAddress.ToInt64():X}");

#if DEBUG
            if (ImGui.IsItemHovered())
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            if (ImGui.IsItemClicked())
                ImGui.SetClipboardText($"0x{gamepadState.GamepadInputAddress.ToInt64():X}");
#endif

            DrawHelper(
                "Buttons Raw",
                gamepadState.ButtonsRaw,
                gamepadState.Raw);
            DrawHelper(
                "Buttons Pressed",
                gamepadState.ButtonsPressed,
                gamepadState.Pressed);
            DrawHelper(
                "Buttons Repeat",
                gamepadState.ButtonsRepeat,
                gamepadState.Repeat);
            DrawHelper(
                "Buttons Released",
                gamepadState.ButtonsReleased,
                gamepadState.Released);
            ImGui.Text($"LeftStickLeft {gamepadState.LeftStickLeft:0.00} " +
                       $"LeftStickUp {gamepadState.LeftStickUp:0.00} " +
                       $"LeftStickRight {gamepadState.LeftStickRight:0.00} " +
                       $"LeftStickDown {gamepadState.LeftStickDown:0.00} ");
            ImGui.Text($"RightStickLeft {gamepadState.RightStickLeft:0.00} " +
                       $"RightStickUp {gamepadState.RightStickUp:0.00} " +
                       $"RightStickRight {gamepadState.RightStickRight:0.00} " +
                       $"RightStickDown {gamepadState.RightStickDown:0.00} ");
        }

        private void Load()
        {
            var dataManager = Service<DataManager>.Get();

            if (dataManager.IsDataReady)
            {
                this.serverOpString = JsonConvert.SerializeObject(dataManager.ServerOpCodes, Formatting.Indented);
                this.wasReady = true;
            }
        }

        private void PrintGameObject(GameObject actor, string tag)
        {
            var actorString =
                $"{actor.Address.ToInt64():X}:{actor.ObjectId:X}[{tag}] - {actor.ObjectKind} - {actor.Name} - X{actor.Position.X} Y{actor.Position.Y} Z{actor.Position.Z} D{actor.YalmDistanceX} R{actor.Rotation} - Target: {actor.TargetObjectId:X}\n";

            if (actor is Npc npc)
                actorString += $"       DataId: {npc.DataId}  NameId:{npc.NameId}\n";

            if (actor is Character chara)
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
