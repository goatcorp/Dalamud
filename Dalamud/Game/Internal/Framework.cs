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

        internal bool HasUnloaded { get; set; } = false;

        public delegate void OnUpdateDelegate(Framework framework);

        /// <summary>
        /// Event that gets fired every time the game framework updates.
        /// </summary>
        public event OnUpdateDelegate OnUpdateEvent;

        private delegate byte OnUpdatePrototype(IntPtr framework);

        private delegate byte OnTryQuitPrototype(IntPtr framework);

        private Hook<OnUpdatePrototype> updateHook;

        private Hook<OnTryQuitPrototype> tryQuitHook;
        
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
            this.updateHook = new Hook<OnUpdatePrototype>(pUpdate, new OnUpdatePrototype(HandleFrameworkUpdate), this);

            var pDestroy = Marshal.ReadIntPtr(vtable, IntPtr.Size * 2);
            this.tryQuitHook =
                new Hook<OnTryQuitPrototype>(pDestroy, new OnTryQuitPrototype(HandleFrameworkTryQuit), this);
        }
        
        public void Enable() {
            Gui.Enable();
            Network.Enable();
            
            this.updateHook.Enable();
            this.tryQuitHook.Enable();
        }
        
        public void Dispose() {
            Gui.Dispose();
            Network.Dispose();

            this.updateHook.Dispose();
        }

        private byte HandleFrameworkUpdate(IntPtr framework) {
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

        // this function can get called multiple times if it ever returns false so lets pretend we're being safe
        private byte HandleFrameworkTryQuit(IntPtr framework) {
            Log.Information("Framework::TryQuit!");

            if (!this.HasUnloaded)
            {
                this.DispatchUpdateEvents = false;

                this.dalamud.Unload();

                this.dalamud.WaitForUnloadFinish();

                this.HasUnloaded = true;
            }

            var retVal = this.tryQuitHook.Original(framework);

            if (retVal == 1)
                this.tryQuitHook.Dispose();

            return retVal;
        }
    }
}
