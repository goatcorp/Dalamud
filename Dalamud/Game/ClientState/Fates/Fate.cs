using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Data;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

using Lumina.Excel;

using CSFateContext = FFXIVClientStructs.FFXIV.Client.Game.Fate.FateContext;

namespace Dalamud.Game.ClientState.Fates;

/// <summary>
/// Interface representing a fate entry that can be seen in the current area.
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
    /// Gets a value indicating whether this <see cref="Fate"/> has a bonus.
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
    nint Address { get; }
}

/// <summary>
/// This struct represents a Fate.
/// </summary>
/// <param name="ptr">A pointer to the FateContext.</param>
internal readonly unsafe struct Fate(CSFateContext* ptr) : IFate
{
    /// <inheritdoc />
    public nint Address => (nint)ptr;

    /// <inheritdoc/>
    public ushort FateId => ptr->FateId;

    /// <inheritdoc/>
    public RowRef<Lumina.Excel.Sheets.Fate> GameData => LuminaUtils.CreateRef<Lumina.Excel.Sheets.Fate>(this.FateId);

    /// <inheritdoc/>
    public int StartTimeEpoch => ptr->StartTimeEpoch;

    /// <inheritdoc/>
    public short Duration => ptr->Duration;

    /// <inheritdoc/>
    public long TimeRemaining => this.StartTimeEpoch + this.Duration - DateTimeOffset.Now.ToUnixTimeSeconds();

    /// <inheritdoc/>
    public SeString Name => MemoryHelper.ReadSeString(&ptr->Name);

    /// <inheritdoc/>
    public SeString Description => MemoryHelper.ReadSeString(&ptr->Description);

    /// <inheritdoc/>
    public SeString Objective => MemoryHelper.ReadSeString(&ptr->Objective);

    /// <inheritdoc/>
    public FateState State => (FateState)ptr->State;

    /// <inheritdoc/>
    public byte HandInCount => ptr->HandInCount;

    /// <inheritdoc/>
    public byte Progress => ptr->Progress;

    /// <inheritdoc/>
    public bool HasBonus => ptr->IsBonus;

    /// <inheritdoc/>
    public uint IconId => ptr->IconId;

    /// <inheritdoc/>
    public byte Level => ptr->Level;

    /// <inheritdoc/>
    public byte MaxLevel => ptr->MaxLevel;

    /// <inheritdoc/>
    public Vector3 Position => ptr->Location;

    /// <inheritdoc/>
    public float Radius => ptr->Radius;

    /// <inheritdoc/>
    public uint MapIconId => ptr->MapIconId;

    /// <summary>
    /// Gets the territory this <see cref="Fate"/> is located in.
    /// </summary>
    public RowRef<Lumina.Excel.Sheets.TerritoryType> TerritoryType => LuminaUtils.CreateRef<Lumina.Excel.Sheets.TerritoryType>(ptr->MapMarkers[0].MapMarkerData.TerritoryTypeId);

    public static bool operator ==(Fate x, Fate y) => x.Equals(y);

    public static bool operator !=(Fate x, Fate y) => !(x == y);

    /// <inheritdoc/>
    public bool Equals(IFate? other)
    {
        return this.FateId == other.FateId;
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Fate fate && this.Equals(fate);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return this.FateId.GetHashCode();
    }
}
