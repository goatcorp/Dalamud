using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Interface.Utility;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace Dalamud.Game.Gui;

/// <summary>
/// A class handling many aspects of the in-game UI.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
internal sealed unsafe class GameGui : IInternalDisposableService, IGameGui
{
    private static readonly ModuleLog Log = new("GameGui");
    
    private readonly GameGuiAddressResolver address;

    private readonly GetMatrixSingletonDelegate getMatrixSingleton;

    private readonly Hook<SetGlobalBgmDelegate> setGlobalBgmHook;
    private readonly Hook<HandleItemHoverDelegate> handleItemHoverHook;
    private readonly Hook<HandleItemOutDelegate> handleItemOutHook;
    private readonly Hook<HandleActionHoverDelegate> handleActionHoverHook;
    private readonly Hook<HandleActionOutDelegate> handleActionOutHook;
    private readonly Hook<HandleImmDelegate> handleImmHook;
    private readonly Hook<ToggleUiHideDelegate> toggleUiHideHook;
    private readonly Hook<Utf8StringFromSequenceDelegate> utf8StringFromSequenceHook;

    private GetUIMapObjectDelegate? getUIMapObject;
    private OpenMapWithFlagDelegate? openMapWithFlag;

    [ServiceManager.ServiceConstructor]
    private GameGui(TargetSigScanner sigScanner)
    {
        this.address = new GameGuiAddressResolver();
        this.address.Setup(sigScanner);

        Log.Verbose("===== G A M E G U I =====");
        Log.Verbose($"GameGuiManager address 0x{this.address.BaseAddress.ToInt64():X}");
        Log.Verbose($"SetGlobalBgm address 0x{this.address.SetGlobalBgm.ToInt64():X}");
        Log.Verbose($"HandleItemHover address 0x{this.address.HandleItemHover.ToInt64():X}");
        Log.Verbose($"HandleItemOut address 0x{this.address.HandleItemOut.ToInt64():X}");
        Log.Verbose($"HandleImm address 0x{this.address.HandleImm.ToInt64():X}");

        this.setGlobalBgmHook = Hook<SetGlobalBgmDelegate>.FromAddress(this.address.SetGlobalBgm, this.HandleSetGlobalBgmDetour);

        this.handleItemHoverHook = Hook<HandleItemHoverDelegate>.FromAddress(this.address.HandleItemHover, this.HandleItemHoverDetour);
        this.handleItemOutHook = Hook<HandleItemOutDelegate>.FromAddress(this.address.HandleItemOut, this.HandleItemOutDetour);

        this.handleActionHoverHook = Hook<HandleActionHoverDelegate>.FromAddress(this.address.HandleActionHover, this.HandleActionHoverDetour);
        this.handleActionOutHook = Hook<HandleActionOutDelegate>.FromAddress(this.address.HandleActionOut, this.HandleActionOutDetour);

        this.handleImmHook = Hook<HandleImmDelegate>.FromAddress(this.address.HandleImm, this.HandleImmDetour);

        this.getMatrixSingleton = Marshal.GetDelegateForFunctionPointer<GetMatrixSingletonDelegate>(this.address.GetMatrixSingleton);
        
        this.toggleUiHideHook = Hook<ToggleUiHideDelegate>.FromAddress(this.address.ToggleUiHide, this.ToggleUiHideDetour);

        this.utf8StringFromSequenceHook = Hook<Utf8StringFromSequenceDelegate>.FromAddress(this.address.Utf8StringFromSequence, this.Utf8StringFromSequenceDetour);

        this.setGlobalBgmHook.Enable();
        this.handleItemHoverHook.Enable();
        this.handleItemOutHook.Enable();
        this.handleImmHook.Enable();
        this.toggleUiHideHook.Enable();
        this.handleActionHoverHook.Enable();
        this.handleActionOutHook.Enable();
        this.utf8StringFromSequenceHook.Enable();
    }

    // Marshaled delegates

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetMatrixSingletonDelegate();

