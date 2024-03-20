using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Internal;
using Dalamud.Utility;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for testing plugin IPC systems.
/// </summary>
internal class PluginIpcWidget : IDataWindowWidget
{
    // IPC
    private ICallGateProvider<string, string>? ipcPub;
    private ICallGateSubscriber<string, string>? ipcSub;
    private string callGateResponse = string.Empty;
    
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "ipc" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Plugin IPC"; 

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
        if (this.ipcPub == null)
        {
            this.ipcPub = new CallGatePubSub<string, string>("dataDemo1");

            this.ipcPub.RegisterAction(msg =>
            {
                Log.Information("Data action was called: {Msg}", msg);
            });

            this.ipcPub.RegisterFunc(msg =>
            {
                Log.Information("Data func was called: {Msg}", msg);
                return Guid.NewGuid().ToString();
            });
        }

        if (this.ipcSub == null)
        {
            this.ipcSub = new CallGatePubSub<string, string>("dataDemo1");
            this.ipcSub.Subscribe(_ =>
            {
                Log.Information("PONG1");
            });
            this.ipcSub.Subscribe(_ =>
            {
                Log.Information("PONG2");
            });
            this.ipcSub.Subscribe(_ => throw new Exception("PONG3"));
        }

        if (ImGui.Button("PING"))
        {
            this.ipcPub.SendMessage("PING");
        }

        if (ImGui.Button("Action"))
        {
            this.ipcSub.InvokeAction("button1");
        }

        if (ImGui.Button("Func"))
        {
            this.callGateResponse = this.ipcSub.InvokeFunc("button2");
        }

        if (!this.callGateResponse.IsNullOrEmpty())
            ImGui.Text($"Response: {this.callGateResponse}");
    }
}
