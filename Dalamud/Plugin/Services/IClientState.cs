using Dalamud.Game.ClientState.Objects.SubKinds;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class represents the state of the game client at the time of access.
/// </summary>
public interface IClientState
{
    /// <summary>
    /// Event that gets fired when the current Territory changes.
    /// </summary>
    public event Action<ushort> TerritoryChanged;

    /// <summary>
    /// Event that fires when a character is logging in, and the local character object is available.
    /// </summary>
    public event Action Login;

    /// <summary>
    /// Event that fires when a character is logging out.
    /// </summary>
    public event Action Logout;

    /// <summary>
    /// Event that fires when a character is entering PvP.
    /// </summary>
    public event Action EnterPvP;

    /// <summary>
    /// Event that fires when a character is leaving PvP.
    /// </summary>
    public event Action LeavePvP;

    /// <summary>
    /// Event that gets fired when a duty is ready.
    /// </summary>
    public event Action<Lumina.Excel.GeneratedSheets.ContentFinderCondition> CfPop;

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
    public PlayerCharacter? LocalPlayer { get; }

    /// <summary>
    /// Gets the content ID of the local character.
    /// </summary>
    public ulong LocalContentId { get; }

    /// <summary>
    /// Gets a value indicating whether a character is logged in.
    /// </summary>
    public bool IsLoggedIn { get; }

    /// <summary>
    /// Gets a value indicating whether or not the user is playing PvP.
    /// </summary>
    public bool IsPvP { get; }

    /// <summary>
    /// Gets a value indicating whether or not the user is playing PvP, excluding the Wolves' Den.
    /// </summary>
    public bool IsPvPExcludingDen { get; }
    
    /// <summary>
    /// Gets a value indicating whether the client is currently in Group Pose (GPose) mode. 
    /// </summary>
    public bool IsGPosing { get; }
}
