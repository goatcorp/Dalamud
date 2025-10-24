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
                return Song.Wanderer;

            if (this.Struct->SongFlags.HasFlag(SongFlags.ArmysPaeon))
                return Song.Army;

            if (this.Struct->SongFlags.HasFlag(SongFlags.MagesBallad))
                return Song.Mage;

            return Song.None;
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
                return Song.Wanderer;

            if (this.Struct->SongFlags.HasFlag(SongFlags.ArmysPaeonLastPlayed))
                return Song.Army;

            if (this.Struct->SongFlags.HasFlag(SongFlags.MagesBalladLastPlayed))
                return Song.Mage;

            return Song.None;
        }
    }

    /// <summary>
    /// Gets the song Coda that are currently active.
    /// </summary>
    /// <remarks>
    /// This will always return an array of size 3, inactive Coda are represented by <see cref="Enums.Song.None"/>.
    /// </remarks>
    public Song[] Coda
    {
        get
        {
            return
            [
                this.Struct->SongFlags.HasFlag(SongFlags.MagesBalladCoda) ? Song.Mage : Song.None,
                this.Struct->SongFlags.HasFlag(SongFlags.ArmysPaeonCoda) ? Song.Army : Song.None,
                this.Struct->SongFlags.HasFlag(SongFlags.WanderersMinuetCoda) ? Song.Wanderer : Song.None,
            ];
        }
    }
}
