using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Game states in which plugin UI may be hidden automatically (doesn't do any hiding, just collects logic).
/// </summary>
internal static class UiHideState
{
    /// <summary>
    /// Gets a value indicating whether the user toggled the game UI off.
    /// </summary>
    public static bool GameUiHidden => Service<GameGui>.GetNullable()?.GameUiHidden == true;

    /// <summary>
    /// Gets a value indicating whether GPose is active.
    /// </summary>
    public static bool GPosing => Service<ClientState>.GetNullable()?.IsGPosing == true;

    /// <summary>
    /// Gets a value indicating whether a cutscene is playing.
    /// </summary>
    public static bool CutsceneActive
    {
        get
        {
            var condition = Service<Condition>.GetNullable();
            if (condition == null)
            {
                return false;
            }

            return condition[ConditionFlag.OccupiedInCutSceneEvent]
                   || condition[ConditionFlag.WatchingCutscene78];
        }
    }

    /// <summary>
    /// Gets a value indicating whether any of the states is active.
    /// </summary>
    public static bool AnyActive => GameUiHidden || GPosing || CutsceneActive;
}
