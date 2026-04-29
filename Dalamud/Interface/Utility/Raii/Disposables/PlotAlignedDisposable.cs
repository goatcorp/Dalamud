using Dalamud.Bindings.ImPlot;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImPlots aligned plot. </summary>
    public ref struct PlotAlignedDisposable : IDisposable
    {
        /// <summary> Whether creating the aligned plot succeeded. This needs to be checked before calling any of the member methods. </summary>
        public readonly bool Success;

        /// <summary> Gets a value indicating whether the aligned plot is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="PlotAlignedDisposable"/> struct. </summary>
        /// <param name="groupId"> The ID of the plot as text. </param>
        /// <param name="vertical"> Whether the plot should be vertical. </param>
        internal PlotAlignedDisposable(string groupId, bool vertical = true)
        {
            this.Success = ImPlot.BeginAlignedPlots(groupId, vertical);
            this.Alive   = true;
        }

        /// <inheritdoc cref="PlotAlignedDisposable(string,bool)"/>
        internal PlotAlignedDisposable(ReadOnlySpan<byte> groupId, bool vertical = true)
        {
            this.Success = ImPlot.BeginAlignedPlots(groupId, vertical);
            this.Alive   = true;
        }

        public static implicit operator bool(PlotAlignedDisposable value)
            => value.Success;

        public static bool operator true(PlotAlignedDisposable i)
            => i.Success;

        public static bool operator false(PlotAlignedDisposable i)
            => !i.Success;

        public static bool operator !(PlotAlignedDisposable i)
            => !i.Success;

        public static bool operator &(PlotAlignedDisposable i, bool value)
            => i.Success && value;

        public static bool operator |(PlotAlignedDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the aligned plot on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImPlot.EndAlignedPlots();
            this.Alive = false;
        }

#pragma warning disable SA1204
        /// <summary> End an aligned Plot without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImPlot.EndAlignedPlots();
#pragma warning restore SA1204
    }
}
