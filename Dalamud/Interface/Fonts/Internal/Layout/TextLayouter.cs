using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Markup;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Utility;

using Serilog;

using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.Layout;

/// <summary>Lays out text for ImGui and Dalamud's own attributed text.</summary>
/// <remarks>Main thread only.</remarks>
internal static unsafe class TextLayouter
{
    // Layouts reused across frames stay for CacheFrames. Single-frame layouts expire after NewFrames before Gen 2 promotion.
    private const int CacheFrames = 120;
    private const int NewFrames = 2;

    // Maximum retained capacity of keptKeyBuffer. Larger keys rent temporary arrays from ArrayPool.
    private const int MaxKeptKeyLength = 1 << 16;

    private const long ReportIntervalMs = 60_000;

    // When each kind of exception was last logged, by the method that threw it.
    private static readonly Dictionary<(Type Type, MethodBase? Site), long> LastReports = [];

    private static readonly Dictionary<byte[], Entry> Cache = new(SpanSequenceComparer<byte>.Instance);

    private static readonly Dictionary<byte[], Entry>.AlternateLookup<ReadOnlySpan<byte>> CacheLookup =
        Cache.GetAlternateLookup<ReadOnlySpan<byte>>();

    private static byte[] keptKeyBuffer = new byte[1024];

    // The kept buffer, or a rented one until ReleaseKey.
    private static byte[] keyBuffer = keptKeyBuffer;
    private static long lastSweepFrame = -1;

    private enum KeyKind : byte
    {
        Plain,
        Styled,
    }

    /// <summary>Gets the total number of layout requests, cached or not.</summary>
    public static long Requests { get; private set; }

    /// <summary>Gets the total number of layout cache misses.</summary>
    public static long Misses { get; private set; }

    /// <summary>Gets the total input text bytes across all layout requests.</summary>
    public static long RequestedBytes { get; private set; }

    /// <summary>Gets the number of cached layouts.</summary>
    public static int CacheCount => Cache.Count;

    /// <summary>Gets the number of cached layouts reused across multiple frames, as of the last sweep.</summary>
    public static int ReusedCount { get; private set; }

    /// <summary>Sets the text callbacks of the current ImGui context.</summary>
    public static void Install()
    {
        NativeTextLayouts.Register();
        ImGuiNative.SetTextCallbacks(&LayoutCallback, &MetricsCallback);
    }

    /// <summary>Lays out text.</summary>
    /// <param name="font">The font of text outside runs.</param>
    /// <param name="desc">Layout parameters.</param>
    /// <param name="text">The text, in bytes.</param>
    /// <param name="codeUnitSize">1 for UTF-8, 2 for UTF-16 (little endian).</param>
    /// <param name="runs">Attributed ranges, in byte offsets into <paramref name="text"/>.</param>
    /// <returns>The layout.</returns>
    /// <remarks>The layout may be shared with other callers. It is valid while cached, and is recycled once evicted
    /// from the cache and released by ImGui. Do not keep it across frames.</remarks>
    public static TextLayout Create(
        FontImpl font, in TextLayoutDesc desc, ReadOnlySpan<byte> text, int codeUnitSize = 1,
        ReadOnlySpan<TextRun> runs = default)
    {
        var frame = Sweep();
        Requests++;
        RequestedBytes += text.Length;
        if (codeUnitSize == 1)
            text = GetFirstLine(desc, text);

        if (Lookup(BuildKey(font, desc, text, codeUnitSize, runs), frame, out var key) is { } cached)
            return cached;

        Misses++;
        var start = FontProfiler.Begin(FontProfiler.Section.Layout);
        var layout = TextLayout.Rent(font, desc, text, codeUnitSize, runs, []);
        FontProfiler.End(FontProfiler.Section.Layout, start, text.Length);
        Add(key!, layout, frame);
        return layout;
    }

    /// <summary>Lays out text according to its markup, or as plain text if the markup has no styler.</summary>
    /// <param name="font">The font whose style the text starts with. Its markup selects how the text is parsed.</param>
    /// <param name="desc">Layout parameters.</param>
    /// <param name="text">The text.</param>
    /// <returns>The layout.</returns>
    /// <remarks><ul>
    /// <li>Main thread only, like <see cref="Create"/>. <see cref="TextStyler.CreateLayout"/> returns
    /// <see langword="null"/> off-thread only as a defensive guard.</li>
    /// <li>Plain text runs through a callback styler only while <see cref="TextCallbackStack.Active"/> is
    /// <see langword="true"/> and <see cref="ImTextLayoutFlags.Plain"/> is unset.</li>
    /// </ul></remarks>
    public static TextLayout CreateStyled(FontImpl font, in TextLayoutDesc desc, ReadOnlySpan<byte> text)
    {
        var markup = font.Style.Markup;
        if ((desc.Flags & ImTextLayoutFlags.Plain) != 0 || (markup == TextMarkup.Plain && !TextCallbackStack.Active))
            return Create(font, desc, text);

        // Other markups may have multi-line tags, so only plain text is truncated at the first LF before invoking a styler.
        if (markup == TextMarkup.Plain)
            text = GetFirstLine(desc, text);
        return TextStyler.Get(markup)?.CreateLayout(font, desc, text) ?? Create(font, desc, text);
    }

