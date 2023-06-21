﻿using Dalamud.Data;
using ImGuiNET;
using Newtonsoft.Json;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Widget to display the currently set server opcodes.
/// </summary>
internal class ServerOpcodeWidget : IDataWindowWidget
{
    private string? serverOpString;
    
    /// <inheritdoc/>
    public DataKind DataKind { get; init; } = DataKind.Server_OpCode;

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        var dataManager = Service<DataManager>.Get();

        if (dataManager.IsDataReady)
        {
            this.serverOpString = JsonConvert.SerializeObject(dataManager.ServerOpCodes, Formatting.Indented);
            this.Ready = true;
        }
    }
    
    /// <inheritdoc/>
    public void Draw()
    {
        ImGui.TextUnformatted(this.serverOpString ?? "serverOpString not initialized");
    }
}
