using System.Linq;
using System.Text;

using Dalamud.Data;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using Serilog;

namespace Dalamud.Game.ClientState;

/// <summary>
/// Provides event data for when the game should initialize a zone.
/// </summary>
[Api15ToDo("Replace all direct references with Lumina RowRef instead.")]
public class ZoneInitEventArgs : EventArgs
{
    /// <summary>
    /// Gets the territory type of the zone being entered.
    /// </summary>
    public TerritoryType TerritoryType { get; private set; }

    /// <summary>
    /// Gets the instance number of the zone, used when multiple copies of an area are active.
    /// </summary>
    public ushort Instance { get; private set; }

    /// <summary>
    /// Gets the associated content finder condition for the zone, if any.
    /// </summary>
    public ContentFinderCondition ContentFinderCondition { get; private set; }

    /// <summary>
    /// Gets the current weather in the zone upon entry.
    /// </summary>
    public Weather Weather { get; private set; }

    /// <summary>
    /// Gets the set of active festivals in the zone.
    /// </summary>
    public Festival[] ActiveFestivals { get; private set; } = [];

    /// <summary>
    /// Gets the phases corresponding to the active festivals.
    /// </summary>
    public ushort[] ActiveFestivalPhases { get; private set; } = [];

    /// <summary>
    /// Reads raw zone initialization data from a network packet and constructs the event arguments.
    /// </summary>
    /// <param name="packet">A pointer to the raw packet data.</param>
    /// <returns>A <see cref="ZoneInitEventArgs"/> populated from the packet.</returns>
    public static unsafe ZoneInitEventArgs Read(nint packet)
    {
        var dataManager = Service<DataManager>.Get();
        var eventArgs = new ZoneInitEventArgs();

        var flags = *(byte*)(packet + 0x12);

        var territoryKey = *(ushort*)(packet + 0x02);
        if (!dataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryKey, out var territoryRow))
        {
            Log.Error($"Failed to get TerritoryType row with ID {territoryKey}. Corrupted packet?");
            return new ZoneInitEventArgs();
        }
        eventArgs.TerritoryType = territoryRow;

        var contentFinderKey = *(ushort*)(packet + 0x06);
        if (!dataManager.GetExcelSheet<ContentFinderCondition>().TryGetRow(contentFinderKey, out var contentFinderRow))
        {
            Log.Error($"Failed to get ContentFinderCondition row with ID {contentFinderKey}. Corrupted packet?");
            return new ZoneInitEventArgs();
        }
        eventArgs.ContentFinderCondition = contentFinderRow;

        var weatherKey = *(byte*)(packet + 0x10);
        if (!dataManager.GetExcelSheet<Weather>().TryGetRow(weatherKey, out var weatherRow))
        {
            Log.Error($"Failed to get Weather row with ID {weatherKey}. Corrupted packet?");
            return new ZoneInitEventArgs();
        }
        eventArgs.Weather = weatherRow;

        eventArgs.Instance = flags >= 0 ? (ushort)0 : *(ushort*)(packet + 0x04);

        const int NumFestivals = 8;
        eventArgs.ActiveFestivals = new Festival[NumFestivals];
        eventArgs.ActiveFestivalPhases = new ushort[NumFestivals];

        // There are also 4 festival ids and phases for PlayerState at +0x3E and +0x46 respectively,
        // but it's unclear why they exist as separate entries and why they would be different.
        for (var i = 0; i < NumFestivals; i++)
        {
            var festivalKey = *(ushort*)(packet + 0x26 + (i * 2));
            if (!dataManager.GetExcelSheet<Festival>().TryGetRow(festivalKey, out var festivalRow))
            {
                Log.Error($"Failed to get Festival row with ID {festivalKey}. Corrupted packet?");
                return new ZoneInitEventArgs();
            }
            eventArgs.ActiveFestivals[i] = festivalRow;
            eventArgs.ActiveFestivalPhases[i] = *(ushort*)(packet + 0x36 + (i * 2));
        }

        return eventArgs;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder("ZoneInitEventArgs { ");
        sb.Append($"TerritoryTypeId = {this.TerritoryType.RowId}, ");
        sb.Append($"Instance = {this.Instance}, ");
        sb.Append($"ContentFinderCondition = {this.ContentFinderCondition.RowId}, ");
        sb.Append($"Weather = {this.Weather.RowId}, ");
        sb.Append($"ActiveFestivals = [{string.Join(", ", this.ActiveFestivals.Select(f => f.RowId))}], ");
        sb.Append($"ActiveFestivalPhases = [{string.Join(", ", this.ActiveFestivalPhases)}]");
        sb.Append(" }");
        return sb.ToString();
    }
}
