using System.Numerics;

namespace Dalamud.Interface.Fonts.Internal.Layout;

/// <summary>Space in text for something drawn by the caller.</summary>
/// <param name="Bounds">The box (x0, y0, x1, y1) relative to the pen on the baseline, y down.</param>
/// <param name="UserData">Uninterpreted data returned by hit testing.</param>
/// <remarks>Replaces exactly one U+FFFC in the text, in order. The pen advances by x1.</remarks>
internal readonly record struct InlineObject(Vector4 Bounds, ulong UserData = 0);
