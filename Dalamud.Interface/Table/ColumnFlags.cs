using ImGuiNET;
using ImRaii = Dalamud.Interface.Raii.ImRaii;

namespace Dalamud.Interface.Table;

public class ColumnFlags<T, TItem> : Column<TItem> where T : struct, Enum
{
    public T AllFlags = default;

    protected virtual IReadOnlyList<T> Values
        => Enum.GetValues<T>();

    protected virtual string[] Names
        => Enum.GetNames<T>();

    public virtual T FilterValue
        => default;

    protected virtual void SetValue(T value, bool enable)
    { }

    public override bool DrawFilter()
    {
        using var id    = ImRaii.PushId(this.FilterLabel);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);
        ImGui.SetNextItemWidth(-Table.ArrowWidth * InterfaceHelpers.GlobalScale);
        var       all   = this.FilterValue.HasFlag(this.AllFlags);
        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, 0x803030A0, !all);
        using var combo = ImRaii.Combo(string.Empty, this.Label, ImGuiComboFlags.NoArrowButton);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            this.SetValue(this.AllFlags, true);
            return true;
        }

        if (!all && ImGui.IsItemHovered())
            ImGui.SetTooltip("Right-click to clear filters.");

        if (!combo)
            return false;

        color.Pop();

        var ret = false;
        if (ImGui.Checkbox("Enable All", ref all))
        {
            this.SetValue(this.AllFlags, all);
            ret = true;
        }

        using var indent = ImRaii.PushIndent(10f);
        for (var i = 0; i < this.Names.Length; ++i)
        {
            var tmp = this.FilterValue.HasFlag(this.Values[i]);
            if (!ImGui.Checkbox(this.Names[i], ref tmp))
                continue;

            this.SetValue(this.Values[i], tmp);
            ret = true;
        }

        return ret;
    }
}
