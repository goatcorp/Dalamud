using System;
using System.Runtime.InteropServices;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Interface;
using ImGuiNET;
using Serilog;
using SharpDX;

namespace Dalamud.Game.Internal.Gui {
    public sealed class GameGui : IDisposable
    {
        private readonly Dalamud dalamud;

        private GameGuiAddressResolver Address { get; }
        
        public ChatGui Chat { get; private set; }
        public PartyFinderGui PartyFinder { get; private set; }
        public ToastGui Toast { get; private set; }

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr SetGlobalBgmDelegate(UInt16 bgmKey, byte a2, UInt32 a3, UInt32 a4, UInt32 a5, byte a6);
        private readonly Hook<SetGlobalBgmDelegate> setGlobalBgmHook;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr HandleItemHoverDelegate(IntPtr hoverState, IntPtr a2, IntPtr a3, ulong a4);
        private readonly Hook<HandleItemHoverDelegate> handleItemHoverHook;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr HandleItemOutDelegate(IntPtr hoverState, IntPtr a2, IntPtr a3, ulong a4);
        private readonly Hook<HandleItemOutDelegate> handleItemOutHook;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void HandleActionHoverDelegate(IntPtr hoverState, HoverActionKind a2, uint a3, int a4, byte a5);
        private readonly Hook<HandleActionHoverDelegate> handleActionHoverHook;
        
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr HandleActionOutDelegate(IntPtr agentActionDetail, IntPtr a2, IntPtr a3, int a4);
        private Hook<HandleActionOutDelegate> handleActionOutHook;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetUIObjectDelegate();
        private readonly GetUIObjectDelegate getUIObject;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr GetUIMapObjectDelegate(IntPtr UIObject);
        private GetUIMapObjectDelegate getUIMapObject;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate bool OpenMapWithFlagDelegate(IntPtr UIMapObject, string flag);
        private OpenMapWithFlagDelegate openMapWithFlag;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr GetMatrixSingletonDelegate();
        internal readonly GetMatrixSingletonDelegate getMatrixSingleton;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private unsafe delegate bool ScreenToWorldNativeDelegate(
            float *camPos, float *clipPos, float rayDistance, float *worldPos, int *unknown);
        private readonly ScreenToWorldNativeDelegate screenToWorldNative;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr ToggleUiHideDelegate(IntPtr thisPtr, byte unknownByte);
        private readonly Hook<ToggleUiHideDelegate> toggleUiHideHook;

        // Return a Client::UI::UIModule
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr GetBaseUIObjectDelegate();
        public readonly GetBaseUIObjectDelegate GetBaseUIObject;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr GetUIObjectByNameDelegate(IntPtr thisPtr, string uiName, int index);
        private readonly GetUIObjectByNameDelegate getUIObjectByName;

        private delegate IntPtr GetUiModuleDelegate(IntPtr basePtr);
        private readonly GetUiModuleDelegate getUiModule;

        private delegate IntPtr GetAgentModuleDelegate(IntPtr uiModule);
        private GetAgentModuleDelegate getAgentModule;

        public bool GameUiHidden { get; private set; }

        /// <summary>
        /// Event which is fired when the game UI hiding is toggled.
        /// </summary>
        public event EventHandler<bool> OnUiHideToggled; 

        /// <summary>
        /// The item ID that is currently hovered by the player. 0 when no item is hovered.
        /// If > 1.000.000, subtract 1.000.000 and treat it as HQ
        /// </summary>
        public ulong HoveredItem { get; set; }

        /// <summary>
        /// The action ID that is current hovered by the player. 0 when no action is hovered.
        /// </summary>
        public HoveredAction HoveredAction { get; } = new HoveredAction();
        
        /// <summary>
        /// Event that is fired when the currently hovered item changes.
        /// </summary>
        public EventHandler<ulong> HoveredItemChanged { get; set; }
        
        /// <summary>
        /// Event that is fired when the currently hovered action changes.
        /// </summary>
        public EventHandler<HoveredAction> HoveredActionChanged { get; set; }

