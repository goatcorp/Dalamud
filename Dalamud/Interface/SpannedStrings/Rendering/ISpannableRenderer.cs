using Dalamud.Interface.SpannedStrings.Spannables;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings.Rendering;

/// <summary>A custom text renderer.</summary>
public interface ISpannableRenderer
{
    /// <summary>Rents a builder.</summary>
    /// <returns>The rented builder.</returns>
    /// <remarks>
    /// Return using <see cref="ReturnBuilder"/>, but don't bother to wrap in <c>try { ... } finally { ... }</c> block,
    /// unless you already have one. The cost of throwing an exception is more significant enough that creating another
    /// instance of <see cref="SpannedStringBuilder"/> doesn't matter at that point.
    /// </remarks>
    SpannedStringBuilder RentBuilder();

    /// <summary>Returns the rented builder.</summary>
    /// <param name="builder">The rented builder from <see cref="RentBuilder"/>.</param>
    /// <remarks>Specifying <c>null</c> to <paramref name="builder"/> is a no-op.</remarks>
    void ReturnBuilder(SpannedStringBuilder? builder);
    
    /// <summary>Renders a spannable.</summary>
    /// <param name="sequence">The char sequence.</param>
    /// <param name="state">The final render state.</param>
    /// <remarks>Use <see cref="Render(ReadOnlySpan{char}, ref RenderState)"/> if you want to retrieve the state after
    /// rendering.</remarks>
    void Render(ReadOnlySpan<char> sequence, RenderState state);

    /// <summary>Renders a spannable.</summary>
    /// <param name="sequence">The char sequence.</param>
    /// <param name="state">The final render state.</param>
    void Render(ReadOnlySpan<char> sequence, ref RenderState state);
    
    /// <summary>Renders a spannable.</summary>
    /// <param name="spannable">The spannable.</param>
    /// <param name="state">The final render state.</param>
    /// <remarks>Use <see cref="Render(IBlockSpannable, ref RenderState)"/> if you want to retrieve the state after
    /// rendering.</remarks>
    void Render(IBlockSpannable spannable, RenderState state);

    /// <summary>Renders a spannable.</summary>
    /// <param name="spannable">The spannable.</param>
    /// <param name="state">The final render state.</param>
    void Render(IBlockSpannable spannable, ref RenderState state);

    /// <summary>Renders an interactive spannable.</summary>
    /// <param name="spannable">The spannable.</param>
    /// <param name="state">The final render state.</param>
    /// <param name="hoveredLink">The payload being hovered, if any.</param>
    /// <returns><c>true</c> if any payload is currently being hovered.</returns>
    /// <remarks><paramref name="hoveredLink"/> is only valid until next render.</remarks>
    /// <remarks>Use <see cref="Render(IBlockSpannable, ref RenderState, out ReadOnlySpan{byte})"/> if you want to
    /// retrieve the state after rendering.</remarks>
    bool Render(IBlockSpannable spannable, RenderState state, out ReadOnlySpan<byte> hoveredLink);

    /// <summary>Renders a spannable.</summary>
    /// <param name="spannable">The spannable.</param>
    /// <param name="state">The final render state.</param>
    /// <param name="hoveredLink">The payload being hovered, if any.</param>
    /// <returns><c>true</c> if any payload is currently being hovered.</returns>
    /// <remarks><paramref name="hoveredLink"/> is only valid until next render.</remarks>
    bool Render(IBlockSpannable spannable, ref RenderState state, out ReadOnlySpan<byte> hoveredLink);

    /// <summary>Struct that defines the purpose of borrowing an instance of <see cref="ISpannableRenderer"/>.</summary>
    public ref struct Usage
    {
        /// <summary>Label in UTF-8.</summary>
        internal ReadOnlySpan<byte> LabelU8;

        /// <summary>Label in UTF-16.</summary>
        internal ReadOnlySpan<char> LabelU16;

        /// <summary>Numeric local ImGui ID.</summary>
        internal nint? Id;

        /// <summary>DrawList to draw to.</summary>
        internal ImDrawListPtr DrawListPtr;

        /// <summary>Whether to put <see cref="ImGui.Dummy"/>.</summary>
        internal bool PutDummy;

        public static implicit operator Usage(ReadOnlySpan<byte> labelU8) =>
            new() { LabelU8 = labelU8 };

        public static implicit operator Usage(ReadOnlySpan<char> labelU16) =>
            new() { LabelU16 = labelU16 };

        public static implicit operator Usage(ReadOnlyMemory<byte> labelU8) =>
            new() { LabelU8 = labelU8.Span };

        public static implicit operator Usage(ReadOnlyMemory<char> labelU16) =>
            new() { LabelU16 = labelU16.Span };

        public static implicit operator Usage(string label) => new() { LabelU16 = label };

        public static implicit operator Usage(int id) => new() { Id = id };

        public static implicit operator Usage(uint id) => new() { Id = unchecked((nint)id) };

        public static implicit operator Usage(nint id) => new() { Id = id };

        public static implicit operator Usage(nuint id) => new() { Id = unchecked((nint)id) };

        public static implicit operator Usage(ImDrawListPtr drawListPtr) =>
            new() { DrawListPtr = drawListPtr };

        public static unsafe implicit operator Usage(ImDrawList* drawListPtr) =>
            new() { DrawListPtr = drawListPtr };

        public static implicit operator Usage(bool b) => new() { PutDummy = b };
    }
}
