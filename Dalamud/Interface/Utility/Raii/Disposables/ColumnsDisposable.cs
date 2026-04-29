using Dalamud.Bindings.ImGui;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around creating pre-table style column separation. </summary>
    public ref struct ColumnsDisposable : IDisposable
    {
        /// <summary> The columns before pushing this to revert to. </summary>
        public readonly int LastColumns;

        /// <summary> Gets the current number of columns. </summary>
        public int Count
            => ImGui.GetColumnsCount();

        /// <summary> Gets the index of the current column. </summary>
        public int Current
            => ImGui.GetColumnIndex();

        /// <summary> Move to the next column. </summary>
        public void Next()
            => ImGui.NextColumn();

        /// <summary> Gets or sets the offset of the current column. </summary>
        public float Offset
        {
            get => ImGui.GetColumnOffset(this.Current);
            set => ImGui.SetColumnOffset(this.Current, value);
        }

        public float GetOffset(int index)
            => ImGui.GetColumnOffset(index);

        public void SetOffset(int index, float value)
            => ImGui.SetColumnOffset(index, value);

        /// <summary> Gets or sets the width of the current column. </summary>
        public float Width
        {
            get => ImGui.GetColumnWidth(this.Current);
            set => ImGui.SetColumnWidth(this.Current, value);
        }

        public float GetWidth(int index)
            => ImGui.GetColumnWidth(index);

        public void SetWidth(int index, float width)
            => ImGui.SetColumnWidth(index, width);

        /// <summary>Initializes a new instance of the <see cref="ColumnsDisposable"/> struct. </summary>
        /// <param name="count"> The number of columns to separate. </param>
        /// <param name="id"> An ID for the separation. </param>
        /// <param name="border"> Whether the columns should be separated by borders. </param>
        /// <remarks> The columns system is outdated. Prefer to use <see cref="Table(ImU8String,int)"/> instead. </remarks>
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
