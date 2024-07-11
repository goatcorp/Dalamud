using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Dalamud.Game.Network;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Memory;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget to display the current packets.
/// </summary>
internal class NetworkMonitorWidget : IDataWindowWidget
{
    private readonly ConcurrentQueue<NetworkPacketData> packets = new();

    private bool trackNetwork;
    private int trackedPackets;
    private Regex? trackedOpCodes;
    private string filterString = string.Empty;
    private Regex? untrackedOpCodes;
    private string negativeFilterString = string.Empty;

    /// <summary> Finalizes an instance of the <see cref="NetworkMonitorWidget"/> class. </summary>
    ~NetworkMonitorWidget()
    {
        if (this.trackNetwork)
        {
            this.trackNetwork = false;
            var network = Service<GameNetwork>.GetNullable();
            if (network != null)
            {
                network.NetworkMessage -= this.OnNetworkMessage;
            }
        }
    }

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "network", "netmon", "networkmonitor" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Network Monitor"; 

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.trackNetwork = false;
        this.trackedPackets = 20;
        this.trackedOpCodes = null;
        this.filterString = string.Empty;
        this.packets.Clear();
        this.Ready = true;
    }
    
    /// <inheritdoc/>
    public void Draw()
    {
        var network = Service<GameNetwork>.Get();
        if (ImGui.Checkbox("Track Network Packets", ref this.trackNetwork))
        {
            if (this.trackNetwork)
            {
                network.NetworkMessage += this.OnNetworkMessage;
            }
            else
            {
                network.NetworkMessage -= this.OnNetworkMessage;
            }
        }

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
        if (ImGui.DragInt("Stored Number of Packets", ref this.trackedPackets, 0.1f, 1, 512))
        {
            this.trackedPackets = Math.Clamp(this.trackedPackets, 1, 512);
        }

        if (ImGui.Button("Clear Stored Packets"))
        {
            this.packets.Clear();
        }

        this.DrawFilterInput();
        this.DrawNegativeFilterInput();

        ImGuiTable.DrawTable(string.Empty, this.packets, this.DrawNetworkPacket, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg, "Direction", "OpCode", "Hex", "Target", "Source", "Data");
    }

    private void DrawNetworkPacket(NetworkPacketData data)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(data.Direction.ToString());

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(data.OpCode.ToString());

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"0x{data.OpCode:X4}");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(data.TargetActorId > 0 ? $"0x{data.TargetActorId:X}" : string.Empty);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted(data.SourceActorId > 0 ? $"0x{data.SourceActorId:X}" : string.Empty);

        ImGui.TableNextColumn();
        if (data.Data.Count > 0)
        {
            ImGui.TextUnformatted(string.Join(" ", data.Data.Select(b => b.ToString("X2"))));
        }
        else
        {
            ImGui.Dummy(ImGui.GetContentRegionAvail() with { Y = 0 });
        }
    }

    private void DrawFilterInput()
    {
        var invalidRegEx = this.filterString.Length > 0 && this.trackedOpCodes == null;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 2 * ImGuiHelpers.GlobalScale, invalidRegEx);
        using var color = ImRaii.PushColor(ImGuiCol.Border, 0xFF0000FF, invalidRegEx);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (!ImGui.InputTextWithHint("##Filter", "Regex Filter OpCodes...", ref this.filterString, 1024))
        {
            return;
        }

        if (this.filterString.Length == 0)
        {
            this.trackedOpCodes = null;
        }
        else
        {
            try
            {
                this.trackedOpCodes = new Regex(this.filterString, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
            }
            catch
            {
                this.trackedOpCodes = null;
            }
        }
    }

    private void DrawNegativeFilterInput()
    {
        var invalidRegEx = this.negativeFilterString.Length > 0 && this.untrackedOpCodes == null;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 2 * ImGuiHelpers.GlobalScale, invalidRegEx);
        using var color = ImRaii.PushColor(ImGuiCol.Border, 0xFF0000FF, invalidRegEx);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (!ImGui.InputTextWithHint("##NegativeFilter", "Regex Filter Against OpCodes...", ref this.negativeFilterString, 1024))
        {
            return;
        }

        if (this.negativeFilterString.Length == 0)
        {
            this.untrackedOpCodes = null;
        }
        else
        {
            try
            {
                this.untrackedOpCodes = new Regex(this.negativeFilterString, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
            }
            catch
            {
                this.untrackedOpCodes = null;
            }
        }
    }

    private void OnNetworkMessage(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
    {
        if ((this.trackedOpCodes == null || this.trackedOpCodes.IsMatch(this.OpCodeToString(opCode)))
            && (this.untrackedOpCodes == null || !this.untrackedOpCodes.IsMatch(this.OpCodeToString(opCode))))
        {
            this.packets.Enqueue(new NetworkPacketData(this, opCode, direction, sourceActorId, targetActorId, dataPtr));
            while (this.packets.Count > this.trackedPackets)
            {
                this.packets.TryDequeue(out _);
            }
        }
    }

    private int GetSizeFromOpCode(ushort opCode)
        => 0;

    /// <remarks> Add known packet-name -> packet struct size associations here to copy the byte data for such packets. </remarks>>
    private int GetSizeFromName(string name)
        => name switch
        {
            _ => 0,
        };

    /// <remarks> The filter should find opCodes by number (decimal and hex) and name, if existing. </remarks>
    private string OpCodeToString(ushort opCode)
        => $"{opCode}\0{opCode:X}";
    
#pragma warning disable SA1313
    private readonly record struct NetworkPacketData(ushort OpCode, NetworkMessageDirection Direction, uint SourceActorId, uint TargetActorId)
#pragma warning restore SA1313
    {
        public readonly IReadOnlyList<byte> Data = Array.Empty<byte>();

        public NetworkPacketData(NetworkMonitorWidget widget, ushort opCode, NetworkMessageDirection direction, uint sourceActorId, uint targetActorId, nint dataPtr)
            : this(opCode, direction, sourceActorId, targetActorId)
            => this.Data = MemoryHelper.Read<byte>(dataPtr, widget.GetSizeFromOpCode(opCode), false);
    }
}
