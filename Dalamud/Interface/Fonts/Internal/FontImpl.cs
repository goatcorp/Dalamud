using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>The font of one text style, as ImGui knows it by an <see cref="ImFontPtr"/>.</summary>
/// <remarks><ul>
/// <li>One per distinct <see cref="TextStyle"/> while in use. Made and released by <see cref="FontResolver"/>.</li>
/// <li>An <see cref="ImFontPtr"/> of it is the frame it was handed out in, and its index in the fonts handed out in
/// that frame. It does not point to memory, and is valid in that frame only.</li>
/// <li>Indices 0 and 1 are the default font. A null <see cref="ImFontPtr"/> is index 0.</li>
/// <li>Handing out and resolving pointers is main thread only.</li>
/// </ul></remarks>
internal sealed unsafe class FontImpl
{
    private const int MaxTransitions = 64;

    // Stale pointers are logged once each, up to this many.
    private const int MaxWarnedStale = 256;

    // Indices of stale pointers are below this.
    private const uint MaxStaleIndex = 1 << 20;

    private static readonly ModuleLog Log = ModuleLog.Create<FontImpl>();

    private static readonly int MarkupCount = Enum.GetValues<TextMarkup>().Length;

    private static readonly ConcurrentDictionary<ulong, byte> WarnedStale = new();
    private static readonly List<FontImpl> Handed = [];
    private static long handedFrame = -1;
    private static long lastId;

    private readonly TextStyle style;
    private Dictionary<TextStyleRange, FontImpl>? transitions;
    private FontImpl?[]? markups;
    private CompositeFont? styledFrom;
    private Font? styled;
    private long nativeFrame = -1;
    private int nativeIndex;
    private uint evictions;

    /// <summary>Initializes a new instance of the <see cref="FontImpl"/> class.</summary>
    /// <param name="resolver">The resolver that made it.</param>
    /// <param name="style">The style.</param>
    /// <param name="entry">The font of the family.</param>
    public FontImpl(FontResolver resolver, in TextStyle style, FontResolver.Entry entry)
    {
        this.Resolver = resolver;
        this.style = style;
        this.Entry = entry;
    }

    /// <summary>Gets the font behind the current ImGui font.</summary>
    /// <seealso cref="FromNative"/>
    public static FontImpl? Current => FromNative(ImGui.Font.Handle);

    /// <summary>Gets the resolver that made it.</summary>
    public FontResolver Resolver { get; }

    /// <summary>Gets the style.</summary>
    public ref readonly TextStyle Style => ref this.style;

    /// <summary>Gets a unique identifier for this font instance.</summary>
    public long Id { get; } = Interlocked.Increment(ref lastId);

    /// <summary>Gets the font of the family.</summary>
    public FontResolver.Entry Entry { get; }

    /// <summary>Gets or sets the frame it was last used in.</summary>
    public long LastUsedFrame { get; set; }

    /// <summary>Gets or sets a value indicating whether <see cref="FontResolver"/> has evicted this instance.</summary>
    public bool Released { get; set; }

    /// <summary>Gets the <see cref="ImFontRef"/> of it, while someone holds it.</summary>
    public WeakReference<ImFontRef> Ref { get; } = new(null!);

    /// <summary>Gets the ImGui font of it, valid in this frame.</summary>
    public ImFont* Native
    {
        get
        {
            ThreadSafety.AssertMainThread();
            var frame = FontClock.Frame;
            if (handedFrame != frame)
            {
                handedFrame = frame;
                Handed.Clear();
                var defaultFont = this.Resolver.GetImpl(default);

                // Populate both index 0 and index 1 so defaultFont.Native is non-null even on frame 0.
                Handed.Add(defaultFont);
                Handed.Add(defaultFont);
                (defaultFont.nativeFrame, defaultFont.nativeIndex) = (frame, 1);
            }

            if (this.nativeFrame != frame)
            {
                (this.nativeFrame, this.nativeIndex) = (frame, Handed.Count);
                Handed.Add(this);
            }

            this.LastUsedFrame = frame;
            return (ImFont*)(((ulong)(uint)frame << 32) | (uint)this.nativeIndex);
        }
    }

    /// <summary>Gets the line height, as a multiple of the glyph size.</summary>
    /// <remarks>A default line height, and the sizes the line height needs, come from the font stack. Outside a frame,
    /// they come from the default font.</remarks>
    public float LineRatio
    {
        get
        {
            var font = FontSizes.Current(this.Resolver.Root);
            if (!FontSizes.WithinFrame || Current != this)
                font = FontSizes.Over(this.style, this.Entry.LoadedTarget, font);
            return font.PushPx / font.GlyphPx;
        }
    }

    /// <summary>Gets the font in the faces of the style.</summary>
    /// <remarks>It is the default font while the family has not loaded, or if it failed.</remarks>
    public Font Font
    {
        get
        {
            var target = this.Entry.LoadedTarget ?? this.Resolver.DefaultTarget;
            if (target != this.styledFrom)
                (this.styledFrom, this.styled) = (target, target.WithStyle(this.style.FaceKey));
            return this.styled!;
        }
    }

