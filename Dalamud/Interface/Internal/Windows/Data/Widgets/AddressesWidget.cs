using System.Collections.Generic;

using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget to display resolved .text sigs.
/// </summary>
internal class AddressesWidget : IDataWindowWidget
{
    private string inputSig = string.Empty;
    private nint sigResult = nint.Zero;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["address"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Addresses";

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
        ImGui.InputText(".text sig"u8, ref this.inputSig, 400);
        if (ImGui.Button("Resolve"u8))
        {
            try
            {
                var sigScanner = Service<TargetSigScanner>.Get();
                this.sigResult = sigScanner.ScanText(this.inputSig);
            }
            catch (KeyNotFoundException)
            {
                this.sigResult = new nint(-1);
            }
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Text($"Result: {this.sigResult:X}");
        ImGui.SameLine();
        if (ImGui.Button($"C##{this.sigResult:X}"))
            ImGui.SetClipboardText($"{this.sigResult:X}");

        foreach (var debugScannedValue in BaseAddressResolver.DebugScannedValues)
        {
            ImGui.Text($"{debugScannedValue.Key}");
            foreach (var valueTuple in debugScannedValue.Value)
            {
                using var indent = ImRaii.PushIndent(10.0f);
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"{valueTuple.ClassName} - {Util.DescribeAddress(valueTuple.Address)}");
                ImGui.SameLine();

                if (ImGui.Button($"C##{valueTuple.Address:X}"))
                    ImGui.SetClipboardText($"{valueTuple.Address:X}");
            }
        }
    }
}
