using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading;

using Dalamud.Logging.Internal;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Dalamud's text decorations, and the ones plugins registered.</summary>
/// <remarks>Registered IDs are the index plus <see cref="TextDecorationId.FirstRegistered"/> in the low 20 bits and
/// a generation in the high 12, so that the ID of an unregistered decoration does not name a later one.</remarks>
internal static class TextDecorationRegistry
{
    // The most bands a decoration may be drawn in.
    private const int MaxBands = 16;

    private const int IndexBits = 20;
    private const uint IndexMask = (1u << IndexBits) - 1;
    private const int MaxRegistered = (int)(IndexMask - TextDecorationId.FirstRegistered);
    private const int MaxGeneration = (1 << (32 - IndexBits)) - 1;

    private static readonly ModuleLog Log = new(typeof(TextDecorationRegistry));
    private static readonly Entry?[] BuiltIns = MakeBuiltIns();
    private static readonly Lock SyncRoot = new();
    private static readonly Stack<int> FreeIndices = new();
    private static int[] generations = [];

    // Replaced as a whole on changes, so that drawing reads it without locking.
    private static volatile Entry?[] registered = [];

    /// <summary>Gets a decoration.</summary>
    /// <param name="id">The ID.</param>
    /// <param name="entry">The decoration, or null if it does not exist.</param>
    /// <returns>Whether it exists, and has not been disabled.</returns>
    public static bool TryGet(TextDecorationId id, [NotNullWhen(true)] out Entry? entry)
    {
        var value = id.Value;
        Entry? e;
        if (value < TextDecorationId.FirstRegistered)
        {
            e = value < BuiltIns.Length ? BuiltIns[value] : null;
        }
        else
        {
            // IDs not made here may have an index below FirstRegistered, which wraps to a large one.
            var index = (value & IndexMask) - TextDecorationId.FirstRegistered;
            var entries = registered;
            e = index < (uint)entries.Length && entries[index] is { } r && r.Id == id ? r : null;
        }

        entry = e;
        return entry is { Disabled: false };
    }

