using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Dalamud.Game.Internal.Gui;
using Dalamud.Game.Internal.Libc;
using Dalamud.Game.Internal.Network;
using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Game.Internal {
    /// <summary>
    /// This class represents the Framework of the native game client and grants access to various subsystems.
    /// </summary>
    public sealed class Framework : IDisposable {
        private readonly Dalamud dalamud;

        internal bool DispatchUpdateEvents { get; set; } = true;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate bool OnUpdateDetour(IntPtr framework);

        private delegate IntPtr OnDestroyDetour();

        public delegate void OnUpdateDelegate(Framework framework);

        public delegate IntPtr OnDestroyDelegate();

        public delegate bool OnRealDestroyDelegate(IntPtr framework);

        /// <summary>
        /// Event that gets fired every time the game framework updates.
        /// </summary>
        public event OnUpdateDelegate OnUpdateEvent;
        
        private Hook<OnUpdateDetour> updateHook;

        private Hook<OnDestroyDetour> destroyHook;

        private Hook<OnRealDestroyDelegate> realDestroyHook;
        
        /// <summary>
        /// A raw pointer to the instance of Client::Framework
        /// </summary>
        public FrameworkAddressResolver Address { get; }

        #region Stats
        public static bool StatsEnabled { get; set; }
        public static Dictionary<string, List<double>> StatsHistory = new Dictionary<string, List<double>>();
        private static Stopwatch statsStopwatch = new Stopwatch();
#endregion
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
        }

        private void HookVTable() {
            var vtable = Marshal.ReadIntPtr(Address.BaseAddress);
            // Virtual function layout:
            // .rdata:00000001411F1FE0 dq offset Xiv__Framework___dtor
            // .rdata:00000001411F1FE8 dq offset Xiv__Framework__init
            // .rdata:00000001411F1FF0 dq offset Xiv__Framework__destroy
            // .rdata:00000001411F1FF8 dq offset Xiv__Framework__free
            // .rdata:00000001411F2000 dq offset Xiv__Framework__update

            var pUpdate = Marshal.ReadIntPtr(vtable, IntPtr.Size * 4);
            this.updateHook = new Hook<OnUpdateDetour>(pUpdate, new OnUpdateDetour(HandleFrameworkUpdate), this);

            var pDestroy = Marshal.ReadIntPtr(vtable, IntPtr.Size * 3);
            this.destroyHook =
                new Hook<OnDestroyDetour>(pDestroy, new OnDestroyDelegate(HandleFrameworkDestroy), this);

            var pRealDestroy = Marshal.ReadIntPtr(vtable, IntPtr.Size * 2);
            this.realDestroyHook =
                new Hook<OnRealDestroyDelegate>(pRealDestroy, new OnRealDestroyDelegate(HandleRealDestroy), this);
        }
        
        public void Enable() {
            Gui.Enable();
            Network.Enable();
            
            this.updateHook.Enable();
            this.destroyHook.Enable();
            this.realDestroyHook.Enable();
        }
        
        public void Dispose() {
            Gui.Dispose();
            Network.Dispose();

            this.updateHook.Dispose();
            this.destroyHook.Dispose();
            this.realDestroyHook.Dispose();
        }

        private bool HandleFrameworkUpdate(IntPtr framework) {
            // If this is the first time we are running this loop, we need to init Dalamud subsystems synchronously
            if (!this.dalamud.IsReady)
                this.dalamud.LoadTier2();

            if (!this.dalamud.IsLoadedPluginSystem && this.dalamud.InterfaceManager.IsReady)
                this.dalamud.LoadTier3();

            try {
                Gui.Chat.UpdateQueue(this);
                Gui.Toast.UpdateQueue();
                Network.UpdateQueue(this);
            } catch (Exception ex) {
                Log.Error(ex, "Exception while handling Framework::Update hook.");
            }

            if (this.DispatchUpdateEvents)
            {
                try {
                    if (StatsEnabled && OnUpdateEvent != null) {
                        // Stat Tracking for Framework Updates
                        var invokeList = OnUpdateEvent.GetInvocationList();
                        var notUpdated = StatsHistory.Keys.ToList();
                        // Individually invoke OnUpdate handlers and time them.
                        foreach (var d in invokeList) {
                            statsStopwatch.Restart();
                            d.Method.Invoke(d.Target, new object[]{ this });
                            statsStopwatch.Stop();
                            var key = $"{d.Target}::{d.Method.Name}";
                            if (notUpdated.Contains(key)) notUpdated.Remove(key);
                            if (!StatsHistory.ContainsKey(key)) StatsHistory.Add(key, new List<double>());
                            StatsHistory[key].Add(statsStopwatch.Elapsed.TotalMilliseconds);
                            if (StatsHistory[key].Count > 1000) {
                                StatsHistory[key].RemoveRange(0, StatsHistory[key].Count - 1000);
                            }
                        }

                        // Cleanup handlers that are no longer being called
                        foreach (var key in notUpdated) {
                            if (StatsHistory[key].Count > 0) {
                                StatsHistory[key].RemoveAt(0);
                            } else {
                                StatsHistory.Remove(key);
                            }
                        }
                    } else {
                        OnUpdateEvent?.Invoke(this);
                    }
                } catch (Exception ex) {
                    Log.Error(ex, "Exception while dispatching Framework::Update event.");
                }
            }

            return this.updateHook.Original(framework);
        }

        private bool HandleRealDestroy(IntPtr framework)
        {
            if (this.DispatchUpdateEvents)
            {
                Log.Information("Framework::Destroy!");
                this.dalamud.DisposePlugins();
                Log.Information("Framework::Destroy OK!");
            }

            this.DispatchUpdateEvents = false;

            return this.realDestroyHook.Original(framework);
        }

        private IntPtr HandleFrameworkDestroy() {
            Log.Information("Framework::Free!");

            // Store the pointer to the original trampoline location
            var originalPtr = Marshal.GetFunctionPointerForDelegate(this.destroyHook.Original);

            this.dalamud.Unload();

            this.dalamud.WaitForUnloadFinish();

            Log.Information("Framework::Free OK!");

            // Return the original trampoline location to cleanly exit
            return originalPtr;
        }
    }
}
