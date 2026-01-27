using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying job gauge data.
/// </summary>
internal class GaugeWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["gauge", "jobgauge", "job"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Job Gauge";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var objectTable = Service<ObjectTable>.Get();
        var jobGauges = Service<JobGauges>.Get();

        var player = objectTable.LocalPlayer;
        if (player == null)
        {
            ImGui.Text("Player is not present"u8);
            return;
        }

        JobGaugeBase? gauge = player.ClassJob.RowId switch
        {
            19 => jobGauges.Get<PLDGauge>(),
            20 => jobGauges.Get<MNKGauge>(),
            21 => jobGauges.Get<WARGauge>(),
            22 => jobGauges.Get<DRGGauge>(),
            23 => jobGauges.Get<BRDGauge>(),
            24 => jobGauges.Get<WHMGauge>(),
            25 => jobGauges.Get<BLMGauge>(),
            27 => jobGauges.Get<SMNGauge>(),
            28 => jobGauges.Get<SCHGauge>(),
            30 => jobGauges.Get<NINGauge>(),
            31 => jobGauges.Get<MCHGauge>(),
            32 => jobGauges.Get<DRKGauge>(),
            33 => jobGauges.Get<ASTGauge>(),
            34 => jobGauges.Get<SAMGauge>(),
            35 => jobGauges.Get<RDMGauge>(),
            37 => jobGauges.Get<GNBGauge>(),
            38 => jobGauges.Get<DNCGauge>(),
            39 => jobGauges.Get<RPRGauge>(),
            40 => jobGauges.Get<SGEGauge>(),
            41 => jobGauges.Get<VPRGauge>(),
            42 => jobGauges.Get<PCTGauge>(),
            _ => null
        };

        if (gauge == null)
        {
            ImGui.Text("No supported gauge exists for this job."u8);
            return;
        }

        Util.ShowObject(gauge);
    }
}
