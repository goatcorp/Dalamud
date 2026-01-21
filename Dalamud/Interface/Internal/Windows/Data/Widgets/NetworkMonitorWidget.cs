using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;

using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Application.Network;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Network;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget to display the current packets.
/// </summary>
internal unsafe class NetworkMonitorWidget : IDataWindowWidget
{
    private readonly ConcurrentQueue<NetworkPacketData> packets = new();

    private Hook<PacketDispatcher.Delegates.OnReceivePacket>? hookDown;
    private Hook<ZoneClientSendPacketDelegate>? hookUp;

    private bool trackNetwork;
    private int trackedPackets = 20;
    private Regex? trackedOpCodes;
    private string filterString = string.Empty;
    private Regex? untrackedOpCodes;
    private string negativeFilterString = string.Empty;

    /// <summary> Finalizes an instance of the <see cref="NetworkMonitorWidget"/> class. </summary>
    ~NetworkMonitorWidget()
    {
        this.hookDown?.Dispose();
        this.hookUp?.Dispose();
    }

    private delegate byte ZoneClientSendPacketDelegate(ZoneClient* thisPtr, nint packet, uint a3, uint a4, byte a5);

    private enum NetworkMessageDirection
    {
        ZoneDown,
        ZoneUp,
    }

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["network", "netmon", "networkmonitor"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Network Monitor";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.hookDown = Hook<PacketDispatcher.Delegates.OnReceivePacket>.FromAddress(
            (nint)PacketDispatcher.StaticVirtualTablePointer->OnReceivePacket,
            this.OnReceivePacketDetour);

        // TODO: switch to ZoneClient.SendPacket from CS
        if (Service<TargetSigScanner>.Get().TryScanText("E8 ?? ?? ?? ?? 4C 8B 44 24 ?? E9", out var address))
            this.hookUp = Hook<ZoneClientSendPacketDelegate>.FromAddress(address, this.SendPacketDetour);

        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        if (ImGui.Checkbox("Track Network Packets"u8, ref this.trackNetwork))
        {
            if (this.trackNetwork)
            {
                this.hookDown?.Enable();
                this.hookUp?.Enable();
            }
            else
            {
                this.hookDown?.Disable();
                this.hookUp?.Disable();
            }
        }

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
        if (ImGui.DragInt("Stored Number of Packets"u8, ref this.trackedPackets, 0.1f, 1, 512))
        {
            this.trackedPackets = Math.Clamp(this.trackedPackets, 1, 512);
        }

        if (ImGui.Button("Clear Stored Packets"u8))
        {
            this.packets.Clear();
        }

        DrawFilterInput("##Filter"u8, "Regex Filter OpCodes..."u8, ref this.filterString, ref this.trackedOpCodes);
        DrawFilterInput("##NegativeFilter"u8, "Regex Filter Against OpCodes..."u8, ref this.negativeFilterString, ref this.untrackedOpCodes);

        using var table = ImRaii.Table("NetworkMonitorTableV2"u8, 5, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Direction"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("OpCode"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("OpCode (Hex)"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Target EntityId"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var packet in this.packets.Reverse())
        {
            ImGui.TableNextColumn();
            ImGui.Text(packet.Time.ToLongTimeString());

            ImGui.TableNextColumn();
            ImGui.Text(packet.Direction.ToString());

            ImGui.TableNextColumn();
            WidgetUtil.DrawCopyableText(packet.OpCode.ToString());

            ImGui.TableNextColumn();
            WidgetUtil.DrawCopyableText($"0x{packet.OpCode:X4}");

            ImGui.TableNextColumn();
            if (packet.TargetActorId > 0)
            {
                WidgetUtil.DrawCopyableText($"{packet.TargetActorId:X}");

                if (packet.TargetActorId == PlayerState.Instance()->EntityId)
                {
                    ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
                    ImGui.Text("(Local Player)");
                }
                else
                {
                    var obj = GameObjectManager.Instance()->Objects.GetObjectByEntityId(packet.TargetActorId);
                    if (obj != null)
                    {
                        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
                        ImGui.Text($"({obj->NameString})");
                    }
                }
            }
        }
    }

    private static void DrawFilterInput(ReadOnlySpan<byte> label, ReadOnlySpan<byte> hint, ref string filterString, ref Regex? regex)
    {
        var invalidRegEx = filterString.Length > 0 && regex == null;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 2 * ImGuiHelpers.GlobalScale, invalidRegEx);
        using var color = ImRaii.PushColor(ImGuiCol.Border, 0xFF0000FF, invalidRegEx);

        ImGui.SetNextItemWidth(-1);
        if (!ImGui.InputTextWithHint(label, hint, ref filterString, 1024))
            return;

        if (filterString.Length == 0)
        {
            regex = null;
            return;
        }

        try
        {
            regex = new Regex(filterString, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        }
        catch
        {
            regex = null;
        }
    }

    private void OnReceivePacketDetour(PacketDispatcher* thisPtr, uint targetId, nint packet)
    {
        var opCode = *(ushort*)(packet + 2);
        this.RecordPacket(new NetworkPacketData(DateTime.Now, opCode, NetworkMessageDirection.ZoneDown, targetId));
        this.hookDown.OriginalDisposeSafe(thisPtr, targetId, packet);
    }

    private byte SendPacketDetour(ZoneClient* thisPtr, nint packet, uint a3, uint a4, byte a5)
    {
        var opCode = *(ushort*)packet;
        this.RecordPacket(new NetworkPacketData(DateTime.Now, opCode, NetworkMessageDirection.ZoneUp, 0));
        return this.hookUp.OriginalDisposeSafe(thisPtr, packet, a3, a4, a5);
    }

    private bool ShouldTrackPacket(ushort opCode)
    {
        return (this.trackedOpCodes == null || this.trackedOpCodes.IsMatch(this.OpCodeToString(opCode)))
            && (this.untrackedOpCodes == null || !this.untrackedOpCodes.IsMatch(this.OpCodeToString(opCode)));
    }

    private void RecordPacket(NetworkPacketData packet)
    {
        if (!this.ShouldTrackPacket(packet.OpCode))
            return;

        this.packets.Enqueue(packet);

        while (this.packets.Count > this.trackedPackets)
        {
            this.packets.TryDequeue(out _);
        }
    }

    /// <remarks> The filter should find opCodes by number (decimal and hex) and name, if existing. </remarks>
    private string OpCodeToString(ushort opCode)
        => $"{opCode}\0{opCode:X}";

#pragma warning disable SA1313
    private readonly record struct NetworkPacketData(DateTime Time, ushort OpCode, NetworkMessageDirection Direction, uint TargetActorId);
#pragma warning restore SA1313
}
