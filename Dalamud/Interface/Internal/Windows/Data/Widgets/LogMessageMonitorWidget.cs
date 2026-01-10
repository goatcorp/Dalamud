using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Chat;
using Dalamud.Game.Gui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

using Lumina.Text.ReadOnly;

using ImGuiTable = Dalamud.Interface.Utility.ImGuiTable;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget to display the LogMessages.
/// </summary>
internal class LogMessageMonitorWidget : IDataWindowWidget
{
    private readonly ConcurrentQueue<LogMessageData> messages = new();

    private bool trackMessages;
    private int trackedMessages;
    private Regex? filterRegex;
    private string filterString = string.Empty;
    
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["logmessage"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "LogMessage Monitor";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.trackMessages = false;
        this.trackedMessages = 20;
        this.filterRegex = null;
        this.filterString = string.Empty;
        this.messages.Clear();
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var network = Service<ChatGui>.Get();
        if (ImGui.Checkbox("Track LogMessages"u8, ref this.trackMessages))
        {
            if (this.trackMessages)
            {
                network.LogMessage += this.OnLogMessage;
            }
            else
            {
                network.LogMessage -= this.OnLogMessage;
            }
        }

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
        if (ImGui.DragInt("Stored Number of Messages"u8, ref this.trackedMessages, 0.1f, 1, 512))
        {
            this.trackedMessages = Math.Clamp(this.trackedMessages, 1, 512);
        }

        if (ImGui.Button("Clear Stored Messages"u8))
        {
            this.messages.Clear();
        }

        this.DrawFilterInput();

        ImGuiTable.DrawTable(string.Empty, this.messages.Where(m => this.filterRegex == null || this.filterRegex.IsMatch(m.Formatted.ExtractText())), this.DrawNetworkPacket, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp, "LogMessageId", "Source", "Target", "Parameters", "Formatted");
    }

    private void DrawNetworkPacket(LogMessageData data)
    {
        ImGui.TableNextColumn();
        ImGui.Text(data.LogMessageId.ToString());

        ImGui.TableNextColumn();
        ImGuiHelpers.SeStringWrapped(data.Source);

        ImGui.TableNextColumn();
        ImGuiHelpers.SeStringWrapped(data.Target);

        ImGui.TableNextColumn();
        ImGui.Text(data.Parameters);

        ImGui.TableNextColumn();
        ImGuiHelpers.SeStringWrapped(data.Formatted);
    }

    private void DrawFilterInput()
    {
        var invalidRegEx = this.filterString.Length > 0 && this.filterRegex == null;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 2 * ImGuiHelpers.GlobalScale, invalidRegEx);
        using var color = ImRaii.PushColor(ImGuiCol.Border, 0xFF0000FF, invalidRegEx);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (!ImGui.InputTextWithHint("##Filter"u8, "Regex Filter..."u8, ref this.filterString, 1024))
        {
            return;
        }

        if (this.filterString.Length == 0)
        {
            this.filterRegex = null;
        }
        else
        {
            try
            {
                this.filterRegex = new Regex(this.filterString, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
            }
            catch
            {
                this.filterRegex = null;
            }
        }
    }

    private void OnLogMessage(ILogMessage message)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartArray();
        for (var i = 0; i < message.ParameterCount; i++)
        {
            if (message.TryGetStringParameter(i, out var str))
                writer.WriteStringValue(str.ExtractText());
            else if (message.TryGetIntParameter(i, out var num))
                writer.WriteNumberValue(num);
            else
                writer.WriteNullValue();
        }

        writer.WriteEndArray();
        writer.Flush();

        this.messages.Enqueue(new LogMessageData(message.LogMessageId, message.SourceEntity?.Name ?? default, message.TargetEntity?.Name ?? default, buffer.WrittenMemory, message.FormatLogMessageForDebugging()));
        while (this.messages.Count > this.trackedMessages)
        {
            this.messages.TryDequeue(out _);
        }
    }

    private readonly record struct LogMessageData(uint LogMessageId, ReadOnlySeString Source, ReadOnlySeString Target, ReadOnlyMemory<byte> Parameters, ReadOnlySeString Formatted);
}