    // Hooked delegates

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate Utf8String* Utf8StringFromSequenceDelegate(Utf8String* thisPtr, byte* sourcePtr, nuint sourceLen);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr GetUIMapObjectDelegate(IntPtr uiObject);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    private delegate bool OpenMapWithFlagDelegate(IntPtr uiMapObject, string flag);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr SetGlobalBgmDelegate(ushort bgmKey, byte a2, uint a3, uint a4, uint a5, byte a6);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr HandleItemHoverDelegate(IntPtr hoverState, IntPtr a2, IntPtr a3, ulong a4);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr HandleItemOutDelegate(IntPtr hoverState, IntPtr a2, IntPtr a3, ulong a4);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void HandleActionHoverDelegate(IntPtr hoverState, HoverActionKind a2, uint a3, int a4, byte a5);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr HandleActionOutDelegate(IntPtr agentActionDetail, IntPtr a2, IntPtr a3, int a4);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate char HandleImmDelegate(IntPtr framework, char a2, byte a3);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr ToggleUiHideDelegate(IntPtr thisPtr, bool uiVisible);
    
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
    {
        var uiModule = this.GetUIModule();

        if (uiModule == IntPtr.Zero)
        {
            Log.Error("OpenMapWithMapLink: Null pointer returned from getUIObject()");
            return false;
        }

        this.getUIMapObject ??= this.address.GetVirtualFunction<GetUIMapObjectDelegate>(uiModule, 0, 8);

        var uiMapObjectPtr = this.getUIMapObject(uiModule);

        if (uiMapObjectPtr == IntPtr.Zero)
        {
            Log.Error("OpenMapWithMapLink: Null pointer returned from GetUIMapObject()");
            return false;
        }

        this.openMapWithFlag ??= this.address.GetVirtualFunction<OpenMapWithFlagDelegate>(uiMapObjectPtr, 0, 63);

        var mapLinkString = mapLink.DataString;

        Log.Debug($"OpenMapWithMapLink: Opening Map Link: {mapLinkString}");

        return this.openMapWithFlag(uiMapObjectPtr, mapLinkString);
    }

