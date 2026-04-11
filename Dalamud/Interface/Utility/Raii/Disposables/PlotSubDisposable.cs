// ReSharper disable once CheckNamespace

using System.Numerics;

using Dalamud.Bindings.ImPlot;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui tables. </summary>
    public ref struct PlotSubDisposable : IDisposable
    {
        /// <summary> Whether creating the table succeeded. This needs to be checked before calling any of the member methods. </summary>
        public readonly bool Success;

        /// <summary> Whether the table is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="PlotSubDisposable"/> struct. </summary>
        /// <param name="titleId"> The ID of the plot as text. </param>
        /// <param name="rows"> The number of rows in the plot. </param>
        /// <param name="cols"> The number of columns in the plot. </param>
        /// <param name="size"> The desired size of the plot. </param>
        /// <param name="flags"> Additional flags for the plot. </param>
        internal PlotSubDisposable(string titleId, int rows, int cols, Vector2 size, ImPlotSubplotFlags flags = ImPlotSubplotFlags.None)
        {
            this.Success = ImPlot.BeginSubplots(titleId, rows, cols, size, flags);
            this.Alive   = true;
        }

        /// <inheritdoc cref="PlotSubDisposable(string,int,int,Vector2,ImPlotSubplotFlags)"/>
        internal PlotSubDisposable(ReadOnlySpan<byte> titleId, int rows, int cols, Vector2 size, ImPlotSubplotFlags flags = ImPlotSubplotFlags.None)
        {
            this.Success = ImPlot.BeginSubplots(titleId, rows, cols, size, flags);
            this.Alive   = true;
        }

        /// <summary>Initializes a new instance of the <see cref="PlotSubDisposable"/> struct. </summary>
        /// <param name="titleId"> The ID of the plot as text. </param>
        /// <param name="rows"> The number of rows in the plot. </param>
        /// <param name="cols"> The number of columns in the plot. </param>
        /// <param name="size"> The desired size of the plot. </param>
        /// <param name="flags"> Additional flags for the plot. </param>
        /// <param name="rowRatios"> The row ratios for the plot. </param>
        /// <param name="colRatios"> The column ratios for the plot. </param>
        internal PlotSubDisposable(string titleId, int rows, int cols, Vector2 size, ImPlotSubplotFlags flags, ref float rowRatios, ref float colRatios)
        {
            this.Success = ImPlot.BeginSubplots(titleId, rows, cols, size, flags, ref rowRatios, ref colRatios);
            this.Alive   = true;
        }

        /// <inheritdoc cref="PlotSubDisposable(string,int,int,Vector2,ImPlotSubplotFlags,ref float,ref float)"/>
        internal PlotSubDisposable(ReadOnlySpan<byte> titleId, int rows, int cols, Vector2 size, ImPlotSubplotFlags flags, ref float rowRatios, ref float colRatios)
        {
            this.Success = ImPlot.BeginSubplots(titleId, rows, cols, size, flags, ref rowRatios, ref colRatios);
            this.Alive   = true;
        }

        /// <summary> Conversion to bool. </summary>
        public static implicit operator bool(PlotSubDisposable value)
            => value.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator true(PlotSubDisposable i)
            => i.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator false(PlotSubDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on NOT operators. </summary>
        public static bool operator !(PlotSubDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on AND operators. </summary>
        public static bool operator &(PlotSubDisposable i, bool value)
            => i.Success && value;

        /// <summary> Conversion to bool on OR operators. </summary>
        public static bool operator |(PlotSubDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the Table on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImPlot.EndSubplots();
            this.Alive = false;
        }

        /// <summary> End a Table without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImPlot.EndSubplots();
    }
}