    /// <summary>Looks up a cached layout for caller-styled text such as a SeString.</summary>
    /// <param name="font">The font.</param>
    /// <param name="desc">Layout parameters.</param>
    /// <param name="text">The text, as given.</param>
    /// <param name="extraKeys">Additional cache-key values distinguishing caller-specific state.</param>
    /// <param name="layout">The layout, if cached.</param>
    /// <param name="key">The key to add the layout with otherwise.</param>
    /// <returns>Whether it is cached.</returns>
    /// <seealso cref="AddCached"/>
    public static bool TryGetCached(
        FontImpl font, in TextLayoutDesc desc, ReadOnlySpan<byte> text, ReadOnlySpan<long> extraKeys,
        [NotNullWhen(true)] out TextLayout? layout, out byte[]? key)
    {
        var frame = Sweep();
        Requests++;
        RequestedBytes += text.Length;
        var length = BeginKey(KeyKind.Styled, font, desc);
        Append(ref length, extraKeys.Length);
        foreach (var k in extraKeys)
            Append(ref length, k);
        layout = Lookup(EndKey(length, text), frame, out key);
        if (layout is null)
            Misses++;
        return layout is not null;
    }

    /// <summary>Caches a layout made after <see cref="TryGetCached"/>.</summary>
    /// <param name="key">The key from <see cref="TryGetCached"/>.</param>
    /// <param name="layout">The layout.</param>
    public static void AddCached(byte[] key, TextLayout layout) => Add(key, layout, Sweep());

