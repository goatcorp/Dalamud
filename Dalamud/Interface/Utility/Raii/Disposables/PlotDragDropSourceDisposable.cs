using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImPlots drag and drop source. </summary>
    public ref struct PlotDragDropSourceDisposable : IDisposable
    {
        /// <summary> Whether creating the drag and drop source succeeded. This needs to be checked before calling any of the member methods. </summary>
        public readonly bool Success;

        /// <summary> Gets a value indicating whether drag and drop source is already ended. </summary>
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

        public static implicit operator bool(PlotDragDropSourceDisposable value)
            => value.Success;

        public static bool operator true(PlotDragDropSourceDisposable i)
            => i.Success;

        public static bool operator false(PlotDragDropSourceDisposable i)
            => !i.Success;

        public static bool operator !(PlotDragDropSourceDisposable i)
            => !i.Success;

        public static bool operator &(PlotDragDropSourceDisposable i, bool value)
            => i.Success && value;

        public static bool operator |(PlotDragDropSourceDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the drag and drop source on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImPlot.EndDragDropSource();
            this.Alive = false;
        }

#pragma warning disable SA1204
        /// <summary> End a drag and drop source without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImPlot.EndDragDropSource();
#pragma warning restore SA1204
    }
}
