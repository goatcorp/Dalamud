using System.Runtime.InteropServices;

using Dalamud.Game.NativeWrapper;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Interface.Utility;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Component.GUI;

using ImGuiNET;

using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Dalamud.Game.Gui;

/// <summary>
/// A class handling many aspects of the in-game UI.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class GameGui : IInternalDisposableService, IGameGui
{
    private static readonly ModuleLog Log = new("GameGui");

    private readonly GameGuiAddressResolver address;

    private readonly Hook<AgentItemDetail.Delegates.Update> handleItemHoverHook;
    private readonly Hook<AgentItemDetail.Delegates.ReceiveEvent> handleItemOutHook;
    private readonly Hook<AgentActionDetail.Delegates.HandleActionHover> handleActionHoverHook;
    private readonly Hook<AgentActionDetail.Delegates.ReceiveEvent> handleActionOutHook;
    private readonly Hook<HandleImmDelegate> handleImmHook;
    private readonly Hook<RaptureAtkModule.Delegates.SetUiVisibility> setUiVisibilityHook;
    private readonly Hook<Utf8String.Delegates.Ctor_FromSequence> utf8StringFromSequenceHook;

    [ServiceManager.ServiceConstructor]
    private GameGui(TargetSigScanner sigScanner)
    {
        this.address = new GameGuiAddressResolver();
        this.address.Setup(sigScanner);

        Log.Verbose("===== G A M E G U I =====");
        Log.Verbose($"HandleImm address {Util.DescribeAddress(this.address.HandleImm)}");

        this.handleItemHoverHook = Hook<AgentItemDetail.Delegates.Update>.FromAddress((nint)AgentItemDetail.StaticVirtualTablePointer->Update, this.HandleItemHoverDetour);
        this.handleItemOutHook = Hook<AgentItemDetail.Delegates.ReceiveEvent>.FromAddress((nint)AgentItemDetail.StaticVirtualTablePointer->ReceiveEvent, this.HandleItemOutDetour);

        this.handleActionHoverHook = Hook<AgentActionDetail.Delegates.HandleActionHover>.FromAddress(AgentActionDetail.Addresses.HandleActionHover.Value, this.HandleActionHoverDetour);
        this.handleActionOutHook = Hook<AgentActionDetail.Delegates.ReceiveEvent>.FromAddress((nint)AgentActionDetail.StaticVirtualTablePointer->ReceiveEvent, this.HandleActionOutDetour);

        this.handleImmHook = Hook<HandleImmDelegate>.FromAddress(this.address.HandleImm, this.HandleImmDetour);

        this.setUiVisibilityHook = Hook<RaptureAtkModule.Delegates.SetUiVisibility>.FromAddress((nint)RaptureAtkModule.StaticVirtualTablePointer->SetUiVisibility, this.SetUiVisibilityDetour);

        this.utf8StringFromSequenceHook = Hook<Utf8String.Delegates.Ctor_FromSequence>.FromAddress(Utf8String.Addresses.Ctor_FromSequence.Value, this.Utf8StringFromSequenceDetour);

        this.handleItemHoverHook.Enable();
        this.handleItemOutHook.Enable();
        this.handleImmHook.Enable();
        this.setUiVisibilityHook.Enable();
        this.handleActionHoverHook.Enable();
        this.handleActionOutHook.Enable();
        this.utf8StringFromSequenceHook.Enable();
    }

    // Hooked delegates

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate char HandleImmDelegate(IntPtr framework, char a2, byte a3);

    /// <inheritdoc/>
    public event EventHandler<bool>? UiHideToggled;

    /// <inheritdoc/>
    public event EventHandler<ulong>? HoveredItemChanged;

    /// <inheritdoc/>
    public event EventHandler<HoveredAction>? HoveredActionChanged;

    /// <inheritdoc/>
    public bool GameUiHidden { get; private set; }

    /// <inheritdoc/>
    public ulong HoveredItem { get; set; }

    /// <inheritdoc/>
    public HoveredAction HoveredAction { get; } = new HoveredAction();

    /// <inheritdoc/>
    public bool OpenMapWithMapLink(MapLinkPayload mapLink)
        => RaptureAtkModule.Instance()->OpenMapWithMapLink(mapLink.DataString);

    /// <inheritdoc/>
    public bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos)
        => this.WorldToScreen(worldPos, out screenPos, out var inView) && inView;

    /// <inheritdoc/>
    public bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos, out bool inView)
    {
        // Read current ViewProjectionMatrix plus game window size
        var windowPos = ImGuiHelpers.MainViewport.Pos;
        var viewProjectionMatrix = Control.Instance()->ViewProjectionMatrix;
        var device = Device.Instance();
        float width = device->Width;
        float height = device->Height;

        var pCoords = Vector4.Transform(new Vector4(worldPos, 1.0f), viewProjectionMatrix);
        var inFront = pCoords.W > 0.0f;

        if (Math.Abs(pCoords.W) < float.Epsilon)
        {
            screenPos = Vector2.Zero;
            inView = false;
            return false;
        }

        pCoords *= MathF.Abs(1.0f / pCoords.W);
        screenPos = new Vector2(pCoords.X, pCoords.Y);

        screenPos.X = (0.5f * width * (screenPos.X + 1f)) + windowPos.X;
        screenPos.Y = (0.5f * height * (1f - screenPos.Y)) + windowPos.Y;

        inView = inFront &&
                 screenPos.X > windowPos.X && screenPos.X < windowPos.X + width &&
                 screenPos.Y > windowPos.Y && screenPos.Y < windowPos.Y + height;

        return inFront;
    }

    /// <inheritdoc/>
    public bool ScreenToWorld(Vector2 screenPos, out Vector3 worldPos, float rayDistance = 100000.0f)
    {
        // The game is only visible in the main viewport, so if the cursor is outside
        // of the game window, do not bother calculating anything
        var windowPos = ImGuiHelpers.MainViewport.Pos;
        var windowSize = ImGuiHelpers.MainViewport.Size;

        if (screenPos.X < windowPos.X || screenPos.X > windowPos.X + windowSize.X ||
            screenPos.Y < windowPos.Y || screenPos.Y > windowPos.Y + windowSize.Y)
        {
            worldPos = default;
            return false;
        }

        var camera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager.Instance()->CurrentCamera;
        if (camera == null)
        {
            worldPos = Vector3.Zero;
            return false;
        }

        var ray = camera->ScreenPointToRay(screenPos);
        var result = BGCollisionModule.RaycastMaterialFilter(ray.Origin, ray.Direction, out var hit);
        worldPos = hit.Point;
        return result;
    }

    /// <inheritdoc/>
    public UIModulePtr GetUIModule()
    {
        return (nint)UIModule.Instance();
    }

    /// <inheritdoc/>
    public AtkUnitBasePtr GetAddonByName(string name, int index = 1)
    {
        var unitManager = RaptureAtkUnitManager.Instance();
        if (unitManager == null)
            return 0;

        return (nint)unitManager->GetAddonByName(name, index);
    }

    /// <inheritdoc/>
    public AgentInterfacePtr GetAgentById(int id)
    {
        var agentModule = AgentModule.Instance();
        if (agentModule == null || id < 0 || id >= agentModule->Agents.Length)
            return 0;

        return (nint)agentModule->Agents[id].Value;
    }

    /// <inheritdoc/>
    public AgentInterfacePtr FindAgentInterface(string addonName)
    {
        var addon = this.GetAddonByName(addonName);
        return this.FindAgentInterface(addon);
    }

    /// <inheritdoc/>
    public AgentInterfacePtr FindAgentInterface(AtkUnitBasePtr addon)
    {
        if (addon.IsNull)
            return 0;

        var agentModule = AgentModule.Instance();
        if (agentModule == null)
            return 0;

        var addonId = addon.ParentId == 0 ? addon.Id : addon.ParentId;
        if (addonId == 0)
            return 0;

        foreach (AgentInterface* agent in agentModule->Agents)
        {
            if (agent != null && agent->AddonId == addonId)
                return (nint)agent;
        }

        return 0;
    }

    /// <summary>
    /// Disables the hooks and submodules of this module.
    /// </summary>
    void IInternalDisposableService.DisposeService()
    {
        this.handleItemHoverHook.Dispose();
        this.handleItemOutHook.Dispose();
        this.handleImmHook.Dispose();
        this.setUiVisibilityHook.Dispose();
        this.handleActionHoverHook.Dispose();
        this.handleActionOutHook.Dispose();
        this.utf8StringFromSequenceHook.Dispose();
    }

    /// <summary>
    /// Indicates if the game is in the lobby scene (title screen, chara select, chara make, aesthetician etc.).
    /// </summary>
    /// <returns>A value indicating whether the game is in the lobby scene.</returns>
    internal bool IsInLobby() => RaptureAtkModule.Instance()->CurrentUIScene.StartsWith("LobbyMain"u8);

    /// <summary>
    /// Sets the current background music.
    /// </summary>
    /// <param name="bgmId">The BGM row id.</param>
    /// <param name="sceneId">The BGM scene index. Defaults to MiniGame scene to avoid conflicts.</param>
    internal void SetBgm(ushort bgmId, uint sceneId = 2) => BGMSystem.SetBGM(bgmId, sceneId);

    /// <summary>
    /// Resets the current background music.
    /// </summary>
    /// <param name="sceneId">The BGM scene index.</param>
    internal void ResetBgm(uint sceneId = 2) => BGMSystem.Instance()->ResetBGM(sceneId);

    /// <summary>
    /// Reset the stored "UI hide" state.
    /// </summary>
    internal void ResetUiHideState()
    {
        this.GameUiHidden = false;
    }

    private void HandleItemHoverDetour(AgentItemDetail* thisPtr, uint frameCount)
    {
        this.handleItemHoverHook.Original(thisPtr, frameCount);

        if (!thisPtr->IsAgentActive())
            return;

        var itemId = (ulong)thisPtr->ItemId;
        if (this.HoveredItem == itemId)
            return;

        this.HoveredItem = itemId;
        this.HoveredItemChanged?.InvokeSafely(this, itemId);

        Log.Verbose($"HoveredItem changed: {itemId}");
    }

    private AtkValue* HandleItemOutDetour(AgentItemDetail* thisPtr, AtkValue* returnValue, AtkValue* values, uint valueCount, ulong eventKind)
    {
        var ret = this.handleItemOutHook.Original(thisPtr, returnValue, values, valueCount, eventKind);

        if (values != null && valueCount == 1 && values->Int == -1)
        {
            this.HoveredItem = 0;
            this.HoveredItemChanged?.InvokeSafely(this, 0ul);

            Log.Verbose("HoveredItem changed: 0");
        }

        return ret;
    }

    private void HandleActionHoverDetour(AgentActionDetail* hoverState, FFXIVClientStructs.FFXIV.Client.UI.Agent.ActionKind actionKind, uint actionId, int a4, byte a5)
    {
        this.handleActionHoverHook.Original(hoverState, actionKind, actionId, a4, a5);
        this.HoveredAction.ActionKind = (HoverActionKind)actionKind;
        this.HoveredAction.BaseActionID = actionId;
        this.HoveredAction.ActionID = hoverState->ActionId;
        this.HoveredActionChanged?.InvokeSafely(this, this.HoveredAction);

        Log.Verbose($"HoverActionId: {actionKind}/{actionId} this:{(nint)hoverState:X}");
    }

    private AtkValue* HandleActionOutDetour(AgentActionDetail* agentActionDetail, AtkValue* a2, AtkValue* a3, uint a4, ulong a5)
    {
        var retVal = this.handleActionOutHook.Original(agentActionDetail, a2, a3, a4, a5);

        if (a3 != null && a4 == 1)
        {
            var a3Val = a3->Int;

            if (a3Val == 255)
            {
                this.HoveredAction.ActionKind = HoverActionKind.None;
                this.HoveredAction.BaseActionID = 0;
                this.HoveredAction.ActionID = 0;
                this.HoveredActionChanged?.InvokeSafely(this, this.HoveredAction);

                Log.Verbose("HoverActionId: 0");
            }
        }

        return retVal;
    }

    private unsafe void SetUiVisibilityDetour(RaptureAtkModule* thisPtr, bool uiVisible)
    {
        this.setUiVisibilityHook.Original(thisPtr, uiVisible);

        this.GameUiHidden = !RaptureAtkModule.Instance()->IsUiVisible;
        this.UiHideToggled?.InvokeSafely(this, this.GameUiHidden);

        Log.Debug("GameUiHidden: {0}", this.GameUiHidden);
    }

    private char HandleImmDetour(IntPtr framework, char a2, byte a3)
    {
        var result = this.handleImmHook.Original(framework, a2, a3);
        if (!ImGuiHelpers.IsImGuiInitialized)
            return result;

        return ImGui.GetIO().WantTextInput
                   ? (char)0
                   : result;
    }

    private Utf8String* Utf8StringFromSequenceDetour(Utf8String* thisPtr, byte* sourcePtr, nuint sourceLen)
    {
        if (sourcePtr != null)
            this.utf8StringFromSequenceHook.Original(thisPtr, sourcePtr, sourceLen);
        else
            thisPtr->Ctor(); // this is in ClientStructs but you could do it manually too

        return thisPtr; // this function shouldn't need to return but the original asm moves this into rax before returning so be safe?
    }
}