    /// <summary>Logs a failed layout, at most once a minute for each kind of exception and the method that threw it.
    /// </summary>
    /// <param name="e">The exception.</param>
    /// <remarks>Callable from any thread.</remarks>
    public static void Report(Exception e)
    {
        var now = Environment.TickCount64;
        lock (LastReports)
        {
            var key = (e.GetType(), e.TargetSite);
            if (LastReports.TryGetValue(key, out var last) && now - last < ReportIntervalMs)
                return;
            LastReports[key] = now;
        }

        Log.Logger.Error(e, "Text layout failed");
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int LayoutCallback(
        ImFont* font, ImTextDesc* desc, byte* text, byte* textEnd, ImTextLayout** outLayout)
    {
        *outLayout = null;

        // ImGui measures the text as empty, and draws nothing.
        if (!ThreadSafety.IsMainThread)
            return RPC.RPC_E_WRONG_THREAD;

        try
        {
            if (FontImpl.FromNative(font) is not { } impl)
                return E.E_INVALIDARG; // The font is not Dalamud's.
            var d = TextLayoutDesc.FromNative(*desc, impl);

            var bytes = new ReadOnlySpan<byte>(text, (int)(textEnd - text));
            *outLayout = NativeTextLayouts.ToNative(CreateStyled(impl, d, bytes));
            return 0;
        }
        catch (Exception e)
        {
            Report(e);
            return e.HResult < 0 ? e.HResult : E.E_FAIL;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void MetricsCallback(
        ImFont* font, float size, float density, ImTextMetrics* outMetrics)
    {
        try
        {
            // Fonts are main thread only. Other threads get the fallback.
            if (ThreadSafety.IsMainThread && FontImpl.FromNative(font) is { } impl)
            {
                var m = (impl.Font.GetMetrics(size / impl.LineRatio, density) with { LineHeight = size }).SplitLineGap();
                WriteMetrics(m, outMetrics);
                return;
            }
        }
        catch (Exception e)
        {
            Report(e);
        }

        WriteMetrics(FontMetrics.Fallback(size), outMetrics);
        return;

        static void WriteMetrics(in FontMetrics m, ImTextMetrics* outMetrics) =>
            *outMetrics = new(m.Ascent, m.Descent, m.LineHeight, m.UnderlinePosition, m.UnderlineThickness);
    }

    // Looks up key in Cache and releases keyBuffer. On a miss, returns a copied key because building the layout may re-entrantly overwrite keyBuffer.
    private static TextLayout? Lookup(ReadOnlySpan<byte> key, long frame, out byte[]? ownKey)
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(CacheLookup, key);
        if (!Unsafe.IsNullRef(ref entry))
        {
            ReleaseKey();
            entry.Use(frame);
            ownKey = null;
            return entry.Layout;
        }

        ownKey = [.. key];
        ReleaseKey();
        return null;
    }

    // Caches a layout, holding a native reference to it, so that drawing it does not make one each time.
    private static void Add(byte[] key, TextLayout layout, long frame)
    {
        if (!Cache.TryAdd(key, new(layout, frame)))
            return;
        layout.Cached = true;
        _ = NativeTextLayouts.ToNative(layout);
    }

    // Slices UTF-8 text through the first LF when StopOnNewLine is set, since InputText passes the remainder of the buffer for each row.
    private static ReadOnlySpan<byte> GetFirstLine(scoped in TextLayoutDesc desc, ReadOnlySpan<byte> text) =>
        (desc.Flags & ImTextLayoutFlags.StopOnNewLine) != 0 && text.IndexOf((byte)'\n') is var lf and >= 0
            ? text[..(lf + 1)]
            : text;

    private static ReadOnlySpan<byte> BuildKey(
        FontImpl font, in TextLayoutDesc desc, ReadOnlySpan<byte> text, int codeUnitSize, ReadOnlySpan<TextRun> runs)
    {
        var length = BeginKey(KeyKind.Plain, font, desc);
        Append(ref length, codeUnitSize);
        Append(ref length, runs.Length);
        foreach (var r in runs)
        {
            Append(ref length, r.TextBegin);
            Append(ref length, r.TextEnd);
            AppendFont(ref length, r.Impl);
            Append(ref length, r.UserData);
        }

        return EndKey(length, text);
    }

    // Writes the common key prefix (kind, font, canonical desc), where KeyKind prevents collisions between plain and styler keys.
    private static int BeginKey(KeyKind kind, FontImpl font, in TextLayoutDesc desc)
    {
        var length = 0;
        Append(ref length, kind);
        AppendFont(ref length, font);

        // ImTextLayoutFlags.Plain only affects styler dispatch (already captured by KeyKind), not layout.
        var canonical = desc.Canonical;
        Append(ref length, canonical with { Flags = canonical.Flags & ~ImTextLayoutFlags.Plain });
        return length;
    }

    private static ReadOnlySpan<byte> EndKey(int length, ReadOnlySpan<byte> text)
    {
        EnsureKey(length + text.Length);
        text.CopyTo(keyBuffer.AsSpan(length));
        return keyBuffer.AsSpan(0, length + text.Length);
    }

    private static void Append<T>(ref int length, in T value)
        where T : unmanaged
    {
        EnsureKey(length + sizeof(T));
        Unsafe.WriteUnaligned(ref keyBuffer[length], value);
        length += sizeof(T);
    }

    // Include both FontImpl.Id and resolved Font.Id so cache keys invalidate when a font family rebuilds.
    private static void AppendFont(ref int length, FontImpl? font)
    {
        Append(ref length, font?.Id ?? 0);
        Append(ref length, font?.Font.Id ?? 0);
    }

    private static long Sweep()
    {
        var frame = FontClock.Frame;
        if (frame != lastSweepFrame)
            SweepFrame(frame);
        return frame;
    }

    // Out-of-line slow path for Sweep so the per-call fast path stays inlineable.
    private static void SweepFrame(long frame)
    {
        lastSweepFrame = frame;

        var reused = 0;
        foreach (var (k, e) in Cache)
        {
            if (frame - e.LastUsedFrame > (e.Reused ? CacheFrames : NewFrames))
            {
                Cache.Remove(k);
                e.Layout.Cached = false;

                // Guard against callers over-releasing the native layout.
                if (e.Layout.NativeHandle != 0)
                    NativeTextLayouts.Release(e.Layout);
            }
            else if (e.Reused)
            {
                reused++;
            }
        }

        ReusedCount = reused;
    }

    private static void EnsureKey(int length)
    {
        if (keyBuffer.Length < length)
            GrowKey(length);
    }

    private static void GrowKey(int length)
    {
        var grown = Math.Max(length, keyBuffer.Length * 2);
        if (length <= MaxKeptKeyLength)
        {
            Array.Resize(ref keptKeyBuffer, Math.Min(grown, MaxKeptKeyLength));
            keyBuffer = keptKeyBuffer;
            return;
        }

        var rented = ArrayPool<byte>.Shared.Rent(grown);
        keyBuffer.CopyTo(rented, 0);
        ReleaseKey();
        keyBuffer = rented;
    }

    private static void ReleaseKey()
    {
        if (keyBuffer == keptKeyBuffer)
            return;
        ArrayPool<byte>.Shared.Return(keyBuffer);
        keyBuffer = keptKeyBuffer;
    }

    // Mutable struct updated in place via CollectionsMarshal.GetValueRefOrNullRef.
    private struct Entry(TextLayout layout, long frame)
    {
        public TextLayout Layout { get; } = layout;

        public long LastUsedFrame { get; private set; } = frame;

        // Whether it was used in a frame after the one it was made in. Measuring and drawing text in one frame is not
        // reuse.
        public bool Reused { get; private set; }

        public void Use(long frame)
        {
            this.Reused |= frame != this.LastUsedFrame;
            this.LastUsedFrame = frame;
        }
    }
}
