using System.Linq;
using System.Text;

using Dalamud.Data;

using Lumina.Excel.Sheets;

using Serilog;

namespace Dalamud.Game.ClientState;

/// <summary>
/// Provides event data for when the game should initialize a zone.
/// </summary>
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

        try
        {
            var flags = *(byte*)(packet + 0x12);

            eventArgs.TerritoryType = dataManager.GetExcelSheet<TerritoryType>().GetRow(*(ushort*)(packet + 0x02));
            eventArgs.Instance = flags >= 0 ? (ushort)0 : *(ushort*)(packet + 0x04);
            eventArgs.ContentFinderCondition = dataManager.GetExcelSheet<ContentFinderCondition>().GetRow(*(ushort*)(packet + 0x06));
            eventArgs.Weather = dataManager.GetExcelSheet<Weather>().GetRow(*(byte*)(packet + 0x10));

            const int NumFestivals = 8;
            eventArgs.ActiveFestivals = new Festival[NumFestivals];
            eventArgs.ActiveFestivalPhases = new ushort[NumFestivals];

            // There are also 4 festival ids and phases for PlayerState at +0x3E and +0x46 respectively,
            // but it's unclear why they exist as separate entries and why they would be different.
            for (var i = 0; i < NumFestivals; i++)
            {
                eventArgs.ActiveFestivals[i] = dataManager.GetExcelSheet<Festival>().GetRow(*(ushort*)(packet + 0x26 + (i * 2)));
                eventArgs.ActiveFestivalPhases[i] = *(ushort*)(packet + 0x36 + (i * 2));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read ZoneInit packet");
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