        public GameGui(IntPtr baseAddress, SigScanner scanner, Dalamud dalamud)
        {
            this.dalamud = dalamud;

            Address = new GameGuiAddressResolver(baseAddress);
            Address.Setup(scanner);

            Log.Verbose("===== G A M E G U I =====");

            Log.Verbose("GameGuiManager address {Address}", Address.BaseAddress);
            Log.Verbose("SetGlobalBgm address {Address}", Address.SetGlobalBgm);
            Log.Verbose("HandleItemHover address {Address}", Address.HandleItemHover);
            Log.Verbose("HandleItemOut address {Address}", Address.HandleItemOut);
            Log.Verbose("GetUIObject address {Address}", Address.GetUIObject);
            Log.Verbose("GetAgentModule address {Address}", Address.GetAgentModule);

            Chat = new ChatGui(Address.ChatManager, scanner, dalamud);
            PartyFinder = new PartyFinderGui(scanner, dalamud);
            Toast = new ToastGui(scanner, dalamud);

            this.setGlobalBgmHook =
                new Hook<SetGlobalBgmDelegate>(Address.SetGlobalBgm,
                                                   new SetGlobalBgmDelegate(HandleSetGlobalBgmDetour),
                                                   this);
            this.handleItemHoverHook =
                new Hook<HandleItemHoverDelegate>(Address.HandleItemHover,
                                               new HandleItemHoverDelegate(HandleItemHoverDetour),
                                               this);

            this.handleItemOutHook =
                new Hook<HandleItemOutDelegate>(Address.HandleItemOut,
                                                  new HandleItemOutDelegate(HandleItemOutDetour),
                                                  this);

            this.handleActionHoverHook =
                new Hook<HandleActionHoverDelegate>(Address.HandleActionHover,
                                                        new HandleActionHoverDelegate(HandleActionHoverDetour),
                                                        this);
            this.handleActionOutHook =
                new Hook<HandleActionOutDelegate>(Address.HandleActionOut,
                                                        new HandleActionOutDelegate(HandleActionOutDetour),
                                                        this);
            
            this.getUIObject = Marshal.GetDelegateForFunctionPointer<GetUIObjectDelegate>(Address.GetUIObject);

            this.getMatrixSingleton =
                Marshal.GetDelegateForFunctionPointer<GetMatrixSingletonDelegate>(Address.GetMatrixSingleton);

            this.screenToWorldNative =
                Marshal.GetDelegateForFunctionPointer<ScreenToWorldNativeDelegate>(Address.ScreenToWorld);

            this.toggleUiHideHook = new Hook<ToggleUiHideDelegate>(Address.ToggleUiHide, new ToggleUiHideDelegate(ToggleUiHideDetour), this);

            this.GetBaseUIObject = Marshal.GetDelegateForFunctionPointer<GetBaseUIObjectDelegate>(Address.GetBaseUIObject);
            this.getUIObjectByName = Marshal.GetDelegateForFunctionPointer<GetUIObjectByNameDelegate>(Address.GetUIObjectByName);

            this.getUiModule = Marshal.GetDelegateForFunctionPointer<GetUiModuleDelegate>(Address.GetUIModule);
            this.getAgentModule = Marshal.GetDelegateForFunctionPointer<GetAgentModuleDelegate>(Address.GetAgentModule);
        }

        private IntPtr HandleSetGlobalBgmDetour(UInt16 bgmKey, byte a2, UInt32 a3, UInt32 a4, UInt32 a5, byte a6) {
            var retVal = this.setGlobalBgmHook.Original(bgmKey, a2, a3, a4, a5, a6);

            Log.Verbose("SetGlobalBgm: {0} {1} {2} {3} {4} {5} -> {6}", bgmKey, a2, a3, a4, a5, a6, retVal);

            return retVal;
        }

        private IntPtr HandleItemHoverDetour(IntPtr hoverState, IntPtr a2, IntPtr a3, ulong a4) {
            var retVal = this.handleItemHoverHook.Original(hoverState, a2, a3, a4);

            if (retVal.ToInt64() == 22) {
                var itemId = (ulong)Marshal.ReadInt32(hoverState, 0x138);
                this.HoveredItem = itemId;

                try {
                    HoveredItemChanged?.Invoke(this, itemId);
                } catch (Exception e) {
                    Log.Error(e, "Could not dispatch HoveredItemChanged event.");
                }

                Log.Verbose("HoverItemId:{0} this:{1}", itemId, hoverState.ToInt64().ToString("X"));
            }

            return retVal;
        }

