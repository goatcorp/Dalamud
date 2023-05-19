using Dalamud.Interface.Raii;
using ImGuiNET;

namespace Dalamud.Interface;

public static class ImGuiTable
{
    // Draw a simple table with the given data using the drawRow action.
    // Headers and thus columns and column count are defined by columnTitles.
    public static void DrawTable<T>(string label, IEnumerable<T> data, Action<T> drawRow, ImGuiTableFlags flags = ImGuiTableFlags.None,
        params string[] columnTitles)
    {
        if (columnTitles.Length == 0)
            return;

        using var table = ImRaii.Table(label, columnTitles.Length, flags);
        if (!table)
            return;

        foreach (var title in columnTitles)
        {
            ImGui.TableNextColumn();
            ImGui.TableHeader(title);
        }

        foreach (var datum in data)
        {
            ImGui.TableNextRow();
            drawRow(datum);
        }
    }

    // Draw a simple table with the given data using the drawRow action inside a collapsing header.
    // Headers and thus columns and column count are defined by columnTitles.
    public static void DrawTabbedTable<T>(string label, IEnumerable<T> data, Action<T> drawRow, ImGuiTableFlags flags = ImGuiTableFlags.None,
        params string[] columnTitles)
    {
        if (ImGui.CollapsingHeader(label))
            DrawTable($"{label}##Table", data, drawRow, flags, columnTitles);
    }
}