    /// <summary>Gets the font behind an ImGui font handed out in this frame, without falling back.</summary>
    /// <param name="font">The ImGui font.</param>
    /// <returns>The font, or null if <paramref name="font"/> is null or was not handed out in this frame, or if called
    /// off the main thread.</returns>
    public static FontImpl? FindExact(ImFont* font)
    {
        var value = (ulong)font;
        var index = (uint)value;

        return font is not null
               && ThreadSafety.IsMainThread
               && handedFrame == FontClock.Frame
               && (uint)(value >> 32) == (uint)handedFrame
               && index < Handed.Count
                   ? Handed[(int)index]
                   : null;
    }

    /// <summary>Gets the font behind an ImGui font.</summary>
    /// <param name="font">The ImGui font.</param>
    /// <returns>The font, the default font for null or the default font of any frame, the current font for another one
    /// not handed out in this frame, or null if there is none. The current font is the default font if it was not
    /// handed out in this frame either.</returns>
    /// <remarks><ul>
    /// <li>A pointer handed out in an earlier frame is logged once, as keeping one draws with the wrong font.</li>
    /// <li>Off the main thread, it is the default font.</li>
    /// </ul></remarks>
    public static FontImpl? FromNative(ImFont* font)
    {
        if (!ThreadSafety.IsMainThread)
            return Service<FontResolver>.GetNullable()?.GetImpl(default);

        // Draw data kept across frames, such as BufferBackedImDrawData, keeps the default font of its frame.
        var found = FindExact(font);
        if (font is not null && found is null && (uint)(ulong)font > 1)
        {
            WarnIfStale(font);
            var ctx = ImGui.GetCurrentContext();
            if (!ctx.IsNull)
                found = FindExact(ctx.Handle->Font);
        }

        return found
               ?? (handedFrame == FontClock.Frame
                       ? Handed[0]
                       : Service<FontResolver>.GetNullable()?.GetImpl(default));
    }

    /// <summary>Gets the font of this style with one field changed.</summary>
    /// <param name="change">The change. Its offset and length are not used.</param>
    /// <returns>The font.</returns>
    /// <remarks>Main thread only. Changes asked for before are found without hashing the style.</remarks>
    public FontImpl With(in TextStyleRange change)
    {
        var key = change.AtZero();
        if (this.transitions?.TryGetValue(key, out var next) is true && !next.Released)
        {
            next.LastUsedFrame = FontClock.Frame;
            return next;
        }

        var changed = this.style;
        key.ApplyTo(ref changed);
        next = this.Resolver.GetImpl(changed);
        if ((this.transitions ??= []).Count >= MaxTransitions)
        {
            // Dictionary reuses the just-removed slot on insert, so advancing skip evicts entries round-robin.
            var skip = this.evictions++ % MaxTransitions;
            foreach (var old in this.transitions.Keys)
            {
                if (skip-- != 0)
                    continue;
                this.transitions.Remove(old);
                break;
            }
        }

        this.transitions[key] = next;
        return next;
    }

    /// <summary>Gets the font of this style with text read as another markup.</summary>
    /// <param name="markup">The markup.</param>
    /// <returns>The font.</returns>
    /// <remarks>Main thread only. Markups asked for before are found without hashing the style.</remarks>
    public FontImpl WithMarkup(TextMarkup markup)
    {
        if (markup == this.style.Markup)
            return this;

        if ((uint)markup >= MarkupCount)
            return this.Resolver.GetImpl(this.style with { Markup = markup });

        ref var next = ref (this.markups ??= new FontImpl?[MarkupCount])[(int)markup];
        if (next is null || next.Released)
            next = this.Resolver.GetImpl(this.style with { Markup = markup });
        next.LastUsedFrame = FontClock.Frame;
        return next;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(FontImpl)}#{this.Id}({this.style})";

    // Logs a pointer handed out in an earlier frame, once per pointer. Pointers of ImGui's own fonts are told apart by
    // their low half, which is rarely as small as an index.
    // Between frames, the current font is still that of the frame before, so only pointers used in a frame count.
    private static void WarnIfStale(ImFont* font)
    {
        var value = (ulong)font;
        var frame = (uint)(value >> 32);
        if (frame >= (uint)FontClock.Frame || (uint)value >= MaxStaleIndex || WarnedStale.Count >= MaxWarnedStale)
        {
            return;
        }

        if (!FontSizes.WithinFrame || !WarnedStale.TryAdd(value, 0))
            return;

        var plugin = Service<PluginManager>.GetNullable()?.FindCallingPlugin(new StackTrace())?.Name;
        Log.Warning(
            "An ImFontPtr of frame {Frame} was used in frame {Now} by {Plugin}. It draws with the current font. Keep an ImFontRef or a TextStyle across frames instead",
            frame,
            FontClock.Frame,
            plugin ?? "Dalamud or an unknown plugin");
    }
}
