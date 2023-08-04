using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Utility.Table;

public static class Table
{
    public const float ArrowWidth = 10;
}

public class Table<T>
{
    protected readonly ICollection<T> Items;
    internal readonly List<(T, int)> FilteredItems;

    protected readonly string Label;
    protected readonly Column<T>[] Headers;

    protected bool filterDirty = true;
    protected bool sortDirty = true;

    protected float ItemHeight { get; set; }

    public float ExtraHeight { get; set; } = 0;

    private int currentIdx = 0;

    protected bool Sortable
    {
        get => this.Flags.HasFlag(ImGuiTableFlags.Sortable);
        set => this.Flags = value ? this.Flags | ImGuiTableFlags.Sortable : this.Flags & ~ImGuiTableFlags.Sortable;
    }

    protected int sortIdx = -1;

    public ImGuiTableFlags Flags = ImGuiTableFlags.RowBg
      | ImGuiTableFlags.Sortable
      | ImGuiTableFlags.BordersOuter
      | ImGuiTableFlags.ScrollY
      | ImGuiTableFlags.ScrollX
      | ImGuiTableFlags.PreciseWidths
      | ImGuiTableFlags.BordersInnerV
      | ImGuiTableFlags.NoBordersInBodyUntilResize;

    public int TotalItems
        => this.Items.Count;

    public int CurrentItems
        => this.FilteredItems.Count;

    public int TotalColumns
        => this.Headers.Length;

    public int VisibleColumns { get; private set; }

    public Table(string label, ICollection<T> items, params Column<T>[] headers)
    {
        this.Label = label;
        this.Items = items;
        this.Headers = headers;
        this.FilteredItems = new List<(T, int)>(this.Items.Count);
        this.VisibleColumns = this.Headers.Length;
    }

    public void Draw(float itemHeight)
    {
        this.ItemHeight = itemHeight;
        using var id = ImRaii.PushId(this.Label);
        this.UpdateFilter();
        this.DrawTableInternal();
    }

    protected virtual void DrawFilters()
        => throw new NotImplementedException();

    protected virtual void PreDraw()
    {
    }

    private void SortInternal()
    {
        if (!this.Sortable)
            return;

        var sortSpecs = ImGui.TableGetSortSpecs();
        this.sortDirty |= sortSpecs.SpecsDirty;

        if (!this.sortDirty)
            return;

        this.sortIdx = sortSpecs.Specs.ColumnIndex;

        if (this.Headers.Length <= this.sortIdx)
            this.sortIdx = 0;

        if (sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending)
            this.FilteredItems.StableSort((a, b) => this.Headers[this.sortIdx].Compare(a.Item1, b.Item1));
        else if (sortSpecs.Specs.SortDirection == ImGuiSortDirection.Descending)
            this.FilteredItems.StableSort((a, b) => this.Headers[this.sortIdx].CompareInv(a.Item1, b.Item1));
        else
            this.sortIdx = -1;

        this.sortDirty = false;
        sortSpecs.SpecsDirty = false;
    }

    private void UpdateFilter()
    {
        if (!this.filterDirty)
            return;

        this.FilteredItems.Clear();
        var idx = 0;
        foreach (var item in this.Items)
        {
            if (this.Headers.All(header => header.FilterFunc(item)))
                this.FilteredItems.Add((item, idx));
            idx++;
        }

        this.filterDirty = false;
        this.sortDirty = true;
    }

    private void DrawItem((T Item, int Index) pair)
    {
        var column = 0;
        using var id = ImRaii.PushId(this.currentIdx);
        this.currentIdx = pair.Index;
        foreach (var header in this.Headers)
        {
            id.Push(column++);
            if (ImGui.TableNextColumn())
                header.DrawColumn(pair.Item, pair.Index);
            id.Pop();
        }
    }

    private void DrawTableInternal()
    {
        using var table = ImRaii.Table("Table", this.Headers.Length, this.Flags,
            ImGui.GetContentRegionAvail() - this.ExtraHeight * Vector2.UnitY * ImGuiHelpers.GlobalScale);
        if (!table)
            return;

        this.PreDraw();
        ImGui.TableSetupScrollFreeze(1, 1);

        foreach (var header in this.Headers)
            ImGui.TableSetupColumn(header.Label, header.Flags, header.Width);

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
        var i = 0;
        this.VisibleColumns = 0;
        foreach (var header in this.Headers)
        {
            using var id = ImRaii.PushId(i);
            if (ImGui.TableGetColumnFlags(i).HasFlag(ImGuiTableColumnFlags.IsEnabled))
                ++this.VisibleColumns;
            if (!ImGui.TableSetColumnIndex(i++))
                continue;

            using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            ImGui.TableHeader(string.Empty);
            ImGui.SameLine();
            style.Pop();
            if (header.DrawFilter())
                this.filterDirty = true;
        }

        this.SortInternal();
        this.currentIdx = 0;
        ImGuiClip.ClippedDraw(this.FilteredItems, this.DrawItem, this.ItemHeight);
    }
}
