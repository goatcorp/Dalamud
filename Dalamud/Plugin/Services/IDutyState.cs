using Dalamud.Game.DutyState;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class represents the state of the currently occupied duty.
/// </summary>
public interface IDutyState : IDalamudService
{
    /// <summary>
    /// Event that gets fired when the duty starts.
    /// Triggers when the "Duty Start" message displays, and on the removal of the ring at duty's spawn.
    /// Does not trigger when loading into a duty that was in progress, or from loading in after a disconnect.
    /// </summary>
    public event Action<DutyStateEventArgs> DutyStarted;
    
    /// <summary>
    /// Event that gets fired when everyone in the party dies and the screen fades to black.
    /// </summary>
    public event Action<DutyStateEventArgs> DutyWiped;
    
    /// <summary>
    /// Event that gets fired when the "Duty Recommence" message displays, and on the removal of the ring at duty's spawn.
    /// </summary>
    public event Action<DutyStateEventArgs> DutyRecommenced;
    
    /// <summary>
    /// Event that gets fired when the duty is completed successfully.
    /// </summary>
    public event Action<DutyStateEventArgs> DutyCompleted;

    /// <summary>
    /// Gets a RowRef to the current ContentFinderCondition row.
    /// </summary>
    public RowRef<ContentFinderCondition> ContentFinderCondition { get; }

    /// <summary>
    /// Gets a value indicating whether the current duty has been started.
    /// </summary>
    public bool IsDutyStarted { get; }
}
