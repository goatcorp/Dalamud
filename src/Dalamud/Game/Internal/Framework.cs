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

namespace Dalamud.Game.Internal
{
    /// <summary>
    /// This class represents the Framework of the native game client and grants access to various subsystems.
    /// </summary>
    public sealed class Framework : IDisposable
    {
        private static Stopwatch statsStopwatch = new();

        private readonly Dalamud dalamud;
        private Hook<OnUpdateDetour> updateHook;
        private Hook<OnDestroyDetour> destroyHook;
        private Hook<OnRealDestroyDelegate> realDestroyHook;

        /// <summary>
        /// Initializes a new instance of the <see cref="Framework"/> class.
        /// </summary>
        /// <param name="scanner">The SigScanner instance.</param>
        /// <param name="dalamud">The Dalamud instance.</param>
        internal Framework(SigScanner scanner, Dalamud dalamud)
        {
            this.dalamud = dalamud;
            this.Address = new FrameworkAddressResolver();
            this.Address.Setup(scanner);

            Log.Verbose($"Framework address 0x{this.Address.BaseAddress.ToInt64():X}");
            if (this.Address.BaseAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException("Framework is not initalized yet.");
            }

            // Hook virtual functions
            this.HookVTable();

            // Initialize subsystems
            this.Libc = new LibcFunction(scanner);

            this.Gui = new GameGui(this.Address.GuiManager, scanner, dalamud);

            this.Network = new GameNetwork(scanner);
        }

        /// <summary>
        /// A delegate type used with the <see cref="OnUpdateEvent"/> event.
        /// </summary>
        /// <param name="framework">The Framework instance.</param>
        public delegate void OnUpdateDelegate(Framework framework);

        /// <summary>
        /// A delegate type used during the native Framework::destroy.
        /// </summary>
        /// <param name="framework">The native Framework address.</param>
        /// <returns>A value indicating if the call was successful.</returns>
        public delegate bool OnRealDestroyDelegate(IntPtr framework);

        /// <summary>
        /// A delegate type used during the native Framework::free.
        /// </summary>
        /// <returns>The native Framework address.</returns>
        public delegate IntPtr OnDestroyDelegate();

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate bool OnUpdateDetour(IntPtr framework);

        private delegate IntPtr OnDestroyDetour(); // OnDestroyDelegate

        /// <summary>
        /// Event that gets fired every time the game framework updates.
        /// </summary>
        public event OnUpdateDelegate OnUpdateEvent;

        /// <summary>
        /// Gets or sets a value indicating whether the collection of stats is enabled.
        /// </summary>
        public static bool StatsEnabled { get; set; }

        /// <summary>
        /// Gets the stats history mapping.
        /// </summary>
        public static Dictionary<string, List<double>> StatsHistory = new();

        #region Subsystems

        /// <summary>
        /// Gets the GUI subsystem, used to access e.g. chat.
        /// </summary>
        public GameGui Gui { get; private set; }

        /// <summary>
        /// Gets the Network subsystem, used to access network data.
        /// </summary>
        public GameNetwork Network { get; private set; }

        // public ResourceManager Resource { get; private set; }

        /// <summary>
        /// Gets the Libc subsystem, used to facilitate interop with std::strings.
        /// </summary>
        public LibcFunction Libc { get; private set; }

        #endregion

        /// <summary>
        /// Gets a raw pointer to the instance of Client::Framework.
        /// </summary>
        public FrameworkAddressResolver Address { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to dispatch update events.
        /// </summary>
        internal bool DispatchUpdateEvents { get; set; } = true;

        /// <summary>
        /// Enable this module.
        /// </summary>
        public void Enable()
        {
            this.Gui.Enable();
            this.Network.Enable();

            this.updateHook.Enable();
            this.destroyHook.Enable();
            this.realDestroyHook.Enable();
        }

        /// <summary>
        /// Dispose of managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Gui.Dispose();
            this.Network.Dispose();

            this.updateHook.Disable();
            this.destroyHook.Disable();
            this.realDestroyHook.Disable();
            Thread.Sleep(500);

            this.updateHook.Dispose();
            this.destroyHook.Dispose();
            this.realDestroyHook.Dispose();
        }

        private void HookVTable()
        {
            var vtable = Marshal.ReadIntPtr(this.Address.BaseAddress);
            // Virtual function layout:
            // .rdata:00000001411F1FE0 dq offset Xiv__Framework___dtor
            // .rdata:00000001411F1FE8 dq offset Xiv__Framework__init
            // .rdata:00000001411F1FF0 dq offset Xiv__Framework__destroy
            // .rdata:00000001411F1FF8 dq offset Xiv__Framework__free
            // .rdata:00000001411F2000 dq offset Xiv__Framework__update

            var pUpdate = Marshal.ReadIntPtr(vtable, IntPtr.Size * 4);
            this.updateHook = new Hook<OnUpdateDetour>(pUpdate, this.HandleFrameworkUpdate);

            var pDestroy = Marshal.ReadIntPtr(vtable, IntPtr.Size * 3);
            this.destroyHook = new Hook<OnDestroyDetour>(pDestroy, this.HandleFrameworkDestroy);

            var pRealDestroy = Marshal.ReadIntPtr(vtable, IntPtr.Size * 2);
            this.realDestroyHook = new Hook<OnRealDestroyDelegate>(pRealDestroy, this.HandleRealDestroy);
        }

        private bool HandleFrameworkUpdate(IntPtr framework)
        {
            // If this is the first time we are running this loop, we need to init Dalamud subsystems synchronously
            if (!this.dalamud.IsReady)
                this.dalamud.LoadTier2();

            if (!this.dalamud.IsLoadedPluginSystem && this.dalamud.InterfaceManager.IsReady)
                this.dalamud.LoadTier3();

            try
            {
                this.Gui.Chat.UpdateQueue(this);
                this.Gui.Toast.UpdateQueue();
                this.Network.UpdateQueue(this);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception while handling Framework::Update hook.");
            }

            if (this.DispatchUpdateEvents)
            {
                try
                {
                    if (StatsEnabled && this.OnUpdateEvent != null)
                    {
                        // Stat Tracking for Framework Updates
                        var invokeList = this.OnUpdateEvent.GetInvocationList();
                        var notUpdated = StatsHistory.Keys.ToList();
                        // Individually invoke OnUpdate handlers and time them.
                        foreach (var d in invokeList)
                        {
                            statsStopwatch.Restart();
                            d.Method.Invoke(d.Target, new object[] { this });
                            statsStopwatch.Stop();
                            var key = $"{d.Target}::{d.Method.Name}";
                            if (notUpdated.Contains(key)) notUpdated.Remove(key);
                            if (!StatsHistory.ContainsKey(key)) StatsHistory.Add(key, new List<double>());
                            StatsHistory[key].Add(statsStopwatch.Elapsed.TotalMilliseconds);
                            if (StatsHistory[key].Count > 1000)
                            {
                                StatsHistory[key].RemoveRange(0, StatsHistory[key].Count - 1000);
                            }
                        }

                        // Cleanup handlers that are no longer being called
                        foreach (var key in notUpdated)
                        {
                            if (StatsHistory[key].Count > 0)
                            {
                                StatsHistory[key].RemoveAt(0);
                            }
                            else
                            {
                                StatsHistory.Remove(key);
                            }
                        }
                    }
                    else
                    {
                        this.OnUpdateEvent?.Invoke(this);
                    }
                }
                catch (Exception ex)
                {
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

        private IntPtr HandleFrameworkDestroy()
        {
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
