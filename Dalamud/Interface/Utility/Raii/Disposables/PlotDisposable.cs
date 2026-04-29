using System.Numerics;

using Dalamud.Bindings.ImPlot;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImPlots plot. </summary>
    public ref struct PlotDisposable : IDisposable
    {
        /// <summary> Whether creating the plot succeeded. This needs to be checked before calling any of the member methods. </summary>
        public readonly bool Success;

        /// <summary> Gets a value indicating whether the plot is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="PlotDisposable"/> struct. </summary>
        /// <param name="titleId"> The ID of the plot as text. </param>
        /// <param name="size"> The desired size of the plot. </param>
        /// <param name="flags"> Additional flags for the plot. </param>
        internal PlotDisposable(string titleId, Vector2 size, ImPlotFlags flags)
        {
            this.Success = ImPlot.BeginPlot(titleId, size, flags);
            this.Alive   = true;
        }

        /// <inheritdoc cref="PlotDisposable(string,Vector2,ImPlotFlags)"/>
        internal PlotDisposable(ReadOnlySpan<byte> titleId, Vector2 size, ImPlotFlags flags)
        {
            this.Success = ImPlot.BeginPlot(titleId, size, flags);
            this.Alive   = true;
        }

        public static implicit operator bool(PlotDisposable value)
            => value.Success;

        public static bool operator true(PlotDisposable i)
            => i.Success;

        public static bool operator false(PlotDisposable i)
            => !i.Success;

        public static bool operator !(PlotDisposable i)
            => !i.Success;

        public static bool operator &(PlotDisposable i, bool value)
            => i.Success && value;

        public static bool operator |(PlotDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the plot on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImPlot.EndPlot();
            this.Alive = false;
        }

#pragma warning disable SA1204
        /// <summary> End a plot without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImPlot.EndPlot();
#pragma warning restore SA1204
    }
}
