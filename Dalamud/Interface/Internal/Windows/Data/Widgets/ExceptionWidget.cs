using System.Drawing;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Logging;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Data widget for easier browsing of plugin exceptions.
/// </summary>
public class ExceptionWidget : IDataWindowWidget
{
    private LocalPlugin? selectedPlugin;
    private PluginExceptionEntry? selectedException;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["exception", "ex", "error"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Exception Browser";

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
        if (this.selectedPlugin is { IsLoaded: false })
        {
            this.selectedPlugin = null;
            this.selectedException = null;
        }

        this.DrawPluginSelectCombo();

        if (this.selectedPlugin is null)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, KnownColor.Orange.Vector()))
            {
                ImGuiHelpers.CenteredText("Select a Plugin form the Combo Above");
            }

            return;
        }

        using var table = ImRaii.Table("##SectionTable", 2, ImGuiTableFlags.Resizable, outerSize: ImGui.GetContentRegionAvail());
        if (!table) return;

        ImGui.TableSetupColumn("##ListBoxColumn", ImGuiTableColumnFlags.WidthStretch, 3);
        ImGui.TableSetupColumn("##ExceptionColumn", ImGuiTableColumnFlags.WidthStretch, 11);

        ImGui.TableNextColumn();
        this.DrawExceptionSelectList();

        ImGui.TableNextColumn();
        this.DrawExceptionInformation();
    }

    private void DrawPluginSelectCombo()
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        using var combo = ImRaii.Combo("##PluginSelectCombo", this.selectedPlugin?.Name ?? "Plugin Not Selected", ImGuiComboFlags.HeightLargest);
        if (!combo) return;

        var options = ScopedPluginLogService.PluginExceptionEntries.Keys;

        if (options.Count is 0)
        {
            ImGui.Text("No Plugins Have Logged Exceptions.");
            return;
        }

        foreach (var option in ScopedPluginLogService.PluginExceptionEntries.Keys)
        {
            if (ImGui.Selectable(option.Name, this.selectedPlugin == option))
            {
                this.selectedPlugin = option;
            }
        }
    }

    private void DrawExceptionSelectList()
    {
        if (this.selectedPlugin is null)
        {
            return;
        }

        using var listBox = ImRaii.ListBox("##ExceptionSelectList", ImGui.GetContentRegionAvail() - new Vector2(0.0f, ImGui.GetStyle().FramePadding.Y));
        if (!listBox) return;

        if (!ScopedPluginLogService.PluginExceptionEntries.TryGetValue(this.selectedPlugin, out var exceptions))
        {
            return;
        }

        foreach (var (index, exceptionEntry) in exceptions.Index().Reverse())
        {
            var exceptionTypeName = exceptionEntry.Exception.GetType().Name;
            var exceptionTimeString = exceptionEntry.Timestamp.ToLocalTime().ToString("G");

            var entryHeight = ImGui.CalcTextSize(exceptionTypeName).Y + ImGui.CalcTextSize(exceptionTimeString).Y + ImGui.GetStyle().ItemSpacing.Y;

            var cursorPosition = ImGui.GetCursorPosY();
            if (ImGui.Selectable($"##{exceptionTypeName}{index}", this.selectedException == exceptionEntry, size: new Vector2(ImGui.GetContentRegionAvail().X, entryHeight)))
            {
                this.selectedException = exceptionEntry;
            }

            ImGui.SetCursorPosY(cursorPosition);
            ImGui.Text(exceptionTypeName);
            ImGui.TextColored(KnownColor.Gray.Vector(), exceptionTimeString);
        }
    }

    private void DrawExceptionInformation()
    {
        using var frameChild = ImRaii.Child("ExceptionFrame", ImGui.GetContentRegionAvail() - new Vector2(0.0f, ImGui.GetStyle().FramePadding.Y));
        if (!frameChild) return;

        if (this.selectedException is null)
        {
            const string labelText = "Select an Exception on the left";
            var textSize = ImGui.CalcTextSize(labelText);

            ImGui.SetCursorPos((ImGui.GetContentRegionAvail() / 2.0f) - (textSize / 2.0f));
            ImGui.Text(labelText);
            return;
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Text(this.selectedException.Exception.Message);

        const string buttonText = "Copy to Clipboard";
        var buttonWidth = ImGui.CalcTextSize(buttonText).X + (ImGui.GetStyle().FramePadding.X * 2.0f);

        ImGui.SameLine(ImGui.GetContentRegionMax().X - buttonWidth);
        if (ImGui.Button(buttonText))
        {
            ImGui.SetClipboardText(this.selectedException.Exception.ToString());
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (10.0f * ImGuiHelpers.GlobalScale));
        using var child = ImRaii.Child("ExceptionChild", ImGui.GetContentRegionAvail());
        if (!child) return;

        ImGui.TextWrapped(this.selectedException.Exception.ToString());
    }
}
