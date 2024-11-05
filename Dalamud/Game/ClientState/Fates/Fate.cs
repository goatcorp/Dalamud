using System.Numerics;

using Dalamud.Data;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using Dalamud.Utility;

using Lumina.Excel;

namespace Dalamud.Game.ClientState.Fates;

/// <summary>
/// Interface representing an fate entry that can be seen in the current area.
/// </summary>
public interface IFate : IEquatable<IFate>
{
    /// <summary>
    /// Gets the Fate ID of this <see cref="Fate" />.
    /// </summary>
    ushort FateId { get; }

    /// <summary>
    /// Gets game data linked to this Fate.
    /// </summary>
    RowRef<Lumina.Excel.Sheets.Fate> GameData { get; }

    /// <summary>
    /// Gets the time this <see cref="Fate"/> started.
    /// </summary>
    int StartTimeEpoch { get; }

    /// <summary>
    /// Gets how long this <see cref="Fate"/> will run.
    /// </summary>
    short Duration { get; }

    /// <summary>
    /// Gets the remaining time in seconds for this <see cref="Fate"/>.
    /// </summary>
    long TimeRemaining { get; }

    /// <summary>
    /// Gets the displayname of this <see cref="Fate" />.
    /// </summary>
    SeString Name { get; }

    /// <summary>
    /// Gets the description of this <see cref="Fate" />.
    /// </summary>
    SeString Description { get; }

    /// <summary>
    /// Gets the objective of this <see cref="Fate" />.
    /// </summary>
    SeString Objective { get; }

    /// <summary>
    /// Gets the state of this <see cref="Fate"/> (Running, Ended, Failed, Preparation, WaitingForEnd).
    /// </summary>
    FateState State { get; }

    /// <summary>
    /// Gets the hand in count of this <see cref="Fate"/>.
    /// </summary>
    byte HandInCount { get; }

    /// <summary>
    /// Gets the progress amount of this <see cref="Fate"/>.
    /// </summary>
    byte Progress { get; }

    /// <summary>
    /// Gets a value indicating whether or not this <see cref="Fate"/> has a EXP bonus.
    /// </summary>
    [Obsolete("Use HasBonus instead")]
    bool HasExpBonus { get; }
    
    /// <summary>
    /// Gets a value indicating whether or not this <see cref="Fate"/> has a EXP bonus.
    /// </summary>
    bool HasBonus { get; }

    /// <summary>
    /// Gets a value indicating whether or not this <see cref="Fate"/> has a bonus.
    /// </summary>
    bool HasBonus { get; }

    /// <summary>
    /// Gets the icon id of this <see cref="Fate"/>.
    /// </summary>
    uint IconId { get; }

    /// <summary>
    /// Gets the level of this <see cref="Fate"/>.
    /// </summary>
    byte Level { get; }

    /// <summary>
    /// Gets the max level level of this <see cref="Fate"/>.
    /// </summary>
    byte MaxLevel { get; }

    /// <summary>
    /// Gets the position of this <see cref="Fate"/>.
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// Gets the radius of this <see cref="Fate"/>.
    /// </summary>
    float Radius { get; }

    /// <summary>
    /// Gets the map icon id of this <see cref="Fate"/>.
    /// </summary>
    uint MapIconId { get; }

    /// <summary>
    /// Gets the territory this <see cref="Fate"/> is located in.
    /// </summary>
    RowRef<Lumina.Excel.Sheets.TerritoryType> TerritoryType { get; }

    /// <summary>
    /// Gets the address of this Fate in memory.
    /// </summary>
    IntPtr Address { get; }
}

/// <summary>
/// This class represents an FFXIV Fate.
/// </summary>
internal unsafe partial class Fate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Fate"/> class.
    /// </summary>
    /// <param name="address">The address of this fate in memory.</param>
    internal Fate(IntPtr address)
    {
        this.Address = address;
    }

    /// <inheritdoc />
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
    bool IEquatable<IFate>.Equals(IFate other) => this.FateId == other?.FateId;

    /// <inheritdoc/>
    public override bool Equals(object obj) => ((IEquatable<IFate>)this).Equals(obj as IFate);

    /// <inheritdoc/>
    public override int GetHashCode() => this.FateId.GetHashCode();
}

/// <summary>
/// This class represents an FFXIV Fate.
/// </summary>
internal unsafe partial class Fate : IFate
{
    /// <inheritdoc/>
    public ushort FateId => this.Struct->FateId;

    /// <inheritdoc/>
    public RowRef<Lumina.Excel.Sheets.Fate> GameData => LuminaUtils.CreateRef<Lumina.Excel.Sheets.Fate>(this.FateId);

    /// <inheritdoc/>
    public int StartTimeEpoch => this.Struct->StartTimeEpoch;

    /// <inheritdoc/>
    public short Duration => this.Struct->Duration;

    /// <inheritdoc/>
    public long TimeRemaining => this.StartTimeEpoch + this.Duration - DateTimeOffset.Now.ToUnixTimeSeconds();

    /// <inheritdoc/>
    public SeString Name => MemoryHelper.ReadSeString(&this.Struct->Name);

    /// <inheritdoc/>
    public SeString Description => MemoryHelper.ReadSeString(&this.Struct->Description);

    /// <inheritdoc/>
    public SeString Objective => MemoryHelper.ReadSeString(&this.Struct->Objective);

    /// <inheritdoc/>
    public FateState State => (FateState)this.Struct->State;

    /// <inheritdoc/>
    public byte HandInCount => this.Struct->HandInCount;

    /// <inheritdoc/>
    public byte Progress => this.Struct->Progress;

    /// <inheritdoc/>
    [Obsolete("Use HasBonus instead")]
    public bool HasExpBonus => this.Struct->IsExpBonus;

    /// <inheritdoc/>
    public bool HasBonus => this.Struct->IsBonus;

    /// <inheritdoc/>
    public uint IconId => this.Struct->IconId;

    /// <inheritdoc/>
    public byte Level => this.Struct->Level;

    /// <inheritdoc/>
    public byte MaxLevel => this.Struct->MaxLevel;

    /// <inheritdoc/>
    public Vector3 Position => this.Struct->Location;

    /// <inheritdoc/>
    public float Radius => this.Struct->Radius;

    /// <inheritdoc/>
    public uint MapIconId => this.Struct->MapIconId;

    /// <summary>
    /// Gets the territory this <see cref="Fate"/> is located in.
    /// </summary>
    public RowRef<Lumina.Excel.Sheets.TerritoryType> TerritoryType => LuminaUtils.CreateRef<Lumina.Excel.Sheets.TerritoryType>(this.Struct->TerritoryId);
}
