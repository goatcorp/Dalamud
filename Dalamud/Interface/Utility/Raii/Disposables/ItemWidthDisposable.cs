// ReSharper disable once CheckNamespace
using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around pushing item widths. </summary>
    public sealed class ItemWidthDisposable : IDisposable
    {
        /// <summary> The number of item widths currently pushed using this disposable. </summary>
        public int Count { get; private set; }

        /// <summary> Push an item width to the item width stack. </summary>
        /// <param name="width"> The item width to push in pixels. </param>
        /// <param name="condition"> If this is false, the item width is not pushed. </param>
        /// <returns> A disposable object that can be used to push further item widths and pops those item widths after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep item widths pushed longer than the current scope, use without using and use <seealso cref="PopUnsafe"/>. </remarks>
        public ItemWidthDisposable Push(float width, bool condition)
            => condition ? this.Push(width) : this;

        /// <inheritdoc cref="Push(float,bool)"/>
        public ItemWidthDisposable Push(float width)
        {
            ImGui.PushItemWidth(width);
            ++this.Count;
            return this;
        }

        /// <summary> Pop a number of item widths. </summary>
        /// <param name="num"> The number of item widths to pop. This is clamped to the number of item widths pushed by this object. </param>
        public void Pop(int num = 1)
        {
            num   =  Math.Min(num, this.Count);
            this.Count -= num;
            while (num-- > 0)
                ImGui.PopItemWidth();
        }

        /// <summary> Pop all pushed item widths. </summary>
        public void Dispose()
            => this.Pop(this.Count);

        /// <summary> Pop a number of item widths. </summary>
        /// <param name="num"> The number of item widths to pop. The number is not checked against the item width stack. </param>
        /// <remarks> Avoid using this function, and item widths across scopes, as much as possible. </remarks>
        public static void PopUnsafe(int num = 1)
        {
            while (num-- > 0)
                ImGui.PopItemWidth();
        }
    }
}
