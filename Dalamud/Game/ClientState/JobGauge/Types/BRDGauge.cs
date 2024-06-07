using Dalamud.Game.ClientState.JobGauge.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Gauge;

namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory BRD job gauge.
/// </summary>
public unsafe class BRDGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.BardGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BRDGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal BRDGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the current song timer in milliseconds.
    /// </summary>
    public ushort SongTimer => this.Struct->SongTimer;

    /// <summary>
    /// Gets the amount of Repertoire accumulated.
    /// </summary>
    public byte Repertoire => this.Struct->Repertoire;

    /// <summary>
    /// Gets the amount of Soul Voice accumulated.
    /// </summary>
    public byte SoulVoice => this.Struct->SoulVoice;

    /// <summary>
    /// Gets the type of song that is active.
    /// </summary>
    public Song Song
    {
        get
        {
            if (this.Struct->SongFlags.HasFlag(SongFlags.WanderersMinuet))
                return Song.WANDERER;

            if (this.Struct->SongFlags.HasFlag(SongFlags.ArmysPaeon))
                return Song.ARMY;

            if (this.Struct->SongFlags.HasFlag(SongFlags.MagesBallad))
                return Song.MAGE;

            return Song.NONE;
        }
    }

    /// <summary>
    /// Gets the type of song that was last played.
    /// </summary>
    public Song LastSong
    {
        get
        {
            if (this.Struct->SongFlags.HasFlag(SongFlags.WanderersMinuetLastPlayed))
                return Song.WANDERER;

            if (this.Struct->SongFlags.HasFlag(SongFlags.ArmysPaeonLastPlayed))
                return Song.ARMY;

            if (this.Struct->SongFlags.HasFlag(SongFlags.MagesBalladLastPlayed))
                return Song.MAGE;

            return Song.NONE;
        }
    }

    /// <summary>
    /// Gets the song Coda that are currently active.
    /// </summary>
    /// <remarks>
    /// This will always return an array of size 3, inactive Coda are represented by <see cref="Song.NONE"/>.
    /// </remarks>
    public Song[] Coda
    {
        get
        {
            return new[]
            {
                this.Struct->SongFlags.HasFlag(SongFlags.MagesBalladCoda) ? Song.MAGE : Song.NONE,
                this.Struct->SongFlags.HasFlag(SongFlags.ArmysPaeonCoda) ? Song.ARMY : Song.NONE,
                this.Struct->SongFlags.HasFlag(SongFlags.WanderersMinuetCoda) ? Song.WANDERER : Song.NONE,
            };
        }
    }
}
