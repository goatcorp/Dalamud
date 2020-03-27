using System.ComponentModel;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Internal;
using Serilog;

namespace Dalamud.Game.ClientState
{
    /// <summary>
    /// This class represents the state of the game client at the time of access.
    /// </summary>
    public class ClientState : INotifyPropertyChanged {
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

        /// <summary>
        /// The current Territory the player resides in.
        /// </summary>
        public uint TerritoryType => (uint) Marshal.ReadInt32(Address.TerritoryType);

        /// <summary>
        /// The content ID of the local character.
        /// </summary>
        public ulong LocalContentId => (ulong) Marshal.ReadInt64(Address.LocalContentId);

        /// <summary>
        /// The class facilitating Job Gauge data access
        /// </summary>
        public JobGauges JobGauges;

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

            Log.Verbose("TerritoryType address {TerritoryType}", Address.TerritoryType);

            dalamud.Framework.OnUpdateEvent += FrameworkOnOnUpdateEvent;
        }

        private void FrameworkOnOnUpdateEvent(Framework framework) {
            // ignored
        }
    }
}
