using ImGuiNET;

namespace Dalamud.Interface.Utility.Table;

public class Column<TItem>
{
    public string                Label = string.Empty;
    public ImGuiTableColumnFlags Flags = ImGuiTableColumnFlags.NoResize;

    public virtual float Width
        => -1f;

    public string FilterLabel
        => $"##{this.Label}Filter";

    public virtual bool DrawFilter()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(this.Label);
        return false;
    }

    public virtual bool FilterFunc(TItem item)
        => true;

    public virtual int Compare(TItem lhs, TItem rhs)
        => 0;

    public virtual void DrawColumn(TItem item, int idx)
    {
    }

    public int CompareInv(TItem lhs, TItem rhs)
        => this.Compare(rhs, lhs);
}
