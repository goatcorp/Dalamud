using System;
using System.Runtime.InteropServices;

using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Interface;
using Serilog;
using SharpDX;

namespace Dalamud.Game.Internal.Gui
{
    /// <summary>
    /// A class handling many aspects of the in-game UI.
    /// </summary>
    public sealed class GameGui : IDisposable
    {
        /// <summary>
        /// The delegate of the native method that gets the Client::UI::UIModule address.
        /// </summary>
        /// <returns>The Client::UI::UIModule address.</returns>
        public readonly GetBaseUIObjectDelegate GetBaseUIObject;

        private readonly Dalamud dalamud;
        private readonly GameGuiAddressResolver address;

        private readonly GetMatrixSingletonDelegate getMatrixSingleton;
        private readonly GetUIObjectDelegate getUIObject;
        private readonly ScreenToWorldNativeDelegate screenToWorldNative;
        private readonly GetUIObjectByNameDelegate getUIObjectByName;
        private readonly GetUiModuleDelegate getUiModule;
        private readonly GetAgentModuleDelegate getAgentModule;

        private readonly Hook<SetGlobalBgmDelegate> setGlobalBgmHook;
        private readonly Hook<HandleItemHoverDelegate> handleItemHoverHook;
        private readonly Hook<HandleItemOutDelegate> handleItemOutHook;
        private readonly Hook<HandleActionHoverDelegate> handleActionHoverHook;
        private readonly Hook<HandleActionOutDelegate> handleActionOutHook;
        private readonly Hook<ToggleUiHideDelegate> toggleUiHideHook;

        private GetUIMapObjectDelegate getUIMapObject;
        private OpenMapWithFlagDelegate openMapWithFlag;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameGui"/> class.
        /// This class is responsible for many aspects of interacting with the native game UI.
        /// </summary>
        /// <param name="baseAddress">The base address of the native GuiManager class.</param>
        /// <param name="scanner">The SigScanner instance.</param>
        /// <param name="dalamud">The Dalamud instance.</param>
        internal GameGui(IntPtr baseAddress, SigScanner scanner, Dalamud dalamud)
        {
            this.dalamud = dalamud;

            this.address = new GameGuiAddressResolver(baseAddress);
            this.address.Setup(scanner);

            Log.Verbose("===== G A M E G U I =====");

            Log.Verbose($"GameGuiManager address 0x{this.address.BaseAddress.ToInt64():X}");
            Log.Verbose($"SetGlobalBgm address 0x{this.address.SetGlobalBgm.ToInt64():X}");
            Log.Verbose($"HandleItemHover address 0x{this.address.HandleItemHover.ToInt64():X}");
            Log.Verbose($"HandleItemOut address 0x{this.address.HandleItemOut.ToInt64():X}");
            Log.Verbose($"GetUIObject address 0x{this.address.GetUIObject.ToInt64():X}");
            Log.Verbose($"GetAgentModule address 0x{this.address.GetAgentModule.ToInt64():X}");

            this.Chat = new ChatGui(this.address.ChatManager, scanner, dalamud);
            this.PartyFinder = new PartyFinderGui(scanner, dalamud);
            this.Toast = new ToastGui(scanner, dalamud);

            this.setGlobalBgmHook = new Hook<SetGlobalBgmDelegate>(this.address.SetGlobalBgm, this.HandleSetGlobalBgmDetour);
            this.handleItemHoverHook = new Hook<HandleItemHoverDelegate>(this.address.HandleItemHover, this.HandleItemHoverDetour);

            this.handleItemOutHook = new Hook<HandleItemOutDelegate>(this.address.HandleItemOut, this.HandleItemOutDetour);

            this.handleActionHoverHook = new Hook<HandleActionHoverDelegate>(this.address.HandleActionHover, this.HandleActionHoverDetour);
            this.handleActionOutHook = new Hook<HandleActionOutDelegate>(this.address.HandleActionOut, this.HandleActionOutDetour);

            this.getUIObject = Marshal.GetDelegateForFunctionPointer<GetUIObjectDelegate>(this.address.GetUIObject);

            this.getMatrixSingleton = Marshal.GetDelegateForFunctionPointer<GetMatrixSingletonDelegate>(this.address.GetMatrixSingleton);

            this.screenToWorldNative = Marshal.GetDelegateForFunctionPointer<ScreenToWorldNativeDelegate>(this.address.ScreenToWorld);

            this.toggleUiHideHook = new Hook<ToggleUiHideDelegate>(this.address.ToggleUiHide, this.ToggleUiHideDetour);

            this.GetBaseUIObject = Marshal.GetDelegateForFunctionPointer<GetBaseUIObjectDelegate>(this.address.GetBaseUIObject);
            this.getUIObjectByName = Marshal.GetDelegateForFunctionPointer<GetUIObjectByNameDelegate>(this.address.GetUIObjectByName);

            this.getUiModule = Marshal.GetDelegateForFunctionPointer<GetUiModuleDelegate>(this.address.GetUIModule);
            this.getAgentModule = Marshal.GetDelegateForFunctionPointer<GetAgentModuleDelegate>(this.address.GetAgentModule);
        }

