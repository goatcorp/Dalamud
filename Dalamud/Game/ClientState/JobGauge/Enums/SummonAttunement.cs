namespace Dalamud.Game.ClientState.JobGauge.Enums;

public enum SummonAttunement
{
    /// <summary>
    /// No attunement.
    /// </summary>
    NONE = 0,

    /// <summary>
    /// Attuned to the summon Ifrit.
    /// Same as <see cref="JobGauge.Types.SMNGauge.IsIfritAttuned"/>.
    /// </summary>
    IFRIT = 1,

    /// <summary>
    /// Attuned to the summon Titan.
    /// Same as <see cref="JobGauge.Types.SMNGauge.IsTitanAttuned"/>.
    /// </summary>
    TITAN = 2,

    /// <summary>
    /// Attuned to the summon Garuda.
    /// Same as <see cref="JobGauge.Types.SMNGauge.IsGarudaAttuned"/>.
    /// </summary>
    GARUDA = 3,
}