/// <summary>
/// Plugin-scoped version of a AddonLifecycle service.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IGameGui>]
#pragma warning restore SA1015
internal class GameGuiPluginScoped : IInternalDisposableService, IGameGui
{
    [ServiceManager.ServiceDependency]
    private readonly GameGui gameGuiService = Service<GameGui>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="GameGuiPluginScoped"/> class.
    /// </summary>
    internal GameGuiPluginScoped()
    {
        this.gameGuiService.UiHideToggled += this.UiHideToggledForward;
        this.gameGuiService.HoveredItemChanged += this.HoveredItemForward;
        this.gameGuiService.HoveredActionChanged += this.HoveredActionForward;
    }

    /// <inheritdoc/>
    public event EventHandler<bool>? UiHideToggled;

    /// <inheritdoc/>
    public event EventHandler<ulong>? HoveredItemChanged;

    /// <inheritdoc/>
    public event EventHandler<HoveredAction>? HoveredActionChanged;

    /// <inheritdoc/>
    public bool GameUiHidden => this.gameGuiService.GameUiHidden;

    /// <inheritdoc/>
    public ulong HoveredItem
    {
        get => this.gameGuiService.HoveredItem;
        set => this.gameGuiService.HoveredItem = value;
    }

    /// <inheritdoc/>
    public HoveredAction HoveredAction => this.gameGuiService.HoveredAction;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.gameGuiService.UiHideToggled -= this.UiHideToggledForward;
        this.gameGuiService.HoveredItemChanged -= this.HoveredItemForward;
        this.gameGuiService.HoveredActionChanged -= this.HoveredActionForward;

