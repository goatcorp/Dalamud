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
        private unsafe delegate IntPtr ScreenToWorldNativeDelegate(float *camPosition, float *clipCoords, float rayDistance, float *worldCoords, float *unknown);
        private readonly ScreenToWorldNativeDelegate screenToWorldNative;

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
        }

        private IntPtr HandleSetGlobalBgmDetour(UInt16 bgmKey, byte a2, UInt32 a3, UInt32 a4, UInt32 a5, byte a6) {
            var retVal = this.setGlobalBgmHook.Original(bgmKey, a2, a3, a4, a5, a6);

            Log.Verbose("SetGlobalBgm: {0} {1} {2} {3} {4} {5} -> {6}", bgmKey, a2, a3, a4, a5, a6, retVal);

            return retVal;
        }

        private IntPtr HandleItemHoverDetour(IntPtr hoverState, IntPtr a2, IntPtr a3, ulong a4) {
            var retVal = this.handleItemHoverHook.Original(hoverState, a2, a3, a4);

            if (retVal.ToInt64() == 22) {
                var itemId = (ulong)Marshal.ReadInt32(hoverState, 0x130);
                this.HoveredItem = itemId;

                try {
                    HoveredItemChanged?.Invoke(this, itemId);
                } catch (Exception e) {
                    Log.Error(e, "Could not dispatch HoveredItemChanged event.");
                }

                Log.Verbose("HoverItemId: {0}", itemId);
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

        public bool OpenMapWithMapLink(MapLinkPayload mapLink)
        {
            var uiObjectPtr = getUIObject();

            if (uiObjectPtr.Equals(IntPtr.Zero))
            {
                Log.Error("OpenMapWithMapLink: Null pointer returned from getUIObject()");
                return false;
            }

            getUIMapObject =
                Address.GetVirtualFunction<GetUIMapObjectDelegate>(uiObjectPtr, 0, 8);


            var uiMapObjectPtr = this.getUIMapObject(uiObjectPtr);

            if (uiMapObjectPtr.Equals(IntPtr.Zero))
            {
                Log.Error("OpenMapWithMapLink: Null pointer returned from GetUIMapObject()");
                return false;
            }

            openMapWithFlag =
                Address.GetVirtualFunction<OpenMapWithFlagDelegate>(uiMapObjectPtr, 0, 63);

            var mapLinkString =
                $"m:{mapLink.TerritoryTypeId},{mapLink.MapId},{unchecked((int)mapLink.RawX)},{unchecked((int)mapLink.RawY)}";

            Log.Debug($"OpenMapWithMapLink: Opening Map Link: {mapLinkString}");

            return this.openMapWithFlag(uiMapObjectPtr, mapLinkString);
        }

        public Vector2 WorldToScreen(Vector3 worldCoords)
        {
            // Get base object with matrices
            var matrixSingleton = this.getMatrixSingleton();

            // Read current ViewProjectionMatrix plus game window size
            var viewProjectionMatrix = new Matrix();
            float width, height;
            unsafe {
                var rawMatrix = (float*) (matrixSingleton + 0x1b4).ToPointer();

                for (var i = 0; i < 16; i++, rawMatrix += 1) {
                    viewProjectionMatrix[i] = *rawMatrix;
                }

                width = *rawMatrix; 
                height = *(rawMatrix + 1);
            }

            Vector3.Transform(ref worldCoords, ref viewProjectionMatrix, out Vector3 pCoords);

            var normalProjCoords = new Vector2(pCoords.X / pCoords.Z, pCoords.Y / pCoords.Z);

            normalProjCoords.X = 0.5f * width * (normalProjCoords.X + 1f);
            normalProjCoords.Y = 0.5f * height * (1f - normalProjCoords.Y);

            return normalProjCoords;
        }

        public Vector3 ScreenToWorld(Vector2 screenCoords) {
            return new Vector3();
        }

        public void SetBgm(ushort bgmKey) => this.setGlobalBgmHook.Original(bgmKey, 0, 0, 0, 0, 0); 

        public void Enable() {
            Chat.Enable();
            this.setGlobalBgmHook.Enable();
            this.handleItemHoverHook.Enable();
            this.handleItemOutHook.Enable();
        }

        public void Dispose() {
            Chat.Dispose();
            this.setGlobalBgmHook.Dispose();
            this.handleItemHoverHook.Dispose();
            this.handleItemOutHook.Dispose();
        }
    }
}
