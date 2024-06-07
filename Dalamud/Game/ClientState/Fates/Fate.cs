using System.Numerics;

using Dalamud.Data;
using Dalamud.Game.ClientState.Resolvers;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

namespace Dalamud.Game.ClientState.Fates;

/// <summary>
/// This class represents an FFXIV Fate.
/// </summary>
public unsafe partial class Fate : IEquatable<Fate>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Fate"/> class.
    /// </summary>
    /// <param name="address">The address of this fate in memory.</param>
    internal Fate(IntPtr address)
    {
        this.Address = address;
    }

    /// <summary>
    /// Gets the address of this Fate in memory.
    /// </summary>
    public IntPtr Address { get; }

    private FFXIVClientStructs.FFXIV.Client.Game.Fate.FateContext* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Fate.FateContext*)this.Address;

    public static bool operator ==(Fate fate1, Fate fate2)
    {
        if (fate1 is null || fate2 is null)
            return Equals(fate1, fate2);

        return fate1.Equals(fate2);
    }

    public static bool operator !=(Fate fate1, Fate fate2) => !(fate1 == fate2);

    /// <summary>
    /// Gets a value indicating whether this Fate is still valid in memory.
    /// </summary>
    /// <param name="fate">The fate to check.</param>
    /// <returns>True or false.</returns>
    public static bool IsValid(Fate fate)
    {
        var clientState = Service<ClientState>.GetNullable();

        if (fate == null || clientState == null)
            return false;

        if (clientState.LocalContentId == 0)
            return false;

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether this actor is still valid in memory.
    /// </summary>
    /// <returns>True or false.</returns>
    public bool IsValid() => IsValid(this);

    /// <inheritdoc/>
    bool IEquatable<Fate>.Equals(Fate other) => this.FateId == other?.FateId;

    /// <inheritdoc/>
    public override bool Equals(object obj) => ((IEquatable<Fate>)this).Equals(obj as Fate);

    /// <inheritdoc/>
    public override int GetHashCode() => this.FateId.GetHashCode();
}

/// <summary>
/// This class represents an FFXIV Fate.
/// </summary>
public unsafe partial class Fate
{
    /// <summary>
    /// Gets the Fate ID of this <see cref="Fate" />.
    /// </summary>
    public ushort FateId => this.Struct->FateId;

    /// <summary>
    /// Gets game data linked to this Fate.
    /// </summary>
    public Lumina.Excel.GeneratedSheets.Fate GameData => Service<DataManager>.Get().GetExcelSheet<Lumina.Excel.GeneratedSheets.Fate>().GetRow(this.FateId);

    /// <summary>
    /// Gets the time this <see cref="Fate"/> started.
    /// </summary>
    public int StartTimeEpoch => this.Struct->StartTimeEpoch;

    /// <summary>
    /// Gets how long this <see cref="Fate"/> will run.
    /// </summary>
    public short Duration => this.Struct->Duration;

    /// <summary>
    /// Gets the remaining time in seconds for this <see cref="Fate"/>.
    /// </summary>
    public long TimeRemaining => this.StartTimeEpoch + this.Duration - DateTimeOffset.Now.ToUnixTimeSeconds();

    /// <summary>
    /// Gets the displayname of this <see cref="Fate" />.
    /// </summary>
    public SeString Name => MemoryHelper.ReadSeString(&this.Struct->Name);

    /// <summary>
    /// Gets the state of this <see cref="Fate"/> (Running, Ended, Failed, Preparation, WaitingForEnd).
    /// </summary>
    public FateState State => (FateState)this.Struct->State;

    /// <summary>
    /// Gets the progress amount of this <see cref="Fate"/>.
    /// </summary>
    public byte Progress => this.Struct->Progress;

    /// <summary>
    /// Gets the level of this <see cref="Fate"/>.
    /// </summary>
    public byte Level => this.Struct->Level;

    /// <summary>
    /// Gets the position of this <see cref="Fate"/>.
    /// </summary>
    public Vector3 Position => this.Struct->Location;

    /// <summary>
    /// Gets the territory this <see cref="Fate"/> is located in.
    /// </summary>
    public ExcelResolver<Lumina.Excel.GeneratedSheets.TerritoryType> TerritoryType => new(this.Struct->TerritoryId);
}
