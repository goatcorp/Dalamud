using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class represents the state of the game client at the time of access.
/// </summary>
public interface IClientState
{
    /// <summary>
    /// A delegate type used for the <see cref="Logout"/> event.
    /// </summary>
    /// <param name="type">The type of logout.</param>
    /// <param name="code">The success/failure code.</param>
    public delegate void LogoutDelegate(int type, int code);

    /// <summary>
    /// Event that gets fired when the current Territory changes.
    /// </summary>
    public event Action<ushort> TerritoryChanged;

    /// <summary>
    /// Event that fires when a characters ClassJob changed.
    /// </summary>
    [Obsolete("Use IPlayerState.ClassJobChange", true)]
    public event IPlayerState.ClassJobChangeDelegate? ClassJobChanged;

    /// <summary>
    /// Event that fires when <em>any</em> character level changes, including levels
    /// for a not-currently-active ClassJob (e.g. PvP matches, DoH/DoL).
    /// </summary>
    [Obsolete("Use IPlayerState.LevelChange", true)]
    public event IPlayerState.LevelChangeDelegate? LevelChanged;

    /// <summary>
    /// Event that fires when the local player is logged in, and the local character object is available.
    /// </summary>
    public event Action Login;

    /// <summary>
    /// Event that fires when the local player is logging out.
    /// </summary>
    public event LogoutDelegate Logout;

    /// <summary>
    /// Event that fires when the local player is entering a PvP zone.
    /// </summary>
    public event Action EnterPvP;

    /// <summary>
    /// Event that fires when the local player is leaving a PvP zone.
    /// </summary>
    public event Action LeavePvP;

    /// <summary>
    /// Event that gets fired when a duty is ready.
    /// </summary>
    public event Action<Lumina.Excel.Sheets.ContentFinderCondition> CfPop;

    /// <summary>
    /// Gets the language of the client.
    /// </summary>
    public ClientLanguage ClientLanguage { get; }

    /// <summary>
    /// Gets the current Territory the player resides in.
    /// </summary>
    public ushort TerritoryType { get; }

    /// <summary>
    /// Gets the current Map the player resides in.
    /// </summary>
    public uint MapId { get; }

    /// <summary>
    /// Gets the local player character, if one is present.
    /// </summary>
    public IPlayerCharacter? LocalPlayer { get; }

    /// <summary>
    /// Gets the content ID of the local character.
    /// </summary>
    [Obsolete("Use IPlayerState.ContentId", true)]
    public ulong LocalContentId { get; }

    /// <summary>
    /// Gets a value indicating whether a character is logged in.
    /// </summary>
    public bool IsLoggedIn { get; }

    /// <summary>
    /// Gets a value indicating whether the user is in a PvP zone.
    /// </summary>
    public bool IsPvP { get; }

    /// <summary>
    /// Gets a value indicating whether the user is in a PvP zone, excluding the Wolves' Den.
    /// </summary>
    public bool IsPvPExcludingDen { get; }

    /// <summary>
    /// Gets a value indicating whether the client is currently in Group Pose (GPose) mode.
    /// </summary>
    public bool IsGPosing { get; }

    /// <summary>
    /// Check whether the client is currently "idle". This means a player is not logged in, or is notctively in combat
    /// or doing anything that we may not want to disrupt.
    /// </summary>
    /// <param name="blockingFlag">An outvar containing the first observed condition blocking the "idle" state. 0 if idle.</param>
    /// <returns>Returns true if the client is idle, false otherwise.</returns>
    public bool IsClientIdle(out ConditionFlag blockingFlag);

    /// <summary>
    /// Check whether the client is currently "idle". This means a player is not logged in, or is notctively in combat
    /// or doing anything that we may not want to disrupt.
    /// </summary>
    /// <returns>Returns true if the client is idle, false otherwise.</returns>
    public bool IsClientIdle() => this.IsClientIdle(out _);
}
