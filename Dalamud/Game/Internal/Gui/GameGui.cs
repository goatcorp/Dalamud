using System;
using System.Runtime.InteropServices;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Serilog;
using SharpDX;

namespace Dalamud.Game.Internal.Gui {
    public sealed class GameGui : IDisposable {
        private GameGuiAddressResolver Address { get; }
        
        public ChatGui Chat { get; private set; }

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr SetGlobalBgmDelegate(UInt16 bgmKey, byte a2, UInt32 a3, UInt32 a4, UInt32 a5, byte a6);
        private readonly Hook<SetGlobalBgmDelegate> setGlobalBgmHook;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr HandleItemHoverDelegate(IntPtr hoverState, IntPtr a2, IntPtr a3, ulong a4);
        private readonly Hook<HandleItemHoverDelegate> handleItemHoverHook;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr HandleItemOutDelegate(IntPtr hoverState, IntPtr a2, IntPtr a3, ulong a4);
        private readonly Hook<HandleItemOutDelegate> handleItemOutHook;

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
        /// Event that is fired when the currently hovered item changes.
        /// </summary>
        public EventHandler<ulong> HoveredItemChanged { get; set; }

        public GameGui(IntPtr baseAddress, SigScanner scanner, Dalamud dalamud) {
            Address = new GameGuiAddressResolver(baseAddress);
            Address.Setup(scanner);

            Log.Verbose("===== G A M E G U I =====");

            Log.Verbose("GameGuiManager address {Address}", Address.BaseAddress);
            Log.Verbose("SetGlobalBgm address {Address}", Address.SetGlobalBgm);
            Log.Verbose("HandleItemHover address {Address}", Address.HandleItemHover);
            Log.Verbose("HandleItemOut address {Address}", Address.HandleItemOut);
            Log.Verbose("GetUIObject address {Address}", Address.GetUIObject);

            Chat = new ChatGui(Address.ChatManager, scanner, dalamud);

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

            this.getUIObject = Marshal.GetDelegateForFunctionPointer<GetUIObjectDelegate>(Address.GetUIObject);

            this.getMatrixSingleton =
                Marshal.GetDelegateForFunctionPointer<GetMatrixSingletonDelegate>(Address.GetMatrixSingleton);

            this.screenToWorldNative =
                Marshal.GetDelegateForFunctionPointer<ScreenToWorldNativeDelegate>(Address.ScreenToWorld);

            this.toggleUiHideHook = new Hook<ToggleUiHideDelegate>(Address.ToggleUiHide, new ToggleUiHideDelegate(ToggleUiHideDetour), this);
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
            unsafe {
                var rawMatrix = (float*) (matrixSingleton + 0x1b4).ToPointer();

                for (var i = 0; i < 16; i++, rawMatrix++)
                    viewProjectionMatrix[i] = *rawMatrix;

                width = *rawMatrix; 
                height = *(rawMatrix + 1);
            }

            Vector3.Transform( ref worldPos, ref viewProjectionMatrix, out Vector3 pCoords);

            screenPos = new Vector2(pCoords.X / pCoords.Z, pCoords.Y / pCoords.Z);

            screenPos.X = 0.5f * width * (screenPos.X + 1f);
            screenPos.Y = 0.5f * height * (1f - screenPos.Y);

            return pCoords.Z > 0;
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

            var screenPos3D = new Vector3 {
                X = screenPos.X / width * 2.0f - 1.0f,
                Y = -(screenPos.Y / height * 2.0f - 1.0f),
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

        public void SetBgm(ushort bgmKey) => this.setGlobalBgmHook.Original(bgmKey, 0, 0, 0, 0, 0); 

        public void Enable() {
            Chat.Enable();
            this.setGlobalBgmHook.Enable();
            this.handleItemHoverHook.Enable();
            this.handleItemOutHook.Enable();
            this.toggleUiHideHook.Enable();
        }

        public void Dispose() {
            Chat.Dispose();
            this.setGlobalBgmHook.Dispose();
            this.handleItemHoverHook.Dispose();
            this.handleItemOutHook.Dispose();
            this.toggleUiHideHook.Dispose();
        }
    }
}
