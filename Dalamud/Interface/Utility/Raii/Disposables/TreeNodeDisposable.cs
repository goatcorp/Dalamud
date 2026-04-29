using Dalamud.Bindings.ImGui;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui tree nodes. </summary>
    public ref struct TreeNodeDisposable : IDisposable
    {
        /// <summary> Whether creating the tree node succeeded and it is open. </summary>
        public readonly bool Success;

        /// <summary> Gets a value indicating whether the tree node is already popped. </summary>
        public bool Alive { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="TreeNodeDisposable"/> struct. </summary>
        /// <param name="label"> The node label as text. </param>
        /// <returns> A disposable object that evaluates to true if the begun tree node is currently expanded. Use with using. </returns>
        internal TreeNodeDisposable(ImU8String label)
        {
            this.Success = ImGui.TreeNodeEx(label);
            this.Alive   = true;
        }

        /// <summary>Initializes a new instance of the <see cref="TreeNodeDisposable"/> struct. </summary>
        /// <param name="label"> The node label as text. </param>
        /// <param name="flags"> Additional flags to control the tree's behavior. </param>
        /// <returns> A disposable object that evaluates to true if the begun tree node is currently expanded. Use with using. </returns>
        internal TreeNodeDisposable(ImU8String label, ImGuiTreeNodeFlags flags)
        {
            this.Success = ImGui.TreeNodeEx(label, flags);
            this.Alive   = !flags.HasFlag(ImGuiTreeNodeFlags.NoTreePushOnOpen);
        }

        public static implicit operator bool(TreeNodeDisposable value)
            => value.Success;

        public static bool operator true(TreeNodeDisposable i)
            => i.Success;

        public static bool operator false(TreeNodeDisposable i)
            => !i.Success;

        public static bool operator !(TreeNodeDisposable i)
            => !i.Success;

        public static bool operator &(TreeNodeDisposable i, bool value)
            => i.Success && value;

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

#pragma warning disable SA1204
        /// <summary> Pop a tree node without using an IDisposable. </summary>
        public static void PopUnsafe()
            => ImGui.TreePop();
#pragma warning restore SA1204
    }
}
