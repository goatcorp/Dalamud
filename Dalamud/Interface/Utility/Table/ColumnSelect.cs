using System.Collections.Generic;

using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace Dalamud.Interface.Utility.Table;

public class ColumnSelect<T, TItem> : Column<TItem> where T : struct, Enum, IEquatable<T>
{
    public ColumnSelect(T initialValue)
        => this.FilterValue = initialValue;

    protected virtual IReadOnlyList<T> Values
        => Enum.GetValues<T>();

    protected virtual string[] Names
        => Enum.GetNames<T>();

    protected virtual void SetValue(T value)
        => this.FilterValue = value;

    public    T   FilterValue;
    protected int idx = -1;

    public override bool DrawFilter()
    {
        using var id    = ImRaii.PushId(this.FilterLabel);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);
        ImGui.SetNextItemWidth(-Table.ArrowWidth * ImGuiHelpers.GlobalScale);
        using var combo = ImRaii.Combo(string.Empty, this.idx < 0 ? this.Label : this.Names[this.idx]);
        if (!combo)
            return false;

        var       ret = false;
        for (var i = 0; i < this.Names.Length; ++i)
        {
            if (this.FilterValue.Equals(this.Values[i]))
                this.idx = i;
            if (!ImGui.Selectable(this.Names[i], this.idx == i) || this.idx == i)
                continue;

            this.idx = i;
            this.SetValue(this.Values[i]);
            ret = true;
        }

        return ret;
    }
}
