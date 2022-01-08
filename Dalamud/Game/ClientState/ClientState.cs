using System;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Aetherytes;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Network.Internal;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Utility;
using Serilog;

namespace Dalamud.Game.ClientState
{
    /// <summary>
    /// This class represents the state of the game client at the time of access.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public sealed class ClientState : IDisposable
    {
        private readonly ClientStateAddressResolver address;
        private readonly Hook<SetupTerritoryTypeDelegate> setupTerritoryTypeHook;

        private bool lastConditionNone = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientState"/> class.
        /// Set up client state access.
        /// </summary>
        internal ClientState()
        {
            this.address = new ClientStateAddressResolver();
            this.address.Setup();

            Log.Verbose("===== C L I E N T  S T A T E =====");

            this.ClientLanguage = Service<DalamudStartInfo>.Get().Language;

            Service<ObjectTable>.Set(this.address);

            Service<FateTable>.Set(this.address);

            Service<PartyList>.Set(this.address);

            Service<BuddyList>.Set(this.address);

            Service<JobGauges>.Set(this.address);

            Service<KeyState>.Set(this.address);

            Service<GamepadState>.Set(this.address);

            Service<Condition>.Set(this.address);

            Service<TargetManager>.Set(this.address);

            Service<AetheryteList>.Set(this.address);

            Log.Verbose($"SetupTerritoryType address 0x{this.address.SetupTerritoryType.ToInt64():X}");

            this.setupTerritoryTypeHook = new Hook<SetupTerritoryTypeDelegate>(this.address.SetupTerritoryType, this.SetupTerritoryTypeDetour);

            var framework = Service<Framework>.Get();
            framework.Update += this.FrameworkOnOnUpdateEvent;

            var networkHandlers = Service<NetworkHandlers>.Get();
            networkHandlers.CfPop += this.NetworkHandlersOnCfPop;
        }

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr SetupTerritoryTypeDelegate(IntPtr manager, ushort terriType);

        /// <summary>
        /// Event that gets fired when the current Territory changes.
        /// </summary>
        public event EventHandler<ushort> TerritoryChanged;

        /// <summary>
        /// Event that fires when a character is logging in.
        /// </summary>
        public event EventHandler Login;

        /// <summary>
        /// Event that fires when a character is logging out.
        /// </summary>
        public event EventHandler Logout;

        /// <summary>
        /// Event that gets fired when a duty is ready.
        /// </summary>
        public event EventHandler<Lumina.Excel.GeneratedSheets.ContentFinderCondition> CfPop;

        /// <summary>
        /// Gets the language of the client.
        /// </summary>
        public ClientLanguage ClientLanguage { get; }

        /// <summary>
        /// Gets the current Territory the player resides in.
        /// </summary>
        public ushort TerritoryType { get; private set; }

        /// <summary>
        /// Gets the local player character, if one is present.
        /// </summary>
        public PlayerCharacter? LocalPlayer => Service<ObjectTable>.Get()[0] as PlayerCharacter;

        /// <summary>
        /// Gets the content ID of the local character.
        /// </summary>
        public ulong LocalContentId => (ulong)Marshal.ReadInt64(this.address.LocalContentId);

        /// <summary>
        /// Gets a value indicating whether a character is logged in.
        /// </summary>
        public bool IsLoggedIn { get; private set; }

        /// <summary>
        /// Enable this module.
        /// </summary>
        public void Enable()
        {
            Service<Condition>.Get().Enable();
            Service<GamepadState>.Get().Enable();
            this.setupTerritoryTypeHook.Enable();
        }

        /// <summary>
        /// Dispose of managed and unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.setupTerritoryTypeHook.Dispose();
            Service<Condition>.Get().ExplicitDispose();
            Service<GamepadState>.Get().ExplicitDispose();
            Service<Framework>.Get().Update -= this.FrameworkOnOnUpdateEvent;
            Service<NetworkHandlers>.Get().CfPop -= this.NetworkHandlersOnCfPop;
        }

        private IntPtr SetupTerritoryTypeDetour(IntPtr manager, ushort terriType)
        {
            this.TerritoryType = terriType;
            this.TerritoryChanged?.Invoke(this, terriType);

            Log.Debug("TerritoryType changed: {0}", terriType);

            return this.setupTerritoryTypeHook.Original(manager, terriType);
        }

        private void NetworkHandlersOnCfPop(object sender, Lumina.Excel.GeneratedSheets.ContentFinderCondition e)
        {
            this.CfPop?.Invoke(this, e);
        }

        private void FrameworkOnOnUpdateEvent(Framework framework)
        {
            var condition = Service<Condition>.Get();
            if (condition.Any() && this.lastConditionNone == true)
            {
                Log.Debug("Is login");
                this.lastConditionNone = false;
                this.IsLoggedIn = true;
                this.Login?.Invoke(this, null);
            }

            if (!condition.Any() && this.lastConditionNone == false)
            {
                Log.Debug("Is logout");
                this.lastConditionNone = true;
                this.IsLoggedIn = false;
                this.Logout?.Invoke(this, null);
            }
        }
    }
}