        // Marshaled delegates

        /// <summary>
        /// The delegate type of the native method that gets the Client::UI::UIModule address.
        /// </summary>
        /// <returns>The Client::UI::UIModule address.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr GetBaseUIObjectDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetMatrixSingletonDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetUIObjectDelegate();

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private unsafe delegate bool ScreenToWorldNativeDelegate(float* camPos, float* clipPos, float rayDistance, float* worldPos, int* unknown);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr GetUIObjectByNameDelegate(IntPtr thisPtr, string uiName, int index);

        private delegate IntPtr GetUiModuleDelegate(IntPtr basePtr);

        private delegate IntPtr GetAgentModuleDelegate(IntPtr uiModule);

        // Hooked delegates

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
        private delegate IntPtr ToggleUiHideDelegate(IntPtr thisPtr, byte unknownByte);

        /// <summary>
        /// Event which is fired when the game UI hiding is toggled.
        /// </summary>
        public event EventHandler<bool> OnUiHideToggled;

        /// <summary>
        /// Gets the <see cref="Chat"/> instance.
        /// </summary>
        public ChatGui Chat { get; private set; }

        /// <summary>
        /// Gets the <see cref="PartyFinder"/> instance.
        /// </summary>
        public PartyFinderGui PartyFinder { get; private set; }

        /// <summary>
        /// Gets the <see cref="Toast"/> instance.
        /// </summary>
        public ToastGui Toast { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the game UI is hidden.
        /// </summary>
        public bool GameUiHidden { get; private set; }

        /// <summary>
        /// Gets or sets the item ID that is currently hovered by the player. 0 when no item is hovered.
        /// If > 1.000.000, subtract 1.000.000 and treat it as HQ.
        /// </summary>
        public ulong HoveredItem { get; set; }

        /// <summary>
        /// Gets the action ID that is current hovered by the player. 0 when no action is hovered.
        /// </summary>
        public HoveredAction HoveredAction { get; } = new HoveredAction();

        /// <summary>
        /// Gets or sets the event that is fired when the currently hovered item changes.
        /// </summary>
        public EventHandler<ulong> HoveredItemChanged { get; set; }

        /// <summary>
        /// Gets or sets the event that is fired when the currently hovered action changes.
        /// </summary>
        public EventHandler<HoveredAction> HoveredActionChanged { get; set; }

        /// <summary>
        /// Opens the in-game map with a flag on the location of the parameter.
        /// </summary>
        /// <param name="mapLink">Link to the map to be opened.</param>
        /// <returns>True if there were no errors and it could open the map.</returns>
        public bool OpenMapWithMapLink(MapLinkPayload mapLink)
        {
            var uiObjectPtr = this.getUIObject();

            if (uiObjectPtr.Equals(IntPtr.Zero))
            {
                Log.Error("OpenMapWithMapLink: Null pointer returned from getUIObject()");
                return false;
            }

            this.getUIMapObject = this.address.GetVirtualFunction<GetUIMapObjectDelegate>(uiObjectPtr, 0, 8);

            var uiMapObjectPtr = this.getUIMapObject(uiObjectPtr);

            if (uiMapObjectPtr.Equals(IntPtr.Zero))
            {
                Log.Error("OpenMapWithMapLink: Null pointer returned from GetUIMapObject()");
                return false;
            }

            this.openMapWithFlag = this.address.GetVirtualFunction<OpenMapWithFlagDelegate>(uiMapObjectPtr, 0, 63);

            var mapLinkString = mapLink.DataString;

            Log.Debug($"OpenMapWithMapLink: Opening Map Link: {mapLinkString}");

            return this.openMapWithFlag(uiMapObjectPtr, mapLinkString);
        }

        /// <summary>
        /// Converts in-world coordinates to screen coordinates (upper left corner origin).
        /// </summary>
        /// <param name="worldPos">Coordinates in the world.</param>
        /// <param name="screenPos">Converted coordinates.</param>
        /// <returns>True if worldPos corresponds to a position in front of the camera.</returns>
        public bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos)
        {
            // Get base object with matrices
            var matrixSingleton = this.getMatrixSingleton();

            // Read current ViewProjectionMatrix plus game window size
            var viewProjectionMatrix = default(Matrix);
            float width, height;
            var windowPos = ImGuiHelpers.MainViewport.Pos;

            unsafe
            {
                var rawMatrix = (float*)(matrixSingleton + 0x1b4).ToPointer();

                for (var i = 0; i < 16; i++, rawMatrix++)
                    viewProjectionMatrix[i] = *rawMatrix;

                width = *rawMatrix;
                height = *(rawMatrix + 1);
            }

            Vector3.Transform(ref worldPos, ref viewProjectionMatrix, out Vector3 pCoords);

            screenPos = new Vector2(pCoords.X / pCoords.Z, pCoords.Y / pCoords.Z);

            screenPos.X = (0.5f * width * (screenPos.X + 1f)) + windowPos.X;
            screenPos.Y = (0.5f * height * (1f - screenPos.Y)) + windowPos.Y;

            return pCoords.Z > 0 &&
                   screenPos.X > windowPos.X && screenPos.X < windowPos.X + width &&
                   screenPos.Y > windowPos.Y && screenPos.Y < windowPos.Y + height;
        }