        private IntPtr HandleItemOutDetour(IntPtr hoverState, IntPtr a2, IntPtr a3, ulong a4)
        {
            var retVal = this.handleItemOutHook.Original(hoverState, a2, a3, a4);

            if (a3 != IntPtr.Zero && a4 == 1) {
                var a3Val = Marshal.ReadByte(a3, 0x8);

                if (a3Val == 255) {
                    this.HoveredItem = 0ul;

                    try {
                        HoveredItemChanged?.Invoke(this, 0ul);
                    } catch (Exception e) {
                        Log.Error(e, "Could not dispatch HoveredItemChanged event.");
                    }

                    Log.Verbose("HoverItemId: 0");
                }
            }

            return retVal;
        }

        private void HandleActionHoverDetour(IntPtr hoverState, HoverActionKind actionKind, uint actionId, int a4, byte a5)
        {
            handleActionHoverHook.Original(hoverState, actionKind, actionId, a4, a5);
            HoveredAction.ActionKind = actionKind;
            HoveredAction.BaseActionID = actionId;
            HoveredAction.ActionID = (uint) Marshal.ReadInt32(hoverState, 0x3C);
            try
            {
                HoveredActionChanged?.Invoke(this, this.HoveredAction);
            } catch (Exception e)
            {
                Log.Error(e, "Could not dispatch HoveredItemChanged event.");
            }
            Log.Verbose("HoverActionId: {0}/{1} this:{2}", actionKind, actionId, hoverState.ToInt64().ToString("X"));
        }
        
        private IntPtr HandleActionOutDetour(IntPtr agentActionDetail, IntPtr a2, IntPtr a3, int a4)
        {
            var retVal = handleActionOutHook.Original(agentActionDetail, a2, a3, a4);
           
            if (a3 != IntPtr.Zero && a4 == 1)
            {
                var a3Val = Marshal.ReadByte(a3, 0x8);

                if (a3Val == 255)
                {
                    this.HoveredAction.ActionKind = HoverActionKind.None;
                    HoveredAction.BaseActionID = 0;
                    HoveredAction.ActionID = 0;

                    try
                    {
                        HoveredActionChanged?.Invoke(this, this.HoveredAction);
                    } catch (Exception e)
                    {
                        Log.Error(e, "Could not dispatch HoveredActionChanged event.");
                    }

                    Log.Verbose("HoverActionId: 0");
                }
            }
            
            return retVal;
        }

        /// <summary>
        /// Opens the in-game map with a flag on the location of the parameter
        /// </summary>
        /// <param name="mapLink">Link to the map to be opened</param>
        /// <returns>True if there were no errors and it could open the map</returns>
        public bool OpenMapWithMapLink(MapLinkPayload mapLink) {
            var uiObjectPtr = this.getUIObject();

            if (uiObjectPtr.Equals(IntPtr.Zero)) {
                Log.Error("OpenMapWithMapLink: Null pointer returned from getUIObject()");
                return false;
            }

            this.getUIMapObject =
                Address.GetVirtualFunction<GetUIMapObjectDelegate>(uiObjectPtr, 0, 8);


            var uiMapObjectPtr = this.getUIMapObject(uiObjectPtr);

            if (uiMapObjectPtr.Equals(IntPtr.Zero)) {
                Log.Error("OpenMapWithMapLink: Null pointer returned from GetUIMapObject()");
                return false;
            }

            this.openMapWithFlag =
                Address.GetVirtualFunction<OpenMapWithFlagDelegate>(uiMapObjectPtr, 0, 63);

            var mapLinkString = mapLink.DataString;

            Log.Debug($"OpenMapWithMapLink: Opening Map Link: {mapLinkString}");

            return this.openMapWithFlag(uiMapObjectPtr, mapLinkString);
        }

