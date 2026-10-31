using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Markup;

namespace Dalamud.Interface.Fonts;

/// <summary>Pops what has been pushed on <see cref="IDisposable.Dispose"/> for font-related things.</summary>
/// <remarks>Dispose it once, as with <see langword="using"/>. Each copy pops again when disposed. A
/// <see langword="default"/> one has pushed nothing.</remarks>
public struct ImPopFontDisposable : IDisposable
{
    private State state;

    /// <summary>Initializes a new instance of the <see cref="ImPopFontDisposable"/> struct.</summary>
    /// <param name="font">Whether <see cref="ImGui.PopFont"/> needs to be called.</param>
    /// <param name="textCallback">Whether <see cref="TextCallbackStack.Pop"/> needs to be called.</param>
    /// <param name="completeGlyphs">Whether <see cref="GlyphRasterBudget.EndSynchronous"/> needs to be called.</param>
    internal ImPopFontDisposable(bool font = false, bool textCallback = false, bool completeGlyphs = false) =>
        this.state = (font ? State.Font : 0) | (textCallback ? State.TextCallback : 0) |
                     (completeGlyphs ? State.CompleteGlyphs : 0);

    [Flags]
    private enum State : byte
    {
        Disposed = 0,
        Font = 1 << 0,
        TextCallback = 1 << 1,
        CompleteGlyphs = 1 << 2,
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        var s = this.state;
        if (s == State.Disposed)
            return;
        this.state = State.Disposed;
        if ((s & State.Font) != 0)
            ImGui.PopFont();
        if ((s & State.TextCallback) != 0)
            TextCallbackStack.Pop();
        if ((s & State.CompleteGlyphs) != 0)
            GlyphRasterBudget.EndSynchronous();
    }
}
