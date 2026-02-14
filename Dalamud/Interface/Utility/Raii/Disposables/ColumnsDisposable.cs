// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around creating pre-table style column separation. </summary>
    public ref struct ColumnsDisposable : IDisposable
    {
        /// <summary> The columns before pushing this to revert to. </summary>
        public readonly int LastColumns;

        /// <summary> Get the current number of columns. </summary>
        public int Count
            => ImGui.GetColumnsCount();

        /// <summary> Get the index of the current column. </summary>
        public int Current
            => ImGui.GetColumnIndex();

        /// <summary> Move to the next column. </summary>
        public void Next()
            => ImGui.NextColumn();

        /// <summary> Get or set the offset of the current column. </summary>
        public float Offset
        {
            get => ImGui.GetColumnOffset(Current);
            set => ImGui.SetColumnOffset(Current, value);
        }

        /// <summary> Get the offset of a column by index. </summary>
        public float GetOffset(int index)
            => ImGui.GetColumnOffset(index);

        /// <summary> Set the offset of a column by index. </summary>
        public void SetOffset(int index, float value)
            => ImGui.SetColumnOffset(index, value);

        /// <summary> Get or set the width of the current column. </summary>
        public float Width
        {
            get => ImGui.GetColumnWidth(Current);
            set => ImGui.SetColumnWidth(Current, value);
        }

        /// <summary> Get the width of a column by index. </summary>
        public float GetWidth(int index)
            => ImGui.GetColumnWidth(index);

        /// <summary> Set the width of a column by index. </summary>
        public void SetWidth(int index, float width)
            => ImGui.SetColumnWidth(index, width);

        /// <summary> Create a new column separation. </summary>
        /// <param name="count"> The number of columns to separate. </param>
        /// <param name="id"> An ID for the separation. If this is a UTF8 string, it HAS to be null-terminated. </param>
        /// <param name="border"> Whether the columns should be separated by borders. </param>
        /// <remarks> The columns system is outdated. Prefer to use <see cref="Table"/> instead. </remarks>
        public ColumnsDisposable(int count, ImU8String id, bool border = false)
        {
            this.LastColumns = this.Count;
            ImGui.Columns(count, id, border);
        }

        /// <summary> Revert to the prior number of columns. </summary>
        public void Dispose()
            => ImGui.Columns(Math.Max(this.LastColumns, 1));
    }
}
