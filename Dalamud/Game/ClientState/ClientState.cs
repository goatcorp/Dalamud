using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Internal;
using Dalamud.Hooking;
using JetBrains.Annotations;
using Lumina.Excel.GeneratedSheets;
using Serilog;

namespace Dalamud.Game.ClientState
{
    /// <summary>
    /// This class represents the state of the game client at the time of access.
    /// </summary>
    public class ClientState : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// The table of all present actors.
        /// </summary>
        public readonly ActorTable Actors;

        /// <summary>
        /// Gets the language of the client.
        /// </summary>
        public readonly ClientLanguage ClientLanguage;

        /// <summary>
        /// The current Territory the player resides in.
        /// </summary>
        public ushort TerritoryType;

        /// <summary>
        /// The class facilitating Job Gauge data access.
        /// </summary>
        public JobGauges JobGauges;

        /// <summary>
        /// The class facilitating party list data access.
        /// </summary>
        public PartyList PartyList;

        /// <summary>
        /// Provides access to the keypress state of keyboard keys in game.
        /// </summary>
        public KeyState KeyState;

        /// <summary>
        /// Provides access to the button state of gamepad buttons in game.
        /// </summary>
        public GamepadState GamepadState;

        /// <summary>
        /// Provides access to client conditions/player state. Allows you to check if a player is in a duty, mounted, etc.
        /// </summary>
        public Condition Condition;

        /// <summary>
        /// The class facilitating target data access.
        /// </summary>
        public Targets Targets;

        /// <summary>
        /// Event that gets fired when the current Territory changes.
        /// </summary>
        public EventHandler<ushort> TerritoryChanged;

        private readonly Dalamud dalamud;
        private readonly ClientStateAddressResolver address;
        private readonly Hook<SetupTerritoryTypeDelegate> setupTerritoryTypeHook;

        private bool lastConditionNone = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientState"/> class.
        /// Set up client state access.
        /// </summary>
        /// <param name="dalamud">Dalamud instance.</param>
        /// <param name="startInfo">StartInfo of the current Dalamud launch.</param>
        /// <param name="scanner">Sig scanner.</param>
        public ClientState(Dalamud dalamud, DalamudStartInfo startInfo, SigScanner scanner)
        {
            this.dalamud = dalamud;
            this.address = new ClientStateAddressResolver();
            this.address.Setup(scanner);

            Log.Verbose("===== C L I E N T  S T A T E =====");

            this.ClientLanguage = startInfo.Language;

            this.Actors = new ActorTable(dalamud, this.address);

            this.PartyList = new PartyList(dalamud, this.address);

            this.JobGauges = new JobGauges(this.address);

            this.KeyState = new KeyState(this.address, scanner.Module.BaseAddress);

            this.GamepadState = new GamepadState(this.address);

            this.Condition = new Condition(this.address);

            this.Targets = new Targets(dalamud, this.address);

            Log.Verbose("SetupTerritoryType address {SetupTerritoryType}", this.address.SetupTerritoryType);

            this.setupTerritoryTypeHook = new Hook<SetupTerritoryTypeDelegate>(this.address.SetupTerritoryType, this.SetupTerritoryTypeDetour);

            dalamud.Framework.OnUpdateEvent += this.FrameworkOnOnUpdateEvent;
            dalamud.NetworkHandlers.CfPop += this.NetworkHandlersOnCfPop;
        }

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr SetupTerritoryTypeDelegate(IntPtr manager, ushort terriType);

        /// <summary>
        /// Event that fires when a property changes.
        /// </summary>
#pragma warning disable CS0067
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore

        /// <summary>
        /// Event that fires when a character is logging in.
        /// </summary>
        public event EventHandler OnLogin;

        /// <summary>
        /// Event that fires when a character is logging out.
        /// </summary>
        public event EventHandler OnLogout;

        /// <summary>
        /// Event that gets fired when a duty is ready.
        /// </summary>
        public event EventHandler<ContentFinderCondition> CfPop;

        /// <summary>
        /// Gets the local player character, if one is present.
        /// </summary>
        [CanBeNull]
        public PlayerCharacter LocalPlayer
        {
            get
            {
                var actor = this.Actors[0];

                if (actor is PlayerCharacter pc)
                    return pc;

                return null;
            }
        }

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
            this.GamepadState.Enable();
            this.PartyList.Enable();
            this.setupTerritoryTypeHook.Enable();
        }

        /// <summary>
        /// Dispose of managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.PartyList.Dispose();
            this.setupTerritoryTypeHook.Dispose();
            this.Actors.Dispose();
            this.GamepadState.Dispose();

            this.dalamud.Framework.OnUpdateEvent -= this.FrameworkOnOnUpdateEvent;
            this.dalamud.NetworkHandlers.CfPop += this.NetworkHandlersOnCfPop;
        }

        private IntPtr SetupTerritoryTypeDetour(IntPtr manager, ushort terriType)
        {
            this.TerritoryType = terriType;
            this.TerritoryChanged?.Invoke(this, terriType);

            Log.Debug("TerritoryType changed: {0}", terriType);

            return this.setupTerritoryTypeHook.Original(manager, terriType);
        }

        private void NetworkHandlersOnCfPop(object sender, ContentFinderCondition e)
        {
            this.CfPop?.Invoke(this, e);
        }

        private void FrameworkOnOnUpdateEvent(Framework framework)
        {
            if (this.Condition.Any() && this.lastConditionNone == true)
            {
                Log.Debug("Is login");
                this.lastConditionNone = false;
                this.IsLoggedIn = true;
                this.OnLogin?.Invoke(this, null);
            }

            if (!this.Condition.Any() && this.lastConditionNone == false)
            {
                Log.Debug("Is logout");
                this.lastConditionNone = true;
                this.IsLoggedIn = false;
                this.OnLogout?.Invoke(this, null);
            }
        }
    }
}
