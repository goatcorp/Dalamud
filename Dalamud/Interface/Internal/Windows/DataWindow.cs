using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Aetherytes;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Game.ClientState.JobGauge;
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
using Dalamud.Hooking;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Memory;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Internal;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using Newtonsoft.Json;
using PInvoke;
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

        private Hook<MessageBoxWDelegate>? messageBoxMinHook;
        private bool hookUseMinHook = false;

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

        private delegate int MessageBoxWDelegate(
            IntPtr hWnd,
            [MarshalAs(UnmanagedType.LPWStr)] string text,
            [MarshalAs(UnmanagedType.LPWStr)] string caption,
            NativeFunctions.MessageBoxType type);

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
            AtkArrayData_Browser,
            StartInfo,
            Target,
            Toast,
            FlyText,
            ImGui,
            Tex,
            KeyState,
            Gamepad,
            Configuration,
            TaskSched,
            Hook,
            Aetherytes,
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

                        case DataKind.AtkArrayData_Browser:
                            this.DrawAtkArrayDataBrowser();
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

                        case DataKind.Configuration:
                            this.DrawConfiguration();
                            break;

                        case DataKind.TaskSched:
                            this.DrawTaskSched();
                            break;

                        case DataKind.Hook:
                            this.DrawHook();
                            break;
                        case DataKind.Aetherytes:
                            this.DrawAetherytes();
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

        private int MessageBoxWDetour(IntPtr hwnd, string text, string caption, NativeFunctions.MessageBoxType type)
        {
            Log.Information("[DATAHOOK] {0} {1} {2} {3}", hwnd, text, caption, type);

            var result = this.messageBoxMinHook.Original(hwnd, "Cause Access Violation?", caption, NativeFunctions.MessageBoxType.YesNo);

            if (result == (int)User32.MessageBoxResult.IDYES)
            {
                Marshal.ReadByte(IntPtr.Zero);
            }

            return result;
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
            JobGaugeBase? gauge = jobID switch
            {
                19 => jobGauges.Get<PLDGauge>(),
                20 => jobGauges.Get<MNKGauge>(),
                21 => jobGauges.Get<WARGauge>(),
                22 => jobGauges.Get<DRGGauge>(),
                23 => jobGauges.Get<BRDGauge>(),
                24 => jobGauges.Get<WHMGauge>(),
                25 => jobGauges.Get<BLMGauge>(),
                27 => jobGauges.Get<SMNGauge>(),
                28 => jobGauges.Get<SCHGauge>(),
                30 => jobGauges.Get<NINGauge>(),
                31 => jobGauges.Get<MCHGauge>(),
                32 => jobGauges.Get<DRKGauge>(),
                33 => jobGauges.Get<ASTGauge>(),
                34 => jobGauges.Get<SAMGauge>(),
                35 => jobGauges.Get<RDMGauge>(),
                37 => jobGauges.Get<GNBGauge>(),
                38 => jobGauges.Get<DNCGauge>(),
                39 => jobGauges.Get<RPRGauge>(),
                40 => jobGauges.Get<SGEGauge>(),
                _ => null,
            };

            if (gauge == null)
            {
                ImGui.Text("No supported gauge exists for this job.");
                return;
            }

            Util.ShowObject(gauge);
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

        private unsafe void DrawAtkArrayDataBrowser()
        {
            var fontWidth = ImGui.CalcTextSize("A").X;
            var fontHeight = ImGui.GetTextLineHeightWithSpacing();
            var uiModule = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule();

            if (uiModule == null)
            {
                ImGui.Text("UIModule unavailable.");
                return;
            }

            var atkArrayDataHolder = &uiModule->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;

            if (ImGui.BeginTabBar("AtkArrayDataBrowserTabBar"))
            {
                if (ImGui.BeginTabItem($"NumberArrayData [{atkArrayDataHolder->NumberArrayCount}]"))
                {
                    if (ImGui.BeginTable("NumberArrayDataTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
                    {
                        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, fontWidth * 10);
                        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, fontWidth * 10);
                        ImGui.TableSetupColumn("Pointer", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableHeadersRow();
                        for (var numberArrayIndex = 0; numberArrayIndex < atkArrayDataHolder->NumberArrayCount; numberArrayIndex++)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text($"{numberArrayIndex} [{numberArrayIndex * 8:X}]");
                            ImGui.TableNextColumn();
                            var numberArrayData = atkArrayDataHolder->NumberArrays[numberArrayIndex];
                            if (numberArrayData != null)
                            {
                                ImGui.Text($"{numberArrayData->AtkArrayData.Size}");
                                ImGui.TableNextColumn();
                                if (ImGui.TreeNodeEx($"{(long)numberArrayData:X}###{numberArrayIndex}", ImGuiTreeNodeFlags.SpanFullWidth))
                                {
                                    ImGui.NewLine();
                                    var tableHeight = Math.Min(40, numberArrayData->AtkArrayData.Size + 4);
                                    if (ImGui.BeginTable($"NumberArrayDataTable", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0.0F, fontHeight * tableHeight)))
                                    {
                                        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, fontWidth * 6);
                                        ImGui.TableSetupColumn("Hex", ImGuiTableColumnFlags.WidthFixed, fontWidth * 9);
                                        ImGui.TableSetupColumn("Integer", ImGuiTableColumnFlags.WidthFixed, fontWidth * 12);
                                        ImGui.TableSetupColumn("Float", ImGuiTableColumnFlags.WidthFixed, fontWidth * 20);
                                        ImGui.TableHeadersRow();
                                        for (var numberIndex = 0; numberIndex < numberArrayData->AtkArrayData.Size; numberIndex++)
                                        {
                                            ImGui.TableNextRow();
                                            ImGui.TableNextColumn();
                                            ImGui.Text($"{numberIndex}");
                                            ImGui.TableNextColumn();
                                            ImGui.Text($"{numberArrayData->IntArray[numberIndex]:X}");
                                            ImGui.TableNextColumn();
                                            ImGui.Text($"{numberArrayData->IntArray[numberIndex]}");
                                            ImGui.TableNextColumn();
                                            ImGui.Text($"{*(float*)&numberArrayData->IntArray[numberIndex]}");
                                        }

                                        ImGui.EndTable();
                                    }

                                    ImGui.TreePop();
                                }
                            }
                            else
                            {
                                ImGui.TextDisabled("--");
                                ImGui.TableNextColumn();
                                ImGui.TextDisabled("--");
                            }
                        }

                        ImGui.EndTable();
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem($"StringArrayData [{atkArrayDataHolder->StringArrayCount}]"))
                {
                    if (ImGui.BeginTable("StringArrayDataTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
                    {
                        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, fontWidth * 10);
                        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, fontWidth * 10);
                        ImGui.TableSetupColumn("Pointer", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableHeadersRow();
                        for (var stringArrayIndex = 0; stringArrayIndex < atkArrayDataHolder->StringArrayCount; stringArrayIndex++)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text($"{stringArrayIndex} [{stringArrayIndex * 8:X}]");
                            ImGui.TableNextColumn();
                            var stringArrayData = atkArrayDataHolder->StringArrays[stringArrayIndex];
                            if (stringArrayData != null)
                            {
                                ImGui.Text($"{stringArrayData->AtkArrayData.Size}");
                                ImGui.TableNextColumn();
                                if (ImGui.TreeNodeEx($"{(long)stringArrayData:X}###{stringArrayIndex}", ImGuiTreeNodeFlags.SpanFullWidth))
                                {
                                    ImGui.NewLine();
                                    var tableHeight = Math.Min(40, stringArrayData->AtkArrayData.Size + 4);
                                    if (ImGui.BeginTable($"StringArrayDataTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0.0F, fontHeight * tableHeight)))
                                    {
                                        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, fontWidth * 6);
                                        ImGui.TableSetupColumn("String", ImGuiTableColumnFlags.WidthStretch);
                                        ImGui.TableHeadersRow();
                                        for (var stringIndex = 0; stringIndex < stringArrayData->AtkArrayData.Size; stringIndex++)
                                        {
                                            ImGui.TableNextRow();
                                            ImGui.TableNextColumn();
                                            ImGui.Text($"{stringIndex}");
                                            ImGui.TableNextColumn();
                                            if (stringArrayData->StringArray[stringIndex] != null)
                                            {
                                                ImGui.Text($"{MemoryHelper.ReadSeStringNullTerminated(new IntPtr(stringArrayData->StringArray[stringIndex]))}");
                                            }
                                            else
                                            {
                                                ImGui.TextDisabled("--");
                                            }
                                        }

                                        ImGui.EndTable();
                                    }

                                    ImGui.TreePop();
                                }
                            }
                            else
                            {
                                ImGui.TextDisabled("--");
                                ImGui.TableNextColumn();
                                ImGui.TextDisabled("--");
                            }
                        }

                        ImGui.EndTable();
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
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
                Util.ShowGameObjectStruct(targetMgr.Target);

                var tot = targetMgr.Target.TargetObject;
                if (tot != null)
                {
                    ImGuiHelpers.ScaledDummy(10);

                    ImGui.Separator();
                    ImGui.Text("ToT");
                    Util.ShowGameObjectStruct(tot);
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

        private void DrawConfiguration()
        {
            var config = Service<DalamudConfiguration>.Get();
            Util.ShowObject(config);
        }

        private void DrawTaskSched()
        {
            if (ImGui.Button("Clear list"))
            {
                TaskTracker.Clear();
            }

            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(10);
            ImGui.SameLine();

            if (ImGui.Button("Short Task.Run"))
            {
                Task.Run(() => { Thread.Sleep(500); });
            }

            ImGui.SameLine();

            if (ImGui.Button("Task in task(Delay)"))
            {
                Task.Run(async () => await this.TestTaskInTaskDelay());
            }

            ImGui.SameLine();

            if (ImGui.Button("Task in task(Sleep)"))
            {
                Task.Run(async () => await this.TestTaskInTaskSleep());
            }

            ImGui.SameLine();

            if (ImGui.Button("Faulting task"))
            {
                Task.Run(() =>
                {
                    Thread.Sleep(200);

                    string a = null;
                    a.Contains("dalamud");
                });
            }

            if (ImGui.Button("Drown in tasks"))
            {
                Task.Run(() =>
                {
                    for (var i = 0; i < 100; i++)
                    {
                        Task.Run(() =>
                        {
                            for (var i = 0; i < 100; i++)
                            {
                                Task.Run(() =>
                                {
                                    for (var i = 0; i < 100; i++)
                                    {
                                        Task.Run(() =>
                                        {
                                            for (var i = 0; i < 100; i++)
                                            {
                                                Task.Run(() =>
                                                {
                                                    for (var i = 0; i < 100; i++)
                                                    {
                                                        Thread.Sleep(1);
                                                    }
                                                });
                                            }
                                        });
                                    }
                                });
                            }
                        });
                    }
                });
            }

            ImGui.SameLine();

            ImGuiHelpers.ScaledDummy(20);

            // Needed to init the task tracker, if we're not on a debug build
            var tracker = Service<TaskTracker>.GetNullable() ?? Service<TaskTracker>.Set();

            for (var i = 0; i < TaskTracker.Tasks.Count; i++)
            {
                var task = TaskTracker.Tasks[i];
                var subTime = DateTime.Now;
                if (task.Task == null)
                    subTime = task.FinishTime;

                switch (task.Status)
                {
                    case TaskStatus.Created:
                    case TaskStatus.WaitingForActivation:
                    case TaskStatus.WaitingToRun:
                        ImGui.PushStyleColor(ImGuiCol.Header, ImGuiColors.DalamudGrey);
                        break;
                    case TaskStatus.Running:
                    case TaskStatus.WaitingForChildrenToComplete:
                        ImGui.PushStyleColor(ImGuiCol.Header, ImGuiColors.ParsedBlue);
                        break;
                    case TaskStatus.RanToCompletion:
                        ImGui.PushStyleColor(ImGuiCol.Header, ImGuiColors.ParsedGreen);
                        break;
                    case TaskStatus.Canceled:
                    case TaskStatus.Faulted:
                        ImGui.PushStyleColor(ImGuiCol.Header, ImGuiColors.DalamudRed);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (ImGui.CollapsingHeader($"#{task.Id} - {task.Status} {(subTime - task.StartTime).TotalMilliseconds}ms###task{i}"))
                {
                    task.IsBeingViewed = true;

                    if (ImGui.Button("CANCEL (May not work)"))
                    {
                        try
                        {
                            var cancelFunc =
                                typeof(Task).GetMethod("InternalCancel", BindingFlags.NonPublic | BindingFlags.Instance);
                            cancelFunc.Invoke(task, null);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Could not cancel task.");
                        }
                    }

                    ImGuiHelpers.ScaledDummy(10);

                    ImGui.TextUnformatted(task.StackTrace.ToString());

                    if (task.Exception != null)
                    {
                        ImGuiHelpers.ScaledDummy(15);
                        ImGui.TextColored(ImGuiColors.DalamudRed, "EXCEPTION:");
                        ImGui.TextUnformatted(task.Exception.ToString());
                    }
                }
                else
                {
                    task.IsBeingViewed = false;
                }

                ImGui.PopStyleColor(1);
            }
        }

        private void DrawHook()
        {
            try
            {
                ImGui.Checkbox("Use MinHook", ref this.hookUseMinHook);

                if (ImGui.Button("Create"))
                    this.messageBoxMinHook = Hook<MessageBoxWDelegate>.FromSymbol("User32", "MessageBoxW", this.MessageBoxWDetour, this.hookUseMinHook);

                if (ImGui.Button("Enable"))
                    this.messageBoxMinHook?.Enable();

                if (ImGui.Button("Disable"))
                    this.messageBoxMinHook?.Disable();

                if (ImGui.Button("Call Original"))
                    this.messageBoxMinHook?.Original(IntPtr.Zero, "Hello from .Original", "Hook Test", NativeFunctions.MessageBoxType.Ok);

                if (ImGui.Button("Dispose"))
                {
                    this.messageBoxMinHook?.Dispose();
                    this.messageBoxMinHook = null;
                }

                if (ImGui.Button("Test"))
                    _ = NativeFunctions.MessageBoxW(IntPtr.Zero, "Hi", "Hello", NativeFunctions.MessageBoxType.Ok);

                if (this.messageBoxMinHook != null)
                    ImGui.Text("Enabled: " + this.messageBoxMinHook?.IsEnabled);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "MinHook error caught");
            }
        }

        private void DrawAetherytes()
        {
            if (!ImGui.BeginTable("##aetheryteTable", 11, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
                return;

            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("Idx", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Zone", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Ward", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Plot", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Sub", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Gil", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Fav", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Shared", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Appartment", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();

            var tpList = Service<AetheryteList>.Get();

            for (var i = 0; i < tpList.Length; i++)
            {
                var info = tpList[i];
                if (info == null)
                    continue;

                ImGui.TableNextColumn(); // Idx
                ImGui.TextUnformatted($"{i}");

                ImGui.TableNextColumn(); // Name
                ImGui.TextUnformatted($"{info.AetheryteData.GameData.PlaceName.Value?.Name}");

                ImGui.TableNextColumn(); // ID
                ImGui.TextUnformatted($"{info.AetheryteId}");

                ImGui.TableNextColumn(); // Zone
                ImGui.TextUnformatted($"{info.TerritoryId}");

                ImGui.TableNextColumn(); // Ward
                ImGui.TextUnformatted($"{info.Ward}");

                ImGui.TableNextColumn(); // Plot
                ImGui.TextUnformatted($"{info.Plot}");

                ImGui.TableNextColumn(); // Sub
                ImGui.TextUnformatted($"{info.SubIndex}");

                ImGui.TableNextColumn(); // Gil
                ImGui.TextUnformatted($"{info.GilCost}");

                ImGui.TableNextColumn(); // Favourite
                ImGui.TextUnformatted($"{info.IsFavourite}");

                ImGui.TableNextColumn(); // Shared
                ImGui.TextUnformatted($"{info.IsSharedHouse}");

                ImGui.TableNextColumn(); // Appartment
                ImGui.TextUnformatted($"{info.IsAppartment}");
            }

            ImGui.EndTable();
        }

        private async Task TestTaskInTaskDelay()
        {
            await Task.Delay(5000);
        }

#pragma warning disable 1998
        private async Task TestTaskInTaskSleep()
#pragma warning restore 1998
        {
            Thread.Sleep(5000);
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
