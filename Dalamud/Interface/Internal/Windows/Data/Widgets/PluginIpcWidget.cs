using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Internal;
using Dalamud.Utility;
using Dalamud.Bindings.ImGui;
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

    // IPC
    private ICallGateProvider<ICharacter?, string>? ipcPubGo;
    private ICallGateSubscriber<ICharacter?, string>? ipcSubGo;

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

        if (this.ipcPubGo == null)
        {
            this.ipcPubGo = new CallGatePubSub<ICharacter?, string>("dataDemo2");

            this.ipcPubGo.RegisterAction(go =>
            {
                Log.Information("Data action was called: {Name}", go?.Name);
            });

            this.ipcPubGo.RegisterFunc(go =>
            {
                Log.Information("Data func was called: {Name}", go?.Name);
                return "test";
            });
        }

        if (this.ipcSubGo == null)
        {
            this.ipcSubGo = new CallGatePubSub<ICharacter?, string>("dataDemo2");
            this.ipcSubGo.Subscribe(go => { Log.Information("GO: {Name}", go.Name); });
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

        if (ImGui.Button("Action GO"))
        {
            this.ipcSubGo.InvokeAction(Service<ClientState>.Get().LocalPlayer);
        }

        if (ImGui.Button("Func GO"))
        {
            this.callGateResponse = this.ipcSubGo.InvokeFunc(Service<ClientState>.Get().LocalPlayer);
        }

        if (!this.callGateResponse.IsNullOrEmpty())
            ImGui.Text($"Response: {this.callGateResponse}");
    }
}
