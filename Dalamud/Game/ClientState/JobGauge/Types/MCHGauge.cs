namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory MCH job gauge.
/// </summary>
public unsafe class MCHGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.MachinistGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MCHGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal MCHGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the time time remaining for Overheat in milliseconds.
    /// </summary>
    public short OverheatTimeRemaining => this.Struct->OverheatTimeRemaining;

    /// <summary>
    /// Gets the time remaining for the Rook or Queen in milliseconds.
    /// </summary>
    public short SummonTimeRemaining => this.Struct->SummonTimeRemaining;

    /// <summary>
    /// Gets the current Heat level.
    /// </summary>
    public byte Heat => this.Struct->Heat;

    /// <summary>
    /// Gets the current Battery level.
    /// </summary>
    public byte Battery => this.Struct->Battery;

    /// <summary>
    /// Gets the battery level of the last summon (robot).
    /// </summary>
    public byte LastSummonBatteryPower => this.Struct->LastSummonBatteryPower;

    /// <summary>
    /// Gets a value indicating whether the player is currently Overheated.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsOverheated => (this.Struct->TimerActive & 1) != 0;

    /// <summary>
    /// Gets a value indicating whether the player has an active Robot.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsRobotActive => (this.Struct->TimerActive & 2) != 0;
}