        /// <summary>
        /// Converts screen coordinates to in-world coordinates via raycasting.
        /// </summary>
        /// <param name="screenPos">Screen coordinates.</param>
        /// <param name="worldPos">Converted coordinates.</param>
        /// <param name="rayDistance">How far to search for a collision.</param>
        /// <returns>True if successful. On false, worldPos's contents are undefined.</returns>
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
            var viewProjectionMatrix = default(Matrix);
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
            var screenPos3D = new Vector3
            {
                X = (localScreenPos.X / width * 2.0f) - 1.0f,
                Y = -((localScreenPos.Y / height * 2.0f) - 1.0f),
                Z = 0,
            };

            Vector3.TransformCoordinate(ref screenPos3D, ref viewProjectionMatrix, out var camPos);

            screenPos3D.Z = 1;
            Vector3.TransformCoordinate(ref screenPos3D, ref viewProjectionMatrix, out var camPosOne);

            var clipPos = camPosOne - camPos;
            clipPos.Normalize();

            bool isSuccess;
            unsafe
            {
                var camPosArray = camPos.ToArray();
                var clipPosArray = clipPos.ToArray();

                // This array is larger than necessary because it contains more info than we currently use
                var worldPosArray = stackalloc float[32];

                // Theory: this is some kind of flag on what type of things the ray collides with
                var unknown = stackalloc int[3]
                {
                    0x4000,
                    0x4000,
                    0x0,
                };

                fixed (float* pCamPos = camPosArray)
                {
                    fixed (float* pClipPos = clipPosArray)
                    {
                        isSuccess = this.screenToWorldNative(pCamPos, pClipPos, rayDistance, worldPosArray, unknown);
                    }
                }

                worldPos = new Vector3
                {
                    X = worldPosArray[0],
                    Y = worldPosArray[1],
                    Z = worldPosArray[2],
                };
            }

            return isSuccess;
        }

        /// <summary>
        /// Gets a pointer to the game's UI module.
        /// </summary>
        /// <returns>IntPtr pointing to UI module.</returns>
        public IntPtr GetUIModule() => this.getUiModule(this.dalamud.Framework.Address.BaseAddress);

