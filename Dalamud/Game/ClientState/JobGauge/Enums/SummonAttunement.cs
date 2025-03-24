namespace Dalamud.Game.ClientState.JobGauge.Enums;

/// <summary>
/// Enum representing the current attunement of a  summoner.
/// </summary>
public enum SummonAttunement
{
    /// <summary>
    /// No attunement.
    /// </summary>
    None = 0,

    /// <summary>
    /// Attuned to the summon Ifrit.
    /// Same as <see cref="JobGauge.Types.SMNGauge.IsIfritAttuned"/>.
    /// </summary>
    Ifrit = 1,

    /// <summary>
    /// Attuned to the summon Titan.
    /// Same as <see cref="JobGauge.Types.SMNGauge.IsTitanAttuned"/>.
    /// </summary>
    Titan = 2,

    /// <summary>
    /// Attuned to the summon Garuda.
    /// Same as <see cref="JobGauge.Types.SMNGauge.IsGarudaAttuned"/>.
    /// </summary>
    Garuda = 3,
}