        /// <summary>
        /// Converts in-world coordinates to screen coordinates (upper left corner origin).
        /// </summary>
        /// <param name="worldPos">Coordinates in the world</param>
        /// <param name="screenPos">Converted coordinates</param>
        /// <returns>True if worldPos corresponds to a position in front of the camera</returns>
        public bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos)
        {
            // Get base object with matrices
            var matrixSingleton = this.getMatrixSingleton();

            // Read current ViewProjectionMatrix plus game window size
            var viewProjectionMatrix = new Matrix();
            float width, height;
            var windowPos = ImGuiHelpers.MainViewport.Pos;

            unsafe {
                var rawMatrix = (float*) (matrixSingleton + 0x1b4).ToPointer();

                for (var i = 0; i < 16; i++, rawMatrix++)
                    viewProjectionMatrix[i] = *rawMatrix;

                width = *rawMatrix; 
                height = *(rawMatrix + 1);
            }

            Vector3.Transform( ref worldPos, ref viewProjectionMatrix, out Vector3 pCoords);

            screenPos = new Vector2(pCoords.X / pCoords.Z, pCoords.Y / pCoords.Z);

            screenPos.X = 0.5f * width * (screenPos.X + 1f) + windowPos.X;
            screenPos.Y = 0.5f * height * (1f - screenPos.Y) + windowPos.Y;

            return pCoords.Z > 0 &&
                   screenPos.X > windowPos.X && screenPos.X < windowPos.X + width &&
                   screenPos.Y > windowPos.Y && screenPos.Y < windowPos.Y + height;
        }

        /// <summary>
        /// Converts screen coordinates to in-world coordinates via raycasting.
        /// </summary>
        /// <param name="screenPos">Screen coordinates</param>
        /// <param name="worldPos">Converted coordinates</param>
        /// <param name="rayDistance">How far to search for a collision</param>
        /// <returns>True if successful. On false, worldPos's contents are undefined</returns>
        public bool ScreenToWorld(Vector2 screenPos, out Vector3 worldPos, float rayDistance = 100000.0f)
        {
            // The game is only visible in the main viewport, so if the cursor is outside
            // of the game window, do not bother calculating anything
            var windowPos = ImGuiHelpers.MainViewport.Pos;
            var windowSize = ImGuiHelpers.MainViewport.Size;

            if (screenPos.X < windowPos.X || screenPos.X > windowPos.X + windowSize.X ||
                screenPos.Y < windowPos.Y || screenPos.Y > windowPos.Y + windowSize.Y)
            {
                worldPos = new Vector3();
                return false;
            }

            // Get base object with matrices
            var matrixSingleton = this.getMatrixSingleton();

            // Read current ViewProjectionMatrix plus game window size
            var viewProjectionMatrix = new Matrix();
            float width, height;
            unsafe
            {
                var rawMatrix = (float*)(matrixSingleton + 0x1b4).ToPointer();

                for (var i = 0; i < 16; i++, rawMatrix++)
                    viewProjectionMatrix[i] = *rawMatrix;

                width = *rawMatrix;
                height = *(rawMatrix + 1);
            }

            viewProjectionMatrix.Invert();

            var localScreenPos = new Vector2(screenPos.X - windowPos.X, screenPos.Y - windowPos.Y);
            var screenPos3D = new Vector3 {
                X = localScreenPos.X / width * 2.0f - 1.0f,
                Y = -(localScreenPos.Y / height * 2.0f - 1.0f),
                Z = 0
            };

            Vector3.TransformCoordinate(ref screenPos3D, ref viewProjectionMatrix, out var camPos);
            
            screenPos3D.Z = 1;
            Vector3.TransformCoordinate(ref screenPos3D, ref viewProjectionMatrix, out var camPosOne);

            var clipPos = camPosOne - camPos;
            clipPos.Normalize();

            bool isSuccess;
            unsafe {
                var camPosArray = camPos.ToArray();
                var clipPosArray = clipPos.ToArray();

                // This array is larger than necessary because it contains more info than we currently use
                var worldPosArray = stackalloc float[32];

                // Theory: this is some kind of flag on what type of things the ray collides with
                var unknown = stackalloc int[3] { 0x4000, 0x4000, 0x0 };

                fixed (float* pCamPos = camPosArray) {
                    fixed (float* pClipPos = clipPosArray) {
                        isSuccess = this.screenToWorldNative(pCamPos, pClipPos, rayDistance, worldPosArray, unknown);
                    }
                }

                worldPos = new Vector3 {
                    X = worldPosArray[0],
                    Y = worldPosArray[1],
                    Z = worldPosArray[2]
                };
            }

            return isSuccess;
        }