        /// <summary>
        /// Gets the pointer to the UI Object with the given name and index.
        /// </summary>
        /// <param name="name">Name of UI to find.</param>
        /// <param name="index">Index of UI to find (1-indexed).</param>
        /// <returns>IntPtr.Zero if unable to find UI, otherwise IntPtr pointing to the start of the UI Object.</returns>
        public IntPtr GetUiObjectByName(string name, int index)
        {
            var baseUi = this.GetBaseUIObject();
            if (baseUi == IntPtr.Zero) return IntPtr.Zero;
            var baseUiProperties = Marshal.ReadIntPtr(baseUi, 0x20);
            if (baseUiProperties == IntPtr.Zero) return IntPtr.Zero;
            return this.getUIObjectByName(baseUiProperties, name, index);
        }

        /// <summary>
        /// Gets an Addon by it's internal name.
        /// </summary>
        /// <param name="name">The addon name.</param>
        /// <param name="index">The index of the addon, starting at 1.</param>
        /// <returns>The native memory representation of the addon, if it exists.</returns>
        public Addon.Addon GetAddonByName(string name, int index)
        {
            var addonMem = this.GetUiObjectByName(name, index);
            if (addonMem == IntPtr.Zero) return null;
            var addonStruct = Marshal.PtrToStructure<Structs.Addon>(addonMem);
            return new Addon.Addon(addonMem, addonStruct);
        }

        /// <summary>
        /// Find the agent associated with an addon, if possible.
        /// </summary>
        /// <param name="addonName">The addon name.</param>
        /// <returns>A pointer to the agent interface.</returns>
        public IntPtr FindAgentInterface(string addonName)
        {
            var addon = this.dalamud.Framework.Gui.GetUiObjectByName(addonName, 1);
            return this.FindAgentInterface(addon);
        }

        /// <summary>
        /// Find the agent associated with an addon, if possible.
        /// </summary>
        /// <param name="addon">The addon address.</param>
        /// <returns>A pointer to the agent interface.</returns>
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

            for (var i = 0; i < 380; i++)
            {
                var agent = Marshal.ReadIntPtr(agentModule, 0x20 + (i * 8));
                if (agent == IntPtr.Zero)
                    continue;
                if (Marshal.ReadInt32(agent, 0x20) == id)
                    return agent;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Set the current background music.
        /// </summary>
        /// <param name="bgmKey">The background music key.</param>
        public void SetBgm(ushort bgmKey) => this.setGlobalBgmHook.Original(bgmKey, 0, 0, 0, 0, 0);

        /// <summary>
        /// Enables the hooks and submodules of this module.
        /// </summary>
        public void Enable()
        {
            this.Chat.Enable();
            this.Toast.Enable();
            this.PartyFinder.Enable();
            this.setGlobalBgmHook.Enable();
            this.handleItemHoverHook.Enable();
            this.handleItemOutHook.Enable();
            this.toggleUiHideHook.Enable();
            this.handleActionHoverHook.Enable();
            this.handleActionOutHook.Enable();
        }

        /// <summary>
        /// Disables the hooks and submodules of this module.
        /// </summary>
        public void Dispose()
        {
            this.Chat.Dispose();
            this.Toast.Dispose();
            this.PartyFinder.Dispose();
            this.setGlobalBgmHook.Dispose();
            this.handleItemHoverHook.Dispose();
            this.handleItemOutHook.Dispose();
            this.toggleUiHideHook.Dispose();
            this.handleActionHoverHook.Dispose();
            this.handleActionOutHook.Dispose();
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

                try
                {
                    this.HoveredItemChanged?.Invoke(this, itemId);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Could not dispatch HoveredItemChanged event.");
                }

                Log.Verbose("HoverItemId:{0} this:{1}", itemId, hoverState.ToInt64().ToString("X"));
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
            try
            {
                this.HoveredActionChanged?.Invoke(this, this.HoveredAction);
            }
            catch (Exception e)
            {
                Log.Error(e, "Could not dispatch HoveredItemChanged event.");
            }

            Log.Verbose("HoverActionId: {0}/{1} this:{2}", actionKind, actionId, hoverState.ToInt64().ToString("X"));
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

        private IntPtr ToggleUiHideDetour(IntPtr thisPtr, byte unknownByte)
        {
            this.GameUiHidden = !this.GameUiHidden;

            try
            {
                this.OnUiHideToggled?.Invoke(this, this.GameUiHidden);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error on OnUiHideToggled event dispatch");
            }

            Log.Debug("UiHide toggled: {0}", this.GameUiHidden);

            return this.toggleUiHideHook.Original(thisPtr, unknownByte);
        }
    }
}
