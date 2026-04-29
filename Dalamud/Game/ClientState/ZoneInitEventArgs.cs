using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dalamud.Data;

using FFXIVClientStructs.FFXIV.Client.Game.Network;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Dalamud.Game.ClientState;

/// <summary>
/// Provides event data for when the game should initialize a zone.
/// </summary>
public class ZoneInitEventArgs : EventArgs
{
    private const int NumFestivals = 8;

    /// <summary>
    /// Gets the territory type of the zone being entered.
    /// </summary>
    public RowRef<TerritoryType> TerritoryType { get; private set; }

    /// <summary>
    /// Gets the instance number of the zone, used when multiple copies of an area are active.
    /// </summary>
    public ushort Instance { get; private set; }

    /// <summary>
    /// Gets the associated content finder condition for the zone, if any.
    /// </summary>
    public RowRef<ContentFinderCondition> ContentFinderCondition { get; private set; }

    /// <summary>
    /// Gets the current weather in the zone upon entry.
    /// </summary>
    public RowRef<Weather> Weather { get; private set; }

    /// <summary>
    /// Gets the set of active festivals in the zone.
    /// </summary>
    public IReadOnlyList<FestivalEntry> ActiveFestivals { get; private set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder("ZoneInitEventArgs { ");
        sb.Append($"TerritoryTypeId = {this.TerritoryType.RowId}, ");
        sb.Append($"Instance = {this.Instance}, ");
        sb.Append($"ContentFinderCondition = {this.ContentFinderCondition.RowId}, ");
        sb.Append($"Weather = {this.Weather.RowId}, ");
        sb.Append($"ActiveFestivals = [{string.Join(", ", this.ActiveFestivals.Select(f => $"{f.Festival.RowId}|{f.FestivalPhase}"))}], ");
        sb.Append(" }");
        return sb.ToString();
    }

    /// <summary>
    /// Reads raw zone initialization data from a network packet and constructs the event arguments.
    /// </summary>
    /// <param name="packet">A pointer to the raw packet data.</param>
    /// <returns>A <see cref="ZoneInitEventArgs"/> populated from the packet.</returns>
    internal static unsafe ZoneInitEventArgs Read(ZoneInitPacket* packet)
    {
        var territoryTypeId = packet->TerritoryTypeId;
        var contentFinderConditionId = packet->ContentFinderConditionId;
        var weatherId = packet->WeatherId;
        var instance = packet->Flags.HasFlag(ZoneInitFlags.IsInstancedArea)
            ? packet->Instance
            : (ushort)0;

        var festivals = new FestivalEntry[NumFestivals];
        for (var i = 0; i < NumFestivals; i++)
        {
            var festivalId = packet->GameFestivalIds[i];
            var festivalPhase = packet->GameFestivalPhases[i];
            festivals[i] = new FestivalEntry(LuminaUtils.CreateRef<Festival>(festivalId), festivalPhase);
        }

        return new ZoneInitEventArgs()
        {
            TerritoryType = LuminaUtils.CreateRef<TerritoryType>(territoryTypeId),
            Instance = instance,
            ContentFinderCondition = LuminaUtils.CreateRef<ContentFinderCondition>(contentFinderConditionId),
            Weather = LuminaUtils.CreateRef<Weather>(weatherId),
            ActiveFestivals = festivals,
        };
    }

    /// <summary>
    /// Represents an active Festival.
    /// </summary>
    /// <param name="Festival">A RowRef to the Festival sheet.</param>
    /// <param name="FestivalPhase">The phase of the Festival.</param>
    public readonly record struct FestivalEntry(RowRef<Festival> Festival, ushort FestivalPhase);
}
