using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Interface.Internal;
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

    /// <summary>Attempts to get an icon by icon ID.</summary>
    /// <param name="iconType">The icon type.</param>
    /// <param name="iconId">The icon ID.</param>
    /// <param name="minDimensions">The minimum dimensions that this icon will be rendered.</param>
    /// <param name="textureWrap">The retrieved texture wrap.</param>
    /// <param name="uv0">The relative UV0 of the icon in <paramref name="textureWrap"/>.</param>
    /// <param name="uv1">The relative UV1 of the icon in <paramref name="textureWrap"/>.</param>
    /// <returns><c>true</c> if icon is retrieved.</returns>
    bool TryGetIcon(
        int iconType,
        uint iconId,
        Vector2 minDimensions,
        [NotNullWhen(true)] out IDalamudTextureWrap? textureWrap,
        out Vector2 uv0,
        out Vector2 uv1);

    /// <summary>Renders a spannable.</summary>
    /// <param name="sequence">The char sequence.</param>
    /// <param name="renderState">The final render state.</param>
    /// <remarks>Use <see cref="Render(ReadOnlySpan{char}, ref RenderState)"/> if you want to retrieve the state after
    /// rendering.</remarks>
    void Render(ReadOnlySpan<char> sequence, RenderState renderState);

    /// <summary>Renders a spannable.</summary>
    /// <param name="sequence">The char sequence.</param>
    /// <param name="renderState">The final render state.</param>
    void Render(ReadOnlySpan<char> sequence, ref RenderState renderState);

    /// <summary>Renders a spannable.</summary>
    /// <param name="spannable">The spannable.</param>
    /// <param name="renderState">The final render state.</param>
    /// <remarks>Use <see cref="Render(ISpannable, ref RenderState)"/> if you want to retrieve the state after
    /// rendering.</remarks>
    void Render(ISpannable spannable, RenderState renderState);

    /// <summary>Renders a spannable.</summary>
    /// <param name="spannable">The spannable.</param>
    /// <param name="renderState">The final render state.</param>
    void Render(ISpannable spannable, ref RenderState renderState);

    /// <summary>Renders an interactive spannable.</summary>
    /// <param name="spannable">The spannable.</param>
    /// <param name="renderState">The final render state.</param>
    /// <param name="hoveredLink">The payload being hovered, if any.</param>
    /// <returns><c>true</c> if any payload is currently being hovered.</returns>
    /// <remarks><paramref name="hoveredLink"/> is only valid until next render.</remarks>
    /// <remarks>Use <see cref="Render(ISpannable, ref RenderState, out ReadOnlySpan{byte})"/> if you want to
    /// retrieve the state after rendering.</remarks>
    bool Render(ISpannable spannable, RenderState renderState, out ReadOnlySpan<byte> hoveredLink);

    /// <summary>Renders a spannable.</summary>
    /// <param name="spannable">The spannable.</param>
    /// <param name="renderState">The final render state.</param>
    /// <param name="hoveredLink">The payload being hovered, if any.</param>
    /// <returns><c>true</c> if any payload is currently being hovered.</returns>
    /// <remarks><paramref name="hoveredLink"/> is only valid until next render.</remarks>
    bool Render(ISpannable spannable, ref RenderState renderState, out ReadOnlySpan<byte> hoveredLink);

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
