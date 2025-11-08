using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace Dalamud.Game.Gui;

/// <summary>
/// Represents a flag set by the game used by agents to conditionally update their addons.
/// </summary>
[Flags]
public enum AgentUpdateFlag : byte
{
    /// <summary> Set when an inventory has been updated. </summary>
    InventoryUpdate = 1 << 0,

    /// <summary> Set when a hotbar slot has been executed, or a Gearset or Macro has been changed. </summary>
    ActionBarUpdate = 1 << 1,

    /// <summary> Set when the RetainerMarket inventory has been updated. </summary>
    RetainerMarketInventoryUpdate = 1 << 2,

    // /// <summary> Unknown use case. </summary>
    // NameplateUpdate = 1 << 3,

    /// <summary> Set when the player unlocked collectibles, contents or systems. </summary>
    UnlocksUpdate = 1 << 4,

    /// <summary> Set when <see cref="AgentHUD.SetMainCommandEnabledState"/> was called. </summary>
    MainCommandEnabledStateUpdate = 1 << 5,

    /// <summary> Set when any housing inventory has been updated. </summary>
    HousingInventoryUpdate = 1 << 6,

    /// <summary> Set when any content inventory has been updated. </summary>
    ContentInventoryUpdate = 1 << 7,
}