        private IntPtr ToggleUiHideDetour(IntPtr thisPtr, byte unknownByte) {
            GameUiHidden = !GameUiHidden;

            try {
                OnUiHideToggled?.Invoke(this, GameUiHidden);
            } catch (Exception ex) {
                Log.Error(ex, "Error on OnUiHideToggled event dispatch");
            }
            
            Log.Debug("UiHide toggled: {0}", GameUiHidden);

            return this.toggleUiHideHook.Original(thisPtr, unknownByte);
        }

        /// <summary>
        /// Gets a pointer to the game's UI module.
        /// </summary>
        /// <returns>IntPtr pointing to UI module</returns>
        public IntPtr GetUIModule()
        {
            return this.getUiModule(this.dalamud.Framework.Address.BaseAddress);
        }

        /// <summary>
        /// Gets the pointer to the UI Object with the given name and index.
        /// </summary>
        /// <param name="name">Name of UI to find</param>
        /// <param name="index">Index of UI to find (1-indexed)</param>
        /// <returns>IntPtr.Zero if unable to find UI, otherwise IntPtr pointing to the start of the UI Object</returns>
        public IntPtr GetUiObjectByName(string name, int index) {
            var baseUi = this.GetBaseUIObject();
            if (baseUi == IntPtr.Zero) return IntPtr.Zero;
            var baseUiProperties = Marshal.ReadIntPtr(baseUi, 0x20);
            if (baseUiProperties == IntPtr.Zero) return IntPtr.Zero;
            return this.getUIObjectByName(baseUiProperties, name, index);
        }

        public Addon.Addon GetAddonByName(string name, int index) {
            var addonMem = GetUiObjectByName(name, index);
            if (addonMem == IntPtr.Zero) return null;
            var addonStruct = Marshal.PtrToStructure<Structs.Addon>(addonMem);
            return new Addon.Addon(addonMem, addonStruct);
        }

        public IntPtr FindAgentInterface(string addonName)
        {
            var addon = this.dalamud.Framework.Gui.GetUiObjectByName(addonName, 1);
            return this.FindAgentInterface(addon);
        }

        public IntPtr FindAgentInterface(IntPtr addon)
        {
            if (addon == IntPtr.Zero)
                return IntPtr.Zero;

            var uiModule = this.dalamud.Framework.Gui.GetUIModule();
            if (uiModule == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            var agentModule = this.getAgentModule(uiModule);
            if (agentModule == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            var id = Marshal.ReadInt16(addon, 0x1CE);
            if (id == 0)
                id = Marshal.ReadInt16(addon, 0x1CC);

            if (id == 0)
                return IntPtr.Zero;

            for (var i = 0; i < 379; i++)
            {
                var agent = Marshal.ReadIntPtr(agentModule, 0x20 + (i * 8));
                if (agent == IntPtr.Zero)
                    continue;
                if (Marshal.ReadInt32(agent, 0x20) == id)
                    return agent;
            }

            return IntPtr.Zero;
        }

        public void SetBgm(ushort bgmKey) => this.setGlobalBgmHook.Original(bgmKey, 0, 0, 0, 0, 0); 

        public void Enable() {
            Chat.Enable();
            Toast.Enable();
            PartyFinder.Enable();
            this.setGlobalBgmHook.Enable();
            this.handleItemHoverHook.Enable();
            this.handleItemOutHook.Enable();
            this.toggleUiHideHook.Enable();
            this.handleActionHoverHook.Enable();
            this.handleActionOutHook.Enable();
        }

        public void Dispose() {
            Chat.Dispose();
            Toast.Dispose();
            PartyFinder.Dispose();
            this.setGlobalBgmHook.Dispose();
            this.handleItemHoverHook.Dispose();
            this.handleItemOutHook.Dispose();
            this.toggleUiHideHook.Dispose();
            this.handleActionHoverHook.Dispose();
            this.handleActionOutHook.Dispose();
        }
    }
}
