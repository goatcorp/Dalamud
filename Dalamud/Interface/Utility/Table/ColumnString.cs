using System.Text.RegularExpressions;

using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace Dalamud.Interface.Utility.Table;

public class ColumnString<TItem> : Column<TItem>
{
    public ColumnString()
        => this.Flags &= ~ImGuiTableColumnFlags.NoResize;

    public    string FilterValue = string.Empty;
    protected Regex? filterRegex;

    public virtual string ToName(TItem item)
        => item!.ToString() ?? string.Empty;

    public override int Compare(TItem lhs, TItem rhs)
        => string.Compare(this.ToName(lhs), this.ToName(rhs), StringComparison.InvariantCulture);

    public override bool DrawFilter()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);

        ImGui.SetNextItemWidth(-Table.ArrowWidth * ImGuiHelpers.GlobalScale);
        var tmp = this.FilterValue;
        if (!ImGui.InputTextWithHint(this.FilterLabel, this.Label, ref tmp, 256) || tmp == this.FilterValue)
            return false;

        this.FilterValue = tmp;
        try
        {
            this.filterRegex = new Regex(this.FilterValue, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch
        {
            this.filterRegex = null;
        }

        return true;
    }

    public override bool FilterFunc(TItem item)
    {
        var name = this.ToName(item);
        if (this.FilterValue.Length == 0)
            return true;

        return this.filterRegex?.IsMatch(name) ?? name.Contains(this.FilterValue, StringComparison.OrdinalIgnoreCase);
    }

    public override void DrawColumn(TItem item, int idx)
    {
        ImGui.TextUnformatted(this.ToName(item));
    }
}
