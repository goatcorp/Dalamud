using System.Collections.Generic;

using Dalamud.Game;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget to display resolved .text sigs.
/// </summary>
internal class AddressesWidget : IDataWindowWidget
{
    private string inputSig = string.Empty;
    private nint sigResult = nint.Zero;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "address" };

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
        ImGui.InputText(".text sig", ref this.inputSig, 400);
        if (ImGui.Button("Resolve"))
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

        ImGui.Text($"Result: {this.sigResult.ToInt64():X}");
        ImGui.SameLine();
        if (ImGui.Button($"C##{this.sigResult.ToInt64():X}"))
            ImGui.SetClipboardText(this.sigResult.ToInt64().ToString("X"));

        foreach (var debugScannedValue in BaseAddressResolver.DebugScannedValues)
        {
            ImGui.TextUnformatted($"{debugScannedValue.Key}");
            foreach (var valueTuple in debugScannedValue.Value)
            {
                ImGui.TextUnformatted(
                    $"      {valueTuple.ClassName} - 0x{valueTuple.Address.ToInt64():X}");
                ImGui.SameLine();

                if (ImGui.Button($"C##{valueTuple.Address.ToInt64():X}"))
                    ImGui.SetClipboardText(valueTuple.Address.ToInt64().ToString("X"));
            }
        }
    }
}