        this.UiHideToggled = null;
        this.HoveredItemChanged = null;
        this.HoveredActionChanged = null;
    }

    /// <inheritdoc/>
    public bool OpenMapWithMapLink(MapLinkPayload mapLink)
        => this.gameGuiService.OpenMapWithMapLink(mapLink);

    /// <inheritdoc/>
    public bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos)
        => this.gameGuiService.WorldToScreen(worldPos, out screenPos);

    /// <inheritdoc/>
    public bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos, out bool inView)
        => this.gameGuiService.WorldToScreen(worldPos, out screenPos, out inView);

    /// <inheritdoc/>
    public bool ScreenToWorld(Vector2 screenPos, out Vector3 worldPos, float rayDistance = 100000)
        => this.gameGuiService.ScreenToWorld(screenPos, out worldPos, rayDistance);

    /// <inheritdoc/>
    public UIModulePtr GetUIModule()
        => this.gameGuiService.GetUIModule();

    /// <inheritdoc/>
    public AtkUnitBasePtr GetAddonByName(string name, int index = 1)
        => this.gameGuiService.GetAddonByName(name, index);

    /// <inheritdoc/>
    public AgentInterfacePtr GetAgentById(int id)
        => this.gameGuiService.GetAgentById(id);

    /// <inheritdoc/>
    public AgentInterfacePtr FindAgentInterface(string addonName)
        => this.gameGuiService.FindAgentInterface(addonName);

    /// <inheritdoc/>
    public AgentInterfacePtr FindAgentInterface(AtkUnitBasePtr addon)
        => this.gameGuiService.FindAgentInterface(addon);

    private void UiHideToggledForward(object sender, bool toggled) => this.UiHideToggled?.Invoke(sender, toggled);

    private void HoveredItemForward(object sender, ulong itemId) => this.HoveredItemChanged?.Invoke(sender, itemId);

    private void HoveredActionForward(object sender, HoveredAction hoverAction) => this.HoveredActionChanged?.Invoke(sender, hoverAction);
}
