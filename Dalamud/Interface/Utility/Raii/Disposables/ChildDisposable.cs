using System.Numerics;

using Dalamud.Bindings.ImGui;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui child windows. </summary>
    public ref struct ChildDisposable : IDisposable
    {
        /// <summary> Whether creating the child window succeeded and it is at least partly visible. </summary>
        public readonly bool Success;

        /// <summary> Gets a value indicating whether the child window is already ended. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="ChildDisposable"/> struct. </summary>
        /// <param name="strId"> The ID of the child as text. </param>
        /// <returns> A disposable object that evaluates to true if any part of the begun child is currently visible. Use with using. </returns>
        internal ChildDisposable(ImU8String strId)
        {
            this.Success = ImGui.BeginChild(strId);
            this.Alive   = true;
        }

        /// <summary>Initializes a new instance of the <see cref="ChildDisposable"/> struct. </summary>
        /// <param name="strId"> The ID of the child as text. </param>
        /// <param name="size"> The desired size of the child. </param>
        /// <returns> A disposable object that evaluates to true if any part of the begun child is currently visible. Use with using. </returns>
        internal ChildDisposable(ImU8String strId, Vector2 size)
        {
            this.Success = ImGui.BeginChild(strId, size);
            this.Alive   = true;
        }

        /// <summary>Initializes a new instance of the <see cref="ChildDisposable"/> struct. </summary>
        /// <param name="strId"> The ID of the child as text. </param>
        /// <param name="size"> The desired size of the child. </param>
        /// <param name="border"> Whether the child should be framed by a border. </param>
        /// <returns> A disposable object that evaluates to true if any part of the begun child is currently visible. Use with using. </returns>
        internal ChildDisposable(ImU8String strId, Vector2 size, bool border)
        {
            this.Success = ImGui.BeginChild(strId, size, border);
            this.Alive   = true;
        }

        /// <summary>Initializes a new instance of the <see cref="ChildDisposable"/> struct. </summary>
        /// <param name="strId"> The ID of the child as text. </param>
        /// <param name="size"> The desired size of the child. </param>
        /// <param name="border"> Whether the child should be framed by a border. </param>
        /// <param name="flags"> Additional flags for the child. </param>
        /// <returns> A disposable object that evaluates to true if any part of the begun child is currently visible. Use with using. </returns>
        internal ChildDisposable(ImU8String strId, Vector2 size, bool border, ImGuiWindowFlags flags)
        {
            this.Success = ImGui.BeginChild(strId, size, border, flags);
            this.Alive   = true;
        }

        /// <summary> Conversion to bool. </summary>
        public static implicit operator bool(ChildDisposable value)
            => value.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator true(ChildDisposable i)
            => i.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator false(ChildDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on NOT operators. </summary>
        public static bool operator !(ChildDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on AND operators. </summary>
        public static bool operator &(ChildDisposable i, bool value)
            => i.Success && value;

        /// <summary> Conversion to bool on OR operators. </summary>
        public static bool operator |(ChildDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the child window on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            ImGui.EndChild();
            this.Alive = false;
        }

        /// <summary> End a child window without using an IDisposable.</summary>
        public static void EndUnsafe()
            => ImGui.EndChild();
    }
}
