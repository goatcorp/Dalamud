// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui tables. </summary>
    public ref struct PlotLegendDisposable : IDisposable
    {
        /// <summary> Whether creating the table succeeded. This needs to be checked before calling any of the member methods. </summary>
        public readonly bool Success;

        /// <summary> Whether the table is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="PlotLegendDisposable"/> struct. </summary>
        /// <param name="labelId"> The ID of the plot as text. </param>
        /// <param name="mouseButton"> The mouse button to use for opening the legend popup. </param>
        internal PlotLegendDisposable(string labelId, ImGuiMouseButton mouseButton = ImGuiMouseButton.Right)
        {
            this.Success = ImPlot.BeginLegendPopup(labelId, mouseButton);
            this.Alive   = true;
        }

        /// <inheritdoc cref="PlotLegendDisposable(string,ImGuiMouseButton)"/>
        internal PlotLegendDisposable(ReadOnlySpan<byte> labelId, ImGuiMouseButton mouseButton = ImGuiMouseButton.Right)
        {
            this.Success = ImPlot.BeginLegendPopup(labelId, mouseButton);
            this.Alive   = true;
        }

        /// <summary> Conversion to bool. </summary>
        public static implicit operator bool(PlotLegendDisposable value)
            => value.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator true(PlotLegendDisposable i)
            => i.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator false(PlotLegendDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on NOT operators. </summary>
        public static bool operator !(PlotLegendDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on AND operators. </summary>
        public static bool operator &(PlotLegendDisposable i, bool value)
            => i.Success && value;

        /// <summary> Conversion to bool on OR operators. </summary>
        public static bool operator |(PlotLegendDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the Table on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImPlot.EndLegendPopup();
            this.Alive = false;
        }

        /// <summary> End a Table without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImPlot.EndLegendPopup();
    }
}
