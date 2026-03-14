// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around pushing text wrap positions. </summary>
    public sealed class TextWrapDisposable : IDisposable
    {
        /// <summary> The number of text wrap positions currently pushed using this disposable. </summary>
        public int Count { get; private set; }

        /// <summary> Push a text wrap position to the text wrap stack. </summary>
        /// <param name="localX"> The window-local X coordinate at which to wrap text. If this is negative, no wrapping, if it is 0, wrap from here to the end of the available content region, and if it is positive, wrap from here. </param>
        /// <param name="condition"> If this is false, the position is not pushed. </param>
        /// <returns> A disposable object that can be used to push further text wrap positions and pops those positions after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep text wrap positions pushed longer than the current scope, use without using and use <seealso cref="Im.PopTextWrapPositionUnsafe"/>. </remarks>
        public TextWrapDisposable Push(float localX, bool condition)
            => condition ? this.Push(localX) : this;

        /// <inheritdoc cref="Push(float,bool)"/>
        public TextWrapDisposable Push(float localX)
        {
            ImGui.PushTextWrapPos(localX);
            ++this.Count;
            return this;
        }

        /// <summary> Pop a number of text wrap positions. </summary>
        /// <param name="num"> The number of text wrap positions to pop. This is clamped to the number of positions pushed by this object. </param>
        public void Pop(int num = 1)
        {
            num   =  Math.Min(num, this.Count);
            this.Count -= num;
            while (num-- > 0)
                ImGui.PopTextWrapPos();
        }

        /// <summary> Pop all pushed text wrap positions. </summary>
        public void Dispose()
            => this.Pop(this.Count);
    }
}
