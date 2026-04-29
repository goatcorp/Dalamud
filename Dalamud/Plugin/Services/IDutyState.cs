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
    /// A delegate type used for the <see cref="DutyStarted"/> event.
    /// </summary>
    /// <param name="args">The events arguments.</param>
    public delegate void DutyStartedDelegate(IDutyStateEventArgs args);

    /// <summary>
    /// A delegate type used for the <see cref="DutyWiped"/> event.
    /// </summary>
    /// <param name="args">The events arguments.</param>
    public delegate void DutyWipedDelegate(IDutyStateEventArgs args);

    /// <summary>
    /// A delegate type used for the <see cref="DutyRecommenced"/> event.
    /// </summary>
    /// <param name="args">The events arguments.</param>
    public delegate void DutyRecommencedDelegate(IDutyStateEventArgs args);

    /// <summary>
    /// A delegate type used for the <see cref="DutyCompleted"/> event.
    /// </summary>
    /// <param name="args">The events arguments.</param>
    public delegate void DutyCompletedDelegate(IDutyStateEventArgs args);

    /// <summary>
    /// Event that gets fired when the duty starts.
    /// Triggers when the "Duty Start" message displays, and on the removal of the ring at duty's spawn.
    /// Does not trigger when loading into a duty that was in progress, or from loading in after a disconnect.
    /// </summary>
    public event DutyStartedDelegate DutyStarted;
    
    /// <summary>
    /// Event that gets fired when everyone in the party dies and the screen fades to black.
    /// </summary>
    public event DutyWipedDelegate DutyWiped;
    
    /// <summary>
    /// Event that gets fired when the "Duty Recommence" message displays, and on the removal of the ring at duty's spawn.
    /// </summary>
    public event DutyRecommencedDelegate DutyRecommenced;
    
    /// <summary>
    /// Event that gets fired when the duty is completed successfully.
    /// </summary>
    public event DutyCompletedDelegate DutyCompleted;

    /// <summary>
    /// Gets a RowRef to the current ContentFinderCondition row.
    /// </summary>
    public RowRef<ContentFinderCondition> ContentFinderCondition { get; }

    /// <summary>
    /// Gets a value indicating whether the current duty has been started.
    /// </summary>
    public bool IsDutyStarted { get; }
}
