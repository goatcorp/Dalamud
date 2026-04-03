// ReSharper disable once CheckNamespace

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around pushing fonts. </summary>
    public sealed class FontDisposable : IDisposable
    {
        internal static int FontPushCounter;
        internal static ImFontPtr DefaultPushed;

        /// <summary> The number of fonts currently pushed using this disposable. </summary>
        public int Count { get; private set; }

        /// <summary> Push a font to the font stack. </summary>
        /// <param name="font"> The font to push. </param>
        /// <param name="condition"> If this is false, the font is not pushed. </param>
        /// <returns> A disposable object that can be used to push further fonts and pops those fonts after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep fonts pushed longer than the current scope, use without using and use <seealso cref="PopUnsafe"/>. </remarks>
        public FontDisposable Push(ImFontPtr font, bool condition = true)
            => condition ? this.InternalPush(font) : this;

        // Push the default font if any other font is currently pushed.
        /// <summary> Push the default font if any other font is currently pushed. </summary>
        public static FontDisposable DefaultFont()
            => new FontDisposable().Push(DefaultPushed, FontPushCounter > 0);

        /// <inheritdoc cref="Push(ImSharp.Im.Font,bool)"/>
        private FontDisposable InternalPush(ImFontPtr font)
        {
            if (FontPushCounter++ == 0)
                DefaultPushed = ImGui.GetFont();

            ImGui.PushFont(font);
            ++this.Count;
            return this;
        }

        /// <summary> Pop a number of fonts. </summary>
        /// <param name="num"> The number of fonts to pop. This is clamped to the number of fonts pushed by this object. </param>
        public void Pop(int num = 1)
        {
            num   =  Math.Min(num, this.Count);
            this.Count -= num;
            FontPushCounter -= num;
            while (num-- > 0)
                ImGui.PopFont();
        }

        /// <summary> Pop all pushed fonts. </summary>
        public void Dispose()
            => this.Pop(this.Count);

        /// <summary> Pop a number of fonts. </summary>
        /// <param name="num"> The number of fonts to pop. The number is not checked against the font stack. </param>
        /// <remarks> Avoid using this function, and fonts across scopes, as much as possible. </remarks>
        public static void PopUnsafe(int num = 1)
        {
            while (num-- > 0)
                ImGui.PopFont();
        }
    }
}
