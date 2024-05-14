using Dalamud.Game.ClientState.Resolvers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace Dalamud.Game.ClientState.Aetherytes;

/// <summary>
/// This class represents an entry in the Aetheryte list.
/// </summary>
public sealed class AetheryteEntry : IAetheryteEntry
{
    private readonly TeleportInfo data;

    /// <summary>
    /// Initializes a new instance of the <see cref="AetheryteEntry"/> class.
    /// </summary>
    /// <param name="data">Data read from the Aetheryte List.</param>
    internal AetheryteEntry(TeleportInfo data)
    {
        this.data = data;
    }

    /// <summary>
    /// Gets the Aetheryte ID.
    /// </summary>
    public uint AetheryteId => this.data.AetheryteId;

    /// <summary>
    /// Gets the Territory ID.
    /// </summary>
    public uint TerritoryId => this.data.TerritoryId;

    /// <summary>
    /// Gets the SubIndex used when there can be multiple Aetherytes with the same ID (Private/Shared Estates etc.).
    /// </summary>
    public byte SubIndex => this.data.SubIndex;

    /// <summary>
    /// Gets the Ward. Zero if not a Shared Estate.
    /// </summary>
    public byte Ward => this.data.Ward;

    /// <summary>
    /// Gets the Plot. Zero if not a Shared Estate.
    /// </summary>
    public byte Plot => this.data.Plot;

    /// <summary>
    /// Gets the Cost in Gil to Teleport to this location.
    /// </summary>
    public uint GilCost => this.data.GilCost;

    /// <summary>
    /// Gets a value indicating whether the LocalPlayer has set this Aetheryte as Favorite or not.
    /// </summary>
    public bool IsFavourite => this.data.IsFavourite != 0;

    /// <summary>
    /// Gets a value indicating whether this Aetheryte is a Shared Estate or not.
    /// </summary>
    public bool IsSharedHouse => this.data.IsSharedHouse;

    /// <summary>
    /// Gets a value indicating whether this Aetheryte is an Apartment or not.
    /// </summary>
    public bool IsApartment => this.data.IsApartment;

    /// <summary>
    /// Gets the Aetheryte data related to this aetheryte.
    /// </summary>
    public ExcelResolver<Lumina.Excel.GeneratedSheets.Aetheryte> AetheryteData => new(this.AetheryteId);
}

/// <summary>
/// Interface representing an aetheryte entry available to the game
/// </summary>
public interface IAetheryteEntry
{
    /// <summary>
    /// Gets the Aetheryte ID.
    /// </summary>
    uint AetheryteId { get; }

    /// <summary>
    /// Gets the Territory ID.
    /// </summary>
    uint TerritoryId { get; }

    /// <summary>
    /// Gets the SubIndex used when there can be multiple Aetherytes with the same ID (Private/Shared Estates etc.).
    /// </summary>
    byte SubIndex { get; }

    /// <summary>
    /// Gets the Ward. Zero if not a Shared Estate.
    /// </summary>
    byte Ward { get; }

    /// <summary>
    /// Gets the Plot. Zero if not a Shared Estate.
    /// </summary>
    byte Plot { get; }

    /// <summary>
    /// Gets the Cost in Gil to Teleport to this location.
    /// </summary>
    uint GilCost { get; }

    /// <summary>
    /// Gets a value indicating whether the LocalPlayer has set this Aetheryte as Favorite or not.
    /// </summary>
    bool IsFavourite { get; }

    /// <summary>
    /// Gets a value indicating whether this Aetheryte is a Shared Estate or not.
    /// </summary>
    bool IsSharedHouse { get; }

    /// <summary>
    /// Gets a value indicating whether this Aetheryte is an Appartment or not.
    /// </summary>
    bool IsAppartment { get; }

    /// <summary>
    /// Gets the Aetheryte data related to this aetheryte.
    /// </summary>
    ExcelResolver<Lumina.Excel.GeneratedSheets.Aetheryte> AetheryteData { get; }
}

