using System.Text;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Dalamud.Game.DutyState;

/// <summary>
/// Provides event data for when the duty state changed.
/// </summary>
public class DutyStateEventArgs : EventArgs
{
    /// <summary>
    /// Gets a RowRef for the TerritoryType at the time the event was fired.
    /// </summary>
    public required RowRef<TerritoryType> TerritoryType { get; init; }

    /// <summary>
    /// Gets a RowRef for the ContentFinderCondition at the time the event was fired.
    /// </summary>
    public required RowRef<ContentFinderCondition> ContentFinderCondition { get; init; }

    /// <summary>
    /// Gets the EventHandler id for which this event was fired.
    /// </summary>
    public required uint EventHandlerId { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder("DutyStateEventArgs { ");
        sb.Append($"TerritoryTypeId = {this.TerritoryType.RowId}, ");
        sb.Append($"ContentFinderCondition = {this.ContentFinderCondition.RowId}, ");
        sb.Append($"EventHandlerId = {this.EventHandlerId}, ");
        sb.Append(" }");
        return sb.ToString();
    }
}
