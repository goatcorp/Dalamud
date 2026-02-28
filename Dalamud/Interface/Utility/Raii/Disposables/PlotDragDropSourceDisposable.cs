// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui tables. </summary>
    public ref struct PlotDragDropSourceDisposable : IDisposable
    {
        /// <summary> Whether creating the table succeeded. This needs to be checked before calling any of the member methods. </summary>
        public readonly bool Success;

        /// <summary> Whether the table is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="PlotDragDropSourceDisposable"/> struct. </summary>
        /// <param name="labelId"> The ID of the plot as text. </param>
        /// <param name="flags"> Additional flags to control the drag and drop behaviour. </param>
        internal PlotDragDropSourceDisposable(string labelId, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        {
            this.Success = ImPlot.BeginDragDropSourceItem(labelId, flags);
            this.Alive   = true;
        }

        /// <inheritdoc cref="PlotDragDropSourceDisposable(string,ImGuiDragDropFlags)"/>
        internal PlotDragDropSourceDisposable(ReadOnlySpan<byte> labelId, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        {
            this.Success = ImPlot.BeginDragDropSourceItem(labelId, flags);
            this.Alive   = true;
        }

        /// <summary> Initialize a new instance of the <see cref="PlotDragDropSourceDisposable"/> struct with SourceAxis. </summary>
        /// <param name="axis"> The axis to drag. </param>
        /// <param name="flags"> Additional flags to control the drag and drop behaviour. </param>
        /// <returns> A disposable object that indicates whether the source is active. Use with using. </returns>
        internal static PlotDragDropSourceDisposable AxisPlot(ImAxis axis, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
            => new(ImPlot.BeginDragDropSourceAxis(axis, flags));

        /// <summary> Initialize a new instance of the <see cref="PlotDragDropSourceDisposable"/> struct with SourcePlot. </summary>
        /// <param name="flags"> Additional flags to control the drag and drop behaviour. </param>
        /// <returns> A disposable object that indicates whether the source is active. Use with using. </returns>
        internal static PlotDragDropSourceDisposable SourcePlot(ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
            => new(ImPlot.BeginDragDropSourcePlot(flags));

        private PlotDragDropSourceDisposable(bool success)
        {
            this.Success = success;
            this.Alive   = true;
        }

        /// <summary> Conversion to bool. </summary>
        public static implicit operator bool(PlotDragDropSourceDisposable value)
            => value.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator true(PlotDragDropSourceDisposable i)
            => i.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator false(PlotDragDropSourceDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on NOT operators. </summary>
        public static bool operator !(PlotDragDropSourceDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on AND operators. </summary>
        public static bool operator &(PlotDragDropSourceDisposable i, bool value)
            => i.Success && value;

        /// <summary> Conversion to bool on OR operators. </summary>
        public static bool operator |(PlotDragDropSourceDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the Table on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImPlot.EndDragDropSource();
            this.Alive = false;
        }

        /// <summary> End a Table without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImPlot.EndDragDropSource();
    }
}