    /// <summary>Registers a decoration.</summary>
    /// <param name="drawer">What draws it.</param>
    /// <param name="flags">The layers it is drawn in.</param>
    /// <param name="bands">The number of times it is drawn in each layer, from 1 to <see cref="MaxBands"/>.</param>
    /// <returns>The ID.</returns>
    public static TextDecorationId Register(TextDecorationDrawer drawer, TextDecorationFlags flags, int bands = 1)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        ArgumentOutOfRangeException.ThrowIfLessThan(bands, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bands, MaxBands);
        using (SyncRoot.EnterScope())
        {
            var entries = registered;
            if (!FreeIndices.TryPop(out var index))
            {
                index = entries.Length;
                if (index >= MaxRegistered)
                    throw new InvalidOperationException("Too many text decorations.");
                Array.Resize(ref generations, index + 1);
            }

            var gen = (uint)++generations[index];
            var id = new TextDecorationId((gen << IndexBits) | ((uint)index + TextDecorationId.FirstRegistered));
            var copy = new Entry?[Math.Max(entries.Length, index + 1)];
            entries.CopyTo(copy, 0);
            copy[index] = new(id, drawer, flags, bands);
            registered = copy;
            return id;
        }
    }

    /// <summary>Unregisters a decoration. Nothing happens if it is not registered.</summary>
    /// <param name="id">The ID.</param>
    public static void Unregister(TextDecorationId id)
    {
        if (id.Value < TextDecorationId.FirstRegistered)
            return;
        using (SyncRoot.EnterScope())
        {
            var index = (id.Value & IndexMask) - TextDecorationId.FirstRegistered;
            var entries = registered;
            if (index >= (uint)entries.Length || entries[index]?.Id != id)
                return;
            var copy = (Entry?[])entries.Clone();
            copy[index] = null;
            registered = copy;

            // An index whose generations are used up is not used again, so that no ID repeats.
            if (generations[index] != MaxGeneration)
                FreeIndices.Push((int)index);
        }
    }

    private static Entry?[] MakeBuiltIns()
    {
        var entries = new Entry?[(int)TextDecorationId.Line.Value + 1];
        entries[TextDecorationId.Background.Value] =
            new(TextDecorationId.Background, DrawBackground, TextDecorationFlags.BelowText);

        // Drawn in both passes, each drawing the positions of its own (Entry.LayersOf).
        entries[TextDecorationId.Line.Value] =
            new(TextDecorationId.Line, DrawLines, TextDecorationFlags.BelowText);
        return entries;
    }

    // Not in the color of the text. A context with alpha 0 is no background.
    private static void DrawBackground(in TextDecorationSpan s)
    {
        if (s.Context >> 24 != 0)
            s.AddRectFilled(new(s.X0, s.LineTop), new(s.X1, s.LineBottom), s.ApplyAlpha(s.Context), true);
    }

    // Under and over the text in the pass under it, and through it in the pass over it. Count lines are drawn at each
    // position, a thickness apart (wavy ones, two). They go down from the underline position, up from the top of the
    // em box, or around the strikethrough position. The em box splits the em as the ascent and descent split the line.
    // Fonts with room for accents above their capitals have an ascent well above the em box.
    private static void DrawLines(in TextDecorationSpan s)
    {
        if (!TextLineDecoration.TryRead(new(TextDecorationId.Line, s.Context, s.Data), out var lines) ||
            !lines.IsOn)
        {
            return;
        }

        var color = s.ContextColor();
        if (s.IsBelowText)
        {
            if ((lines.Positions & TextLinePosition.Under) != 0)
                DrawLinesAt(s, lines, s.Baseline + s.UnderlinePosition, s.UnderlineThickness, 1, color);
            if ((lines.Positions & TextLinePosition.Over) != 0)
            {
                var top = s.Ascent + s.Descent > s.EmSize ? (s.EmSize * s.Ascent) / (s.Ascent + s.Descent) : s.Ascent;
                DrawLinesAt(s, lines, MathF.Round(s.Baseline - top), s.UnderlineThickness, -1, color);
            }
        }
        else if ((lines.Positions & TextLinePosition.Through) != 0)
        {
            var t = ThicknessOf(s, lines, s.StrikethroughThickness);
            var spread = ((Math.Max((int)lines.Count, 1) - 1) * StepOf(lines, t)) / 2;
            DrawLinesAt(s, lines, s.Baseline - s.StrikethroughPosition - spread, s.StrikethroughThickness, 1, color);
        }
    }

    // The distance between the tops of lines.
    private static float StepOf(in TextLineDecoration lines, float t) =>
        t * (lines.Style == TextLineStyle.Wavy ? 3 : 2);

    // At least a device pixel, as patterns step by it and wavy lines take points by it.
    private static float ThicknessOf(in TextDecorationSpan s, in TextLineDecoration lines, float fontThickness)
    {
        var t = lines.Thickness > 0 ? lines.Thickness : fontThickness;
        var min = 1 / s.Density;
        return t >= min ? t : min;
    }

    // Count lines from the top of the first, going down (direction 1) or up (-1).
    private static void DrawLinesAt(
        in TextDecorationSpan s, in TextLineDecoration lines, float first, float fontThickness, int direction,
        uint color)
    {
        var t = ThicknessOf(s, lines, fontThickness);
        var step = StepOf(lines, t);
        for (var i = 0; i < Math.Max((int)lines.Count, 1); i++)
        {
            var y = first + (direction * i * step);
            switch (lines.Style)
            {
                case TextLineStyle.Dotted:
                    Pattern(s, y, t, t, t * 2, color);
                    break;
                case TextLineStyle.Dashed:
                    Pattern(s, y, t, t * 3, t * 5, color);
                    break;
                case TextLineStyle.Wavy:
                    Wavy(s, y, t, color);
                    break;
                case TextLineStyle.None:
                case TextLineStyle.Solid:
                default:
                    s.AddRectFilled(new(s.X0, y), new(s.X1, y + t), color);
                    break;
            }
        }
    }

    // A zigzag from y down, twice as high as it is thick, with its corners on a grid from the line start so that
    // adjacent spans join.
    private static void Wavy(in TextDecorationSpan s, float y, float t, uint color)
    {
        var half = t * 2;
        var center = y + t;
        var start = s.LineStart;
        var first = MathF.Floor((s.X0 - start) / half) + 1;
        var pooledPoints = PooledSpan<Vector2>.Rent((int)Math.Max(2, ((s.X1 - s.X0) / half) + 3));
        var points = pooledPoints.Span;
        var n = 0;
        points[n++] = new(s.X0, Wave(s.X0, start, half, center, t));
        for (var k = first; start + (k * half) < s.X1 && n < points.Length - 1; k++)
            points[n++] = new(start + (k * half), Wave(start + (k * half), start, half, center, t));
        points[n++] = new(s.X1, Wave(s.X1, start, half, center, t));
        s.AddPolyline(points[..n], color, t);
        pooledPoints.Recycle();
        return;

        // Up from the bottom (y grows down) after even corners, down from the top after odd ones.
        static float Wave(float x, float start, float half, float center, float t)
        {
            var p = (x - start) / half;
            var frac = p - MathF.Floor(p);
            return ((long)MathF.Floor(p) & 1) == 0 ? (center + t) - (2 * t * frac) : (center - t) + (2 * t * frac);
        }
    }

    // Segments of a length every period, on a grid from the line start.
    private static void Pattern(in TextDecorationSpan s, float y, float t, float length, float period, uint color)
    {
        for (var x = s.LineStart + (MathF.Floor((s.X0 - s.LineStart) / period) * period); x < s.X1; x += period)
        {
            var x0 = Math.Max(x, s.X0);
            var x1 = Math.Min(x + length, s.X1);
            if (x1 > x0)
                s.AddRectFilled(new(x0, y), new(x1, y + t), color);
        }
    }

    /// <summary>A decoration.</summary>
    /// <param name="id">The ID.</param>
    /// <param name="drawer">What draws it.</param>
    /// <param name="flags">The layers it is drawn in.</param>
    /// <param name="bands">The number of times it is drawn in each layer.</param>
    internal sealed class Entry(
        TextDecorationId id, TextDecorationDrawer drawer, TextDecorationFlags flags, int bands = 1)
    {
        private const TextDecorationFlags AllLayers =
            TextDecorationFlags.BelowText | TextDecorationFlags.BelowEdge | TextDecorationFlags.BelowFill |
            TextDecorationFlags.OverText;

        /// <summary>Gets the ID.</summary>
        public TextDecorationId Id { get; } = id;

        /// <summary>Gets the layers it is drawn in. Without any, it is drawn over the text.</summary>
        public TextDecorationFlags Layers { get; } =
            (flags & AllLayers) is not TextDecorationFlags.None and var layers ? layers : TextDecorationFlags.OverText;

        /// <summary>Gets the number of times it is drawn in each layer, each time for all the spans.</summary>
        public int Bands { get; } = bands;

        /// <summary>Gets a value indicating whether it threw, and is not drawn any more.</summary>
        public bool Disabled { get; private set; }

        /// <summary>Gets the layers a decoration of this kind is drawn in.</summary>
        /// <param name="decoration">The decoration.</param>
        /// <returns>The layers. Lines under and over the text are drawn under it, and lines through it over it.
        /// Others are drawn in the layers registered.</returns>
        public TextDecorationFlags LayersOf(in TextDecoration decoration)
        {
            if (!TextLineDecoration.TryRead(decoration, out var lines))
                return this.Layers;
            var layers = TextDecorationFlags.None;
            if ((lines.Positions & (TextLinePosition.Under | TextLinePosition.Over)) != 0)
                layers |= TextDecorationFlags.BelowText;
            if ((lines.Positions & TextLinePosition.Through) != 0)
                layers |= TextDecorationFlags.OverText;
            return layers;
        }

        /// <summary>Draws a span. A registered decoration that throws is disabled, and why is logged.</summary>
        /// <param name="span">The span.</param>
        public void Draw(in TextDecorationSpan span)
        {
            if (this.Id.IsBuiltIn)
            {
                drawer(span);
                return;
            }

            try
            {
                drawer(span);
            }
            catch (Exception e)
            {
                this.Disabled = true;
                Log.Error(e, "Text decoration {Id:X} threw, and is not drawn any more", this.Id.Value);
            }
        }
    }
}
