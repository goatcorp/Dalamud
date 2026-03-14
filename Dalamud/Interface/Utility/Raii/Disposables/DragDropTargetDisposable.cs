// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around a ImGui Drag and Drop Target. </summary>
    public ref struct DragDropTargetDisposable : IDisposable
    {
        /// <summary> Whether creating the drag and drop target succeeded. </summary>
        public readonly bool Success;

        /// <summary> Gets a value indicating whether the drag and drop target is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="DragDropTargetDisposable"/> struct. </summary>
        /// <returns> A disposable object that indicates whether the target is active. Use with using. </returns>
        public DragDropTargetDisposable()
        {
            this.Success = ImGui.BeginDragDropTarget();
            this.Alive   = true;
        }

        /// <summary> Conversion to bool. </summary>
        public static implicit operator bool(DragDropTargetDisposable value)
            => value.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator true(DragDropTargetDisposable i)
            => i.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator false(DragDropTargetDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on NOT operators. </summary>
        public static bool operator !(DragDropTargetDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on AND operators. </summary>
        public static bool operator &(DragDropTargetDisposable i, bool value)
            => i.Success && value;

        /// <summary> Conversion to bool on OR operators. </summary>
        public static bool operator |(DragDropTargetDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the drag and drop target on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImGui.EndDragDropTarget();
            this.Alive = false;
        }

        /// <summary> End a drag and drop target without using an IDisposable.</summary>
        public static void EndUnsafe()
            => ImGui.EndDragDropTarget();
    }
}
