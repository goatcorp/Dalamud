namespace Dalamud.Plugin.Services;

/// <summary>
/// This class represents the state of the currently occupied duty.
/// </summary>
public interface IDutyState
{
    /// <summary>
    /// Event that gets fired when the duty starts.
    /// Triggers when the "Duty Start" message displays, and on the removal of the ring at duty's spawn.
    /// Does not trigger when loading into a duty that was in progress, or from loading in after a disconnect.
    /// </summary>
    public event EventHandler<ushort> DutyStarted;
    
    /// <summary>
    /// Event that gets fired when everyone in the party dies and the screen fades to black.
    /// </summary>
    public event EventHandler<ushort> DutyWiped;
    
    /// <summary>
    /// Event that gets fired when the "Duty Recommence" message displays, and on the removal of the ring at duty's spawn.
    /// </summary>
    public event EventHandler<ushort> DutyRecommenced;
    
    /// <summary>
    /// Event that gets fired when the duty is completed successfully.
    /// </summary>
    public event EventHandler<ushort> DutyCompleted;
    
    /// <summary>
    /// Gets a value indicating whether the current duty has been started.
    /// </summary>
    public bool IsDutyStarted { get; }
}
