using Dalamud.Bindings.ImPlot;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImPlots drag and drop target. </summary>
    public ref struct PlotDragDropTargetDisposable : IDisposable
    {
        /// <summary> Whether creating the drag and drop target succeeded. This needs to be checked before calling any of the member methods. </summary>
        public readonly bool Success;

        /// <summary> Gets a value indicating whether the drag and drop target is already ended. </summary>
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

        public static implicit operator bool(PlotDragDropTargetDisposable value)
            => value.Success;

        public static bool operator true(PlotDragDropTargetDisposable i)
            => i.Success;

        public static bool operator false(PlotDragDropTargetDisposable i)
            => !i.Success;

        public static bool operator !(PlotDragDropTargetDisposable i)
            => !i.Success;

        public static bool operator &(PlotDragDropTargetDisposable i, bool value)
            => i.Success && value;

        public static bool operator |(PlotDragDropTargetDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the drag and drop target on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImPlot.EndDragDropTarget();
            this.Alive = false;
        }

#pragma warning disable SA1204
        /// <summary> End a drag and drop target without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImPlot.EndDragDropTarget();
#pragma warning restore SA1204
    }
}
