// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around disabled state. </summary>
    public sealed class DisabledDisposable : IDisposable
    {
        /// <summary> The global count of disabled pushes to reenable. </summary>
        public static int GlobalCount;

        /// <summary> Gets the number of disabled states currently pushed using this disposable. </summary>
        public int Count { get; private set; }

        /// <summary> Push a disabled state onto the stack. </summary>
        /// <param name="condition"> Whether to actually push a disabled state. </param>
        /// <returns> A disposable object that can be used to push further disabled states for whatever reason and pop them on leaving scope. Use with using.</returns>
        /// <remarks> If you need to keep a disabled state pushed longer than the current scope, use without using and use <seealso cref="PopUnsafe"/>. </remarks>
        public DisabledDisposable Push(bool condition)
            => condition ? this.Push() : this;

        /// <inheritdoc cref="Push(bool)"/>
        public DisabledDisposable Push()
        {
            ImGui.BeginDisabled(true);
            ++this.Count;
            ++GlobalCount;
            return this;
        }

        /// <summary> Pop a number of disabled states. </summary>
        /// <param name="num"> The number of disabled states to pop. This is clamped to the number of disabled states pushed by this object. </param>
        public void Pop(int num = 1)
        {
            num = Math.Min(num, this.Count);
            this.Count -= num;
            GlobalCount -= num;
            while (num-- > 0)
                ImGui.EndDisabled();
        }

        /// <summary> Pop all disabled states. </summary>
        public void Dispose()
            => this.Pop(this.Count);

        /// <summary> Pop a number of disabled states. </summary>
        /// <param name="num"> The number of disabled states to pop. The number is not checked against the disabled stack. </param>
        /// <remarks> Avoid using this function, and disabled states across scopes, as much as possible. </remarks>
        public static void PopUnsafe(int num = 1)
        {
            while (num-- > 0)
                ImGui.EndDisabled();
        }
    }
}
