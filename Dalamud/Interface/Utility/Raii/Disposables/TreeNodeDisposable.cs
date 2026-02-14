// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui tree nodes. </summary>
    public ref struct TreeNodeDisposable : IDisposable
    {
        /// <summary> Whether creating the tree node succeeded and it is open. </summary>
        public readonly bool Success;

        /// <summary> Whether the tree node is already popped. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="TreeNodeDisposable"/> struct. </summary>
        /// <param name="label"> The node label as text. If this is a UTF8 string, it HAS to be null-terminated. </param>
        /// <param name="flags"> Additional flags to control the tree's behaviour. </param>
        /// <returns> A disposable object that evaluates to true if the begun tree node is currently expanded. Use with using. </returns>
        internal TreeNodeDisposable(ImU8String label)
        {
            this.Success = ImGui.TreeNodeEx(label);
            this.Alive   = true;
        }

        /// <summary>Initializes a new instance of the <see cref="TreeNodeDisposable"/> struct. </summary>
        /// <param name="label"> The node label as text. If this is a UTF8 string, it HAS to be null-terminated. </param>
        /// <param name="flags"> Additional flags to control the tree's behaviour. </param>
        /// <returns> A disposable object that evaluates to true if the begun tree node is currently expanded. Use with using. </returns>
        internal TreeNodeDisposable(ImU8String label, ImGuiTreeNodeFlags flags)
        {
            this.Success = ImGui.TreeNodeEx(label, flags);
            this.Alive   = !flags.HasFlag(ImGuiTreeNodeFlags.NoTreePushOnOpen);
        }

        /// <summary> Conversion to bool. </summary>

        public static implicit operator bool(TreeNodeDisposable value)
            => value.Success;

        /// <summary> Conversion to bool. </summary>

        public static bool operator true(TreeNodeDisposable i)
            => i.Success;

        /// <summary> Conversion to bool. </summary>

        public static bool operator false(TreeNodeDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on NOT operators. </summary>

        public static bool operator !(TreeNodeDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on AND operators. </summary>

        public static bool operator &(TreeNodeDisposable i, bool value)
            => i.Success && value;

        /// <summary> Conversion to bool on OR operators. </summary>

        public static bool operator |(TreeNodeDisposable i, bool value)
            => i.Success || value;

        /// <summary> Pop the tree node on leaving scope. </summary>

        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImGui.TreePop();
            this.Alive = false;
        }

        /// <summary> Pop a tree node without using an IDisposable. </summary>

        public static void PopUnsafe()
            => ImGui.TreePop();
    }
}
