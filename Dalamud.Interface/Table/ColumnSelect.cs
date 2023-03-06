using ImGuiNET;
using ImRaii = Dalamud.Interface.Raii.ImRaii;

namespace Dalamud.Interface.Table;

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
    protected int Idx = -1;

    public override bool DrawFilter()
    {
        using var id    = ImRaii.PushId(this.FilterLabel);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);
        ImGui.SetNextItemWidth(-Table.ArrowWidth * InterfaceHelpers.GlobalScale);
        using var combo = ImRaii.Combo(string.Empty, this.Idx < 0 ? this.Label : this.Names[this.Idx]);
        if(!combo)
            return false;

        var       ret = false;
        for (var i = 0; i < this.Names.Length; ++i)
        {
            if (this.FilterValue.Equals(this.Values[i]))
                this.Idx = i;
            if (!ImGui.Selectable(this.Names[i], this.Idx == i) || this.Idx == i)
                continue;

            this.Idx = i;
            this.SetValue(this.Values[i]);
            ret = true;
        }

        return ret;
    }
}
