using System.Numerics;

using Dalamud.Bindings.ImGui;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui child frame. </summary>
    public ref struct ChildFrameDisposable : IDisposable
    {
        /// <summary> Whether creating the child frame succeeded and it is at least partly visible. </summary>
        public readonly bool Success;

        /// <summary> Gets a value indicating whether the child frame is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="ChildFrameDisposable"/> struct. </summary>
        /// <param name="id"> The ID of the child frame. </param>
        /// <param name="size"> The desired size of the child frame. </param>
        /// <returns> A disposable object that evaluates to true if any part of the begun child frame is currently visible. Use with using. </returns>
        internal ChildFrameDisposable(uint id, Vector2 size)
        {
            this.Success = ImGui.BeginChildFrame(id, size);
            this.Alive = true;
        }

        /// <summary>Initializes a new instance of the <see cref="ChildFrameDisposable"/> struct. </summary>
        /// <param name="id"> The ID of the child frame. </param>
        /// <param name="size"> The desired size of the child frame. </param>
        /// <param name="flags"> Additional flags for the child frame. </param>
        /// <returns> A disposable object that evaluates to true if any part of the begun child frame is currently visible. Use with using. </returns>
        internal ChildFrameDisposable(uint id, Vector2 size, ImGuiWindowFlags flags)
        {
            this.Success = ImGui.BeginChildFrame(id, size, flags);
            this.Alive = true;
        }

        public static implicit operator bool(ChildFrameDisposable value)
            => value.Success;

        public static bool operator true(ChildFrameDisposable i)
            => i.Success;

        public static bool operator false(ChildFrameDisposable i)
            => !i.Success;

        public static bool operator !(ChildFrameDisposable i)
            => !i.Success;

        public static bool operator &(ChildFrameDisposable i, bool value)
            => i.Success && value;

        public static bool operator |(ChildFrameDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the child frame on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            ImGui.EndChildFrame();
            this.Alive = false;
        }

#pragma warning disable SA1204
        /// <summary> End a child frame without using an IDisposable.</summary>
        public static void EndUnsafe()
            => ImGui.EndChildFrame();
#pragma warning restore SA1204
    }
}
