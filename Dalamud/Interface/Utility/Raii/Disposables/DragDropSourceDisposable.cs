// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around a ImGui Drag and Drop Source. </summary>
    public ref struct DragDropSourceDisposable : IDisposable
    {
        /// <summary> Whether creating the drag and drop source succeeded. </summary>
        public readonly bool Success;

        /// <summary> Whether the drag and drop source is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="DragDropSourceDisposable"/> struct. </summary>
        /// <returns> A disposable object that indicates whether the source is active. Use with using. </returns>
        public DragDropSourceDisposable()
        {
            this.Success = ImGui.BeginDragDropSource();
            this.Alive   = true;
        }

        /// <summary>Initializes a new instance of the <see cref="DragDropSourceDisposable"/> struct. </summary>
        /// <param name="flags"> Additional flags to control the drag and drop behaviour. </param>
        /// <returns> A disposable object that indicates whether the source is active. Use with using. </returns>
        internal DragDropSourceDisposable(ImGuiDragDropFlags flags)
        {
            this.Success = ImGui.BeginDragDropSource(flags);
            this.Alive   = true;
        }

        /// <summary> Conversion to bool. </summary>
        public static implicit operator bool(DragDropSourceDisposable value)
            => value.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator true(DragDropSourceDisposable i)
            => i.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator false(DragDropSourceDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on NOT operators. </summary>
        public static bool operator !(DragDropSourceDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on AND operators. </summary>
        public static bool operator &(DragDropSourceDisposable i, bool value)
            => i.Success && value;

        /// <summary> Conversion to bool on OR operators. </summary>
        public static bool operator |(DragDropSourceDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the drag and drop source on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImGui.EndDragDropSource();
            this.Alive = false;
        }

        /// <summary> End a drag and drop source without using an IDisposable.</summary>
        public static void EndUnsafe()
            => ImGui.EndDragDropSource();
    }
}
