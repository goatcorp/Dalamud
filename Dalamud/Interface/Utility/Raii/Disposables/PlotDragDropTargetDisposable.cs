// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui tables. </summary>
    public ref struct PlotDragDropTargetDisposable : IDisposable
    {
        /// <summary> Whether creating the table succeeded. This needs to be checked before calling any of the member methods. </summary>
        public readonly bool Success;

        /// <summary> Whether the table is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary> Initialize a new instance of the <see cref="PlotDragDropTargetDisposable"/> struct with SourceAxis. </summary>
        /// <param name="axis"> The axis to drag. </param>
        /// <returns> A disposable object that indicates whether the source is active. Use with using. </returns>
        internal static PlotDragDropTargetDisposable AxisPlot(ImAxis axis)
            => new(ImPlot.BeginDragDropTargetAxis(axis));

        /// <summary> Initialize a new instance of the <see cref="PlotDragDropTargetDisposable"/> struct with SourcePlot. </summary>
        /// <returns> A disposable object that indicates whether the source is active. Use with using. </returns>
        internal static PlotDragDropTargetDisposable LegendPlot()
            => new(ImPlot.BeginDragDropTargetLegend());

        /// <summary> Initialize a new instance of the <see cref="PlotDragDropTargetDisposable"/> struct with SourcePlot. </summary>
        /// <returns> A disposable object that indicates whether the source is active. Use with using. </returns>
        internal static PlotDragDropTargetDisposable SourcePlot()
            => new(ImPlot.BeginDragDropTargetPlot());

        private PlotDragDropTargetDisposable(bool success)
        {
            this.Success = success;
            this.Alive   = true;
        }

        /// <summary> Conversion to bool. </summary>
        public static implicit operator bool(PlotDragDropTargetDisposable value)
            => value.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator true(PlotDragDropTargetDisposable i)
            => i.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator false(PlotDragDropTargetDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on NOT operators. </summary>
        public static bool operator !(PlotDragDropTargetDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on AND operators. </summary>
        public static bool operator &(PlotDragDropTargetDisposable i, bool value)
            => i.Success && value;

        /// <summary> Conversion to bool on OR operators. </summary>
        public static bool operator |(PlotDragDropTargetDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the Table on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImPlot.EndDragDropTarget();
            this.Alive = false;
        }

        /// <summary> End a Table without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImPlot.EndDragDropTarget();
    }
}
