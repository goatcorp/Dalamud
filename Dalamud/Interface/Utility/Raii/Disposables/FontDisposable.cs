using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts;
using Dalamud.Interface.Internal;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around pushing fonts. </summary>
    public sealed class FontDisposable : IDisposable
    {
        /// <summary> Gets the number of fonts currently pushed using this disposable. </summary>
        public int Count { get; private set; }

        /// <summary> Push the default font if any other font is currently pushed. </summary>
        /// <returns> A disposable object that can be used to push further fonts and pops those fonts after leaving scope. Use with using. </returns>
        [Obsolete("Use `ImRaii.DefaultFont()` instead.")]
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static FontDisposable DefaultFont()
            => PushDefaultFont();

        /// <summary> Push the default font if any other font is currently pushed. </summary>
        /// <returns> A disposable object that can be used to push further fonts and pops those fonts after leaving scope. Use with using. </returns>
        public static unsafe FontDisposable PushDefaultFont()
        {
            if (ImGui.GetCurrentContext().Handle->FontStack.Size > 1 &&
                Service<InterfaceManager>.GetNullable()?.DefaultFontRef is { } defaultFont)
            {
                return new FontDisposable().InternalPush(defaultFont);
            }

            return new FontDisposable();
        }

        /// <summary> Pushes a font onto the font stack with its inherent style and size. </summary>
        /// <param name="font"> The font to push. </param>
        /// <param name="condition"> If this is false, the font is not pushed. </param>
        /// <returns> A disposable object that can be used to push further fonts and pops those fonts after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep fonts pushed longer than the current scope, use without using and use <seealso cref="PopUnsafe"/>. </remarks>
        /// <seealso cref="TextStyle.PushFamily"/>
        public FontDisposable Push(ImFontPtr font, bool condition = true)
            => condition ? this.InternalPush(font) : this;

        private FontDisposable InternalPush(ImFontPtr font)
        {
            ImGui.PushFont(font);
            ++this.Count;
            return this;
        }

        /// <summary> Pop a number of fonts. </summary>
        /// <param name="num"> The number of fonts to pop. This is clamped to the number of fonts pushed by this object. </param>
        public void Pop(int num = 1)
        {
            num = Math.Min(num, this.Count);
            this.Count -= num;
            while (num-- > 0)
                ImGui.PopFont();
        }

        /// <summary> Pop all pushed fonts. </summary>
        public void Dispose()
            => this.Pop(this.Count);

#pragma warning disable SA1204
        /// <summary> Pop a number of fonts. </summary>
        /// <param name="num"> The number of fonts to pop. The number is not checked against the font stack. </param>
        /// <remarks> Avoid using this function, and fonts across scopes, as much as possible. </remarks>
        public static void PopUnsafe(int num = 1)
        {
            while (num-- > 0)
                ImGui.PopFont();
        }
#pragma warning restore SA1204
    }
}
