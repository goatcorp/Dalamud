using System;
using System.Runtime.InteropServices;
using Dalamud.Game.Internal.Gui;
using Dalamud.Game.Internal.Libc;
using Dalamud.Game.Internal.Network;
using Dalamud.Hooking;
using Serilog;
using Dalamud.Game.Internal.File;

namespace Dalamud.Game.Internal {
    /// <summary>
    /// This class represents the Framework of the native game client and grants access to various subsystems.
    /// </summary>
    public sealed class Framework : IDisposable {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate bool OnUpdateDetour(IntPtr framework);

        public delegate void OnUpdateDelegate(Framework framework);

        /// <summary>
        /// Event that gets fired every time the game framework updates.
        /// </summary>
        public event OnUpdateDelegate OnUpdateEvent;
        
        private Hook<OnUpdateDetour> updateHook;
        private Dalamud dalamud;
        
        
        /// <summary>
        /// A raw pointer to the instance of Client::Framework
        /// </summary>
        public FrameworkAddressResolver Address { get; }
        
#region Subsystems

        /// <summary>
        /// The GUI subsystem, used to access e.g. chat.
        /// </summary>
        public GameGui Gui { get; private set; }

        /// <summary>
        /// The Network subsystem, used to access network data.
        /// </summary>
        public GameNetwork Network { get; private set; }

        //public ResourceManager Resource { get; private set; }
        
        public LibcFunction Libc { get; private set; }

        #endregion
        
        public Framework(SigScanner scanner, Dalamud dalamud) {
            this.dalamud = dalamud;

            Address = new FrameworkAddressResolver();
            Address.Setup(scanner);
            
            Log.Verbose("Framework address {FrameworkAddress}", Address.BaseAddress);
            if (Address.BaseAddress == IntPtr.Zero) {
                throw new InvalidOperationException("Framework is not initalized yet.");
            }
            
            // Hook virtual functions
            HookVTable();
            
            // Initialize subsystems
            Libc = new LibcFunction(scanner);
            
            Gui = new GameGui(Address.GuiManager, scanner, dalamud);

            Network = new GameNetwork(scanner);

            //Resource = new ResourceManager(dalamud, scanner);
        }

        private void HookVTable() {
            var vtable = Marshal.ReadIntPtr(Address.BaseAddress);
            // Virtual function layout:
            // .rdata:00000001411F1FE0 dq offset Xiv__Framework___dtor
            // .rdata:00000001411F1FE8 dq offset Xiv__Framework__init
            // .rdata:00000001411F1FF0 dq offset sub_1400936E0
            // .rdata:00000001411F1FF8 dq offset sub_1400939E0
            // .rdata:00000001411F2000 dq offset Xiv__Framework__update

            var pUpdate = Marshal.ReadIntPtr(vtable, IntPtr.Size * 4);
            this.updateHook = new Hook<OnUpdateDetour>(pUpdate, new OnUpdateDetour(HandleFrameworkUpdate), this);
        }
        
        public void Enable() {
            Gui.Enable();
            Network.Enable();
            //Resource.Enable();
            
            this.updateHook.Enable();
        }
        
        public void Dispose() {
            Gui.Dispose();
            Network.Dispose();
            //Resource.Dispose();
            
            this.updateHook.Dispose();
        }

        private bool HandleFrameworkUpdate(IntPtr framework) {
            try {
                dalamud.ClientState.Actors.ResetCache();
                Gui.Chat.UpdateQueue(this);
                Network.UpdateQueue(this);
            } catch (Exception ex) {
                Log.Error(ex, "Exception while handling Framework::Update hook.");
            }
            
            try {
                OnUpdateEvent?.Invoke(this);
            } catch (Exception ex) {
                Log.Error(ex, "Exception while dispatching Framework::Update event.");
            }

            return this.updateHook.Original(framework);
        }
    }
}
