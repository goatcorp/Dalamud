using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Internal;
using Dalamud.Game.Internal.Network;
using Dalamud.Hooking;
using Lumina.Excel.GeneratedSheets;
using Serilog;

namespace Dalamud.Game.ClientState
{
    /// <summary>
    /// This class represents the state of the game client at the time of access.
    /// </summary>
    public class ClientState : INotifyPropertyChanged, IDisposable {
        public event PropertyChangedEventHandler PropertyChanged;

        private ClientStateAddressResolver Address { get; }

        public readonly ClientLanguage ClientLanguage;

        /// <summary>
        /// The table of all present actors.
        /// </summary>
        public readonly ActorTable Actors;

        /// <summary>
        /// The local player character, if one is present.
        /// </summary>
        public PlayerCharacter LocalPlayer {
            get {
                var actor = this.Actors[0];

                if (actor is PlayerCharacter pc)
                    return pc;

                return null;
            }
        }

        #region TerritoryType

        // TODO: The hooking logic for this should go into a separate class.
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr SetupTerritoryTypeDelegate(IntPtr manager, ushort terriType);

        private readonly Hook<SetupTerritoryTypeDelegate> setupTerritoryTypeHook;

        /// <summary>
        /// The current Territory the player resides in.
        /// </summary>
        public ushort TerritoryType;

        /// <summary>
        /// Event that gets fired when the current Territory changes.
        /// </summary>
        public EventHandler<ushort> TerritoryChanged;

        private IntPtr SetupTerritoryTypeDetour(IntPtr manager, ushort terriType)
        {
            this.TerritoryType = terriType;
            this.TerritoryChanged?.Invoke(this, terriType);

            Log.Debug("TerritoryType changed: {0}", terriType);

            return this.setupTerritoryTypeHook.Original(manager, terriType);
        }

        #endregion

        /// <summary>
        /// The content ID of the local character.
        /// </summary>
        public ulong LocalContentId => (ulong) Marshal.ReadInt64(Address.LocalContentId);

        /// <summary>
        /// The class facilitating Job Gauge data access
        /// </summary>
        public JobGauges JobGauges;

        /// <summary>
        /// Provides access to the keypress state of keyboard keys in game.
        /// </summary>
        public KeyState KeyState;

        /// <summary>
        /// Set up client state access.
        /// </summary>
        /// <param name="dalamud">Dalamud instance</param>
        /// /// <param name="startInfo">StartInfo of the current Dalamud launch</param>
        /// <param name="scanner">Sig scanner</param>
        public ClientState(Dalamud dalamud, DalamudStartInfo startInfo, SigScanner scanner) {
            Address = new ClientStateAddressResolver();
            Address.Setup(scanner);

            Log.Verbose("===== C L I E N T  S T A T E =====");

            this.ClientLanguage = startInfo.Language;

            this.Actors = new ActorTable(dalamud, Address);

            this.JobGauges = new JobGauges(Address);

            this.KeyState = new KeyState(Address, scanner.Module.BaseAddress);

            Log.Verbose("SetupTerritoryType address {SetupTerritoryType}", Address.SetupTerritoryType);

            this.setupTerritoryTypeHook = new Hook<SetupTerritoryTypeDelegate>(Address.SetupTerritoryType,
                                                                               new SetupTerritoryTypeDelegate(SetupTerritoryTypeDetour),
                                                                               this);

            dalamud.Framework.OnUpdateEvent += FrameworkOnOnUpdateEvent;
        }

        public void Enable() {
            this.Actors.Enable();
            this.setupTerritoryTypeHook.Enable();
        }

        public void Dispose() {
            this.Actors.Dispose();
            this.setupTerritoryTypeHook.Dispose();
        }

        private void FrameworkOnOnUpdateEvent(Framework framework) {
            // ignored
        }
    }
}