    /// <inheritdoc/>
    public bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos)
        => this.WorldToScreen(worldPos, out screenPos, out var inView) && inView;

    /// <inheritdoc/>
    public bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos, out bool inView)
    {
        // Get base object with matrices
        var matrixSingleton = this.getMatrixSingleton();

        // Read current ViewProjectionMatrix plus game window size
        var windowPos = ImGuiHelpers.MainViewport.Pos;
        var viewProjectionMatrix = *(Matrix4x4*)(matrixSingleton + 0x1b4);
        var device = Device.Instance();
        float width = device->Width;
        float height = device->Height;

        var pCoords = Vector3.Transform(worldPos, viewProjectionMatrix);
        screenPos = new Vector2(pCoords.X / MathF.Abs(pCoords.Z), pCoords.Y / MathF.Abs(pCoords.Z));

        screenPos.X = (0.5f * width * (screenPos.X + 1f)) + windowPos.X;
        screenPos.Y = (0.5f * height * (1f - screenPos.Y)) + windowPos.Y;

        var inFront = pCoords.Z > 0;
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

        // Get base object with matrices
        var matrixSingleton = this.getMatrixSingleton();

        // Read current ViewProjectionMatrix plus game window size
        var rawMatrix = (float*)(matrixSingleton + 0x1b4);
        var inverseViewProjectionMatrix = *(Matrix4x4*)rawMatrix;
        var width = *(rawMatrix + 16);
        var height = *(rawMatrix + 17);

        if (!Matrix4x4.Invert(inverseViewProjectionMatrix, out var viewProjectionMatrix))
            viewProjectionMatrix = Matrix4x4.Identity;

        var localScreenPos = new Vector2(screenPos.X - windowPos.X, screenPos.Y - windowPos.Y);
        var screenPos3D = new Vector3
        {
            X = (localScreenPos.X / width * 2.0f) - 1.0f,
            Y = -((localScreenPos.Y / height * 2.0f) - 1.0f),
            Z = 0,
        };

        var camPos = Vector3.Transform(screenPos3D, viewProjectionMatrix);

        screenPos3D.Z = 1;
        var camPosOne = Vector3.Transform(screenPos3D, viewProjectionMatrix);

        var clipPos = Vector3.Normalize(camPosOne - camPos);

        // This array is larger than necessary because it contains more info than we currently use
        var worldPosArray = default(RaycastHit);

        // Theory: this is some kind of flag on what type of things the ray collides with
        var unknown = stackalloc int[3]
        {
            0x4000,
            0x4000,
            0x0,
        };

        var isSuccess = BGCollisionModule.Raycast2(camPos, clipPos, rayDistance, &worldPosArray, unknown);
        worldPos = worldPosArray.Point;

        return isSuccess;
    }

    /// <inheritdoc/>
    public IntPtr GetUIModule()
    {
        var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
        if (framework == null)
            return IntPtr.Zero;

        var uiModule = framework->GetUiModule();
        if (uiModule == null)
            return IntPtr.Zero;

        return (IntPtr)uiModule;
    }

    /// <inheritdoc/>
    public IntPtr GetAddonByName(string name, int index = 1)
    {
        var atkStage = AtkStage.GetSingleton();
        if (atkStage == null)
            return IntPtr.Zero;

        var unitMgr = atkStage->RaptureAtkUnitManager;
        if (unitMgr == null)
            return IntPtr.Zero;

        var addon = unitMgr->GetAddonByName(name, index);
        if (addon == null)
            return IntPtr.Zero;

        return (IntPtr)addon;
    }

    /// <inheritdoc/>
    public IntPtr FindAgentInterface(string addonName)
    {
        var addon = this.GetAddonByName(addonName);
        return this.FindAgentInterface(addon);
    }

    /// <inheritdoc/>
    public IntPtr FindAgentInterface(void* addon) => this.FindAgentInterface((IntPtr)addon);
    
    /// <inheritdoc/>
    public IntPtr FindAgentInterface(IntPtr addonPtr)
    {
        if (addonPtr == IntPtr.Zero)
            return IntPtr.Zero;

        var uiModule = (UIModule*)this.GetUIModule();
        if (uiModule == null)
            return IntPtr.Zero;

        var agentModule = uiModule->GetAgentModule();
        if (agentModule == null)
            return IntPtr.Zero;

        var addon = (AtkUnitBase*)addonPtr;
        var addonId = addon->ParentID == 0 ? addon->ID : addon->ParentID;

        if (addonId == 0)
            return IntPtr.Zero;

        var index = 0;
        while (true)
        {
            var agent = agentModule->GetAgentByInternalID((uint)index++);
            if (agent == uiModule || agent == null)
                break;

            if (agent->AddonId == addonId)
                return new IntPtr(agent);
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Disables the hooks and submodules of this module.
    /// </summary>
    void IInternalDisposableService.DisposeService()
    {
        this.setGlobalBgmHook.Dispose();
        this.handleItemHoverHook.Dispose();
        this.handleItemOutHook.Dispose();
        this.handleImmHook.Dispose();
        this.toggleUiHideHook.Dispose();
        this.handleActionHoverHook.Dispose();
        this.handleActionOutHook.Dispose();
        this.utf8StringFromSequenceHook.Dispose();
    }

    /// <summary>
    /// Indicates if the game is on the title screen.
    /// </summary>
    /// <returns>A value indicating whether or not the game is on the title screen.</returns>
    internal bool IsOnTitleScreen()
    {
        var charaSelect = this.GetAddonByName("CharaSelect");
        var charaMake = this.GetAddonByName("CharaMake");
        var titleDcWorldMap = this.GetAddonByName("TitleDCWorldMap");
        if (charaMake != nint.Zero || charaSelect != nint.Zero || titleDcWorldMap != nint.Zero)
            return false;

        return !Service<ClientState.ClientState>.Get().IsLoggedIn;
    }

    /// <summary>
    /// Set the current background music.
    /// </summary>
    /// <param name="bgmKey">The background music key.</param>
    internal void SetBgm(ushort bgmKey) => this.setGlobalBgmHook.Original(bgmKey, 0, 0, 0, 0, 0);

    /// <summary>
    /// Reset the stored "UI hide" state.
    /// </summary>
    internal void ResetUiHideState()
    {
        this.GameUiHidden = false;
    }

    private IntPtr HandleSetGlobalBgmDetour(ushort bgmKey, byte a2, uint a3, uint a4, uint a5, byte a6)
    {
        var retVal = this.setGlobalBgmHook.Original(bgmKey, a2, a3, a4, a5, a6);

        Log.Verbose("SetGlobalBgm: {0} {1} {2} {3} {4} {5} -> {6}", bgmKey, a2, a3, a4, a5, a6, retVal);

        return retVal;
    }

    private IntPtr HandleItemHoverDetour(IntPtr hoverState, IntPtr a2, IntPtr a3, ulong a4)
    {
        var retVal = this.handleItemHoverHook.Original(hoverState, a2, a3, a4);

        if (retVal.ToInt64() == 22)
        {
            var itemId = (ulong)Marshal.ReadInt32(hoverState, 0x138);
            this.HoveredItem = itemId;

            this.HoveredItemChanged?.InvokeSafely(this, itemId);

            Log.Verbose($"HoverItemId:{itemId} this:{hoverState.ToInt64()}");
        }

        return retVal;
    }

    private IntPtr HandleItemOutDetour(IntPtr hoverState, IntPtr a2, IntPtr a3, ulong a4)
    {
        var retVal = this.handleItemOutHook.Original(hoverState, a2, a3, a4);

        if (a3 != IntPtr.Zero && a4 == 1)
        {
            var a3Val = Marshal.ReadByte(a3, 0x8);

            if (a3Val == 255)
            {
                this.HoveredItem = 0ul;

                try
                {
                    this.HoveredItemChanged?.Invoke(this, 0ul);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Could not dispatch HoveredItemChanged event.");
                }

                Log.Verbose("HoverItemId: 0");
            }
        }

        return retVal;
    }

    private void HandleActionHoverDetour(IntPtr hoverState, HoverActionKind actionKind, uint actionId, int a4, byte a5)
    {
        this.handleActionHoverHook.Original(hoverState, actionKind, actionId, a4, a5);
        this.HoveredAction.ActionKind = actionKind;
        this.HoveredAction.BaseActionID = actionId;
        this.HoveredAction.ActionID = (uint)Marshal.ReadInt32(hoverState, 0x3C);
        this.HoveredActionChanged?.InvokeSafely(this, this.HoveredAction);

        Log.Verbose($"HoverActionId: {actionKind}/{actionId} this:{hoverState.ToInt64():X}");
    }

    private IntPtr HandleActionOutDetour(IntPtr agentActionDetail, IntPtr a2, IntPtr a3, int a4)
    {
        var retVal = this.handleActionOutHook.Original(agentActionDetail, a2, a3, a4);

        if (a3 != IntPtr.Zero && a4 == 1)
        {
            var a3Val = Marshal.ReadByte(a3, 0x8);

            if (a3Val == 255)
            {
                this.HoveredAction.ActionKind = HoverActionKind.None;
                this.HoveredAction.BaseActionID = 0;
                this.HoveredAction.ActionID = 0;

                try
                {
                    this.HoveredActionChanged?.Invoke(this, this.HoveredAction);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Could not dispatch HoveredActionChanged event.");
                }

                Log.Verbose("HoverActionId: 0");
            }
        }

        return retVal;
    }

    private IntPtr ToggleUiHideDetour(IntPtr thisPtr, bool unknownByte)
    {
        var result = this.toggleUiHideHook.Original(thisPtr, unknownByte);

        this.GameUiHidden = !RaptureAtkModule.Instance()->IsUiVisible;
        this.UiHideToggled?.InvokeSafely(this, this.GameUiHidden);

        Log.Debug("UiHide toggled: {0}", this.GameUiHidden);

        return result;
    }

    private char HandleImmDetour(IntPtr framework, char a2, byte a3)
    {
        var result = this.handleImmHook.Original(framework, a2, a3);
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
[InterfaceVersion("1.0")]
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
    public IntPtr GetUIModule()
        => this.gameGuiService.GetUIModule();
    
    /// <inheritdoc/>
    public IntPtr GetAddonByName(string name, int index = 1)
        => this.gameGuiService.GetAddonByName(name, index);
    
    /// <inheritdoc/>
    public IntPtr FindAgentInterface(string addonName)
        => this.gameGuiService.FindAgentInterface(addonName);
    
    /// <inheritdoc/>
    public unsafe IntPtr FindAgentInterface(void* addon) 
        => this.gameGuiService.FindAgentInterface(addon);

    /// <inheritdoc/>
    public IntPtr FindAgentInterface(IntPtr addonPtr) 
        => this.gameGuiService.FindAgentInterface(addonPtr);

    private void UiHideToggledForward(object sender, bool toggled) => this.UiHideToggled?.Invoke(sender, toggled);
    
    private void HoveredItemForward(object sender, ulong itemId) => this.HoveredItemChanged?.Invoke(sender, itemId);
    
    private void HoveredActionForward(object sender, HoveredAction hoverAction) => this.HoveredActionChanged?.Invoke(sender, hoverAction);
}
