using System;
using Dalamud.Game.ClientState.Actors;

namespace Dalamud.Game.ClientState.Fates
{
    public class Fate : IEquatable<Fate>
    {
        /// <summary>
        /// The memory representation of the base fate.
        /// </summary>
        protected Structs.Fate fateStruct;

        /// <summary>
        /// The address of this fate in memory.
        /// </summary>
        public readonly IntPtr Address;

        /// <summary>
        ///     Initialize a representation of a basic FFXIV fate.
        /// </summary>
        /// <param name="fateStruct">The memory representation of the base fate.</param>
        /// <param name="address">The address of this fate in memory.</param>
        public Fate(IntPtr address, Structs.Fate fateStruct)
        {
            this.fateStruct = fateStruct;
            this.Address = address;
        }

        /// <summary>
        /// Fate ID of this <see cref="Fate" />.
        /// </summary>
        public int FateId => this.fateStruct.FateId;

        /// <summary>
        /// Display name of this <see cref="Fate" />.
        /// </summary>
        public string Name => this.fateStruct.Name;

        /// <summary>
        /// State of this <see cref="Fate" /> (Running, Ended, Failed, Preparation, WaitingForEnd).
        /// </summary>
        public FateState State => this.fateStruct.State;

        /// <summary>
        /// Time this <see cref="Fate" /> started.
        /// </summary>
        public int StartTimeEpoch => this.fateStruct.StartTimeEpoch;

        /// <summary>
        /// How long this <see cref="Fate" /> will run.
        /// </summary>
        public short Duration => this.fateStruct.Duration;

        /// <summary>
        /// Remaining time in seconds for this <see cref="Fate" />.
        /// </summary>
        public long TimeRemaining => (StartTimeEpoch + Duration) - DateTimeOffset.Now.ToUnixTimeSeconds();

        /// <summary>
        /// Progress amount of this <see cref="Fate" />.
        /// </summary>
        public byte Progress => this.fateStruct.Progress;

        /// <summary>
        /// Level of this <see cref="Fate" />.
        /// </summary>
        public byte Level => this.fateStruct.Level;

        /// <summary>
        /// Territory this <see cref="Fate" /> is located.
        /// </summary>
        public short Territory => this.fateStruct.Territory;

        /// <summary>
        /// Position of this <see cref="Fate" />.
        /// </summary>
        public Position3 Position => this.fateStruct.Position;

        public bool Equals(Fate other)
        {
            if (other is null)
                return false;

            return this.FateId == other.FateId;
        }

        public override bool Equals(object obj) => Equals(obj as Fate);
        public override int GetHashCode() => (this.FateId).GetHashCode();

        public static bool operator ==(Fate fate1, Fate fate2)
        {
            if (((object)fate1) == null || ((object)fate2) == null)
                return Object.Equals(fate1, fate2);

            return fate1.Equals(fate2);
        }

        public static bool operator !=(Fate fate1, Fate fate2)
        {
            if (((object)fate1) == null || ((object)fate2) == null)
                return !Object.Equals(fate1, fate2);

            return !fate1.Equals(fate2);
        }
    }
}
