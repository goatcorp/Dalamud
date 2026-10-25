using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Interface.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Loads and keeps the fonts of font families, and the fonts of text styles.</summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class FontResolver : IInternalDisposableService
{
    // Frames a font is kept after its last use, once no ImFontRef holds it. It covers the frame being drawn, and fonts
    // that are pushed every frame without being held.
    private const int GraceFrames = 120;

    // Frames between looks for fonts to release.
    private const int SweepFrames = 60;

    // Frames until a font that failed to load is tried again while it is used. Each failure doubles it, up to
    // 2^MaxRetryShift times.
    private const int RetryFrames = 600;
    private const int MaxRetryShift = 6;

    // The number of styles from which the one used least recently is released even if it is in use.
    private const int MaxImpls = 65536;

    private static readonly ModuleLog Log = ModuleLog.Create<FontResolver>();

    private readonly FontFactory factory;
    private readonly InterfaceManager interfaceManager;
    private readonly Lock syncRoot = new();
    private readonly Dictionary<IFontFamilyId, Entry> entries = [];
    private readonly Dictionary<TextStyle, FontImpl> impls = [];
    private Entry? defaultEntry;
    private int defaultGeneration;
    private bool implsFull;
    private FontSizes.Sizes root;
    private (long Frame, CompositeFont? Target, float DeviceScale) rootKey = (-1, null, 0);

    [ServiceManager.ServiceConstructor]
    private FontResolver(FontFactory factory, InterfaceManager interfaceManager)
    {
        this.factory = factory;
        this.interfaceManager = interfaceManager;
        factory.DefaultChainChanged += this.OnDefaultChainChanged;
    }

    /// <summary>Raised once the fonts that depend on the default font have been made again.</summary>
    /// <remarks>Raised on the main thread, before a frame. Pointers stay the same, but text sizes and metrics may
    /// change.</remarks>
    public event Action? DefaultFontChanged;

    /// <summary>Gets the sizes of the default font, which the font stack starts from.</summary>
    /// <remarks>Kept on the main thread for a frame, while the default font and the device scale stay the same.
    /// </remarks>
    public FontSizes.Sizes Root
    {
        get
        {
            var key = (Frame: FontClock.Frame, Target: this.defaultEntry?.Slot.LoadedTarget, FontSizes.DeviceScale);
            if (ThreadSafety.IsMainThread && this.rootKey == key)
                return this.root;
            var root = FontSizes.Over(
                this.factory.DefaultTextStyle with { Size = StyleLength.Px(this.factory.DefaultSizePx) },
                key.Target,
                default);
            if (ThreadSafety.IsMainThread)
                (this.root, this.rootKey) = (root, key);
            return root;
        }
    }

    /// <summary>Gets the default font, waiting for it to load if needed.</summary>
    public CompositeFont DefaultTarget =>
        (this.defaultEntry ?? this.GetImpl(default).Entry).Slot.Target;

    /// <summary>Gets the font of a style, loading its family in the background if needed, without waiting.</summary>
    /// <param name="style">The style.</param>
    /// <param name="retry">Whether to load again if an earlier attempt failed or left fonts of the family out.</param>
    /// <returns>The font. It draws with the default font until loaded. <see cref="ImFontRef.LoadTask"/> says when it
    /// is loaded, or why it could not be.</returns>
    public ImFontRef Get(in TextStyle style, bool retry = false) => this.RefOf(this.GetImpl(style, retry));

    /// <summary>Gets the font behind an ImGui font made by this resolver.</summary>
    /// <param name="font">The ImGui font.</param>
    /// <returns>The font, or null if <paramref name="font"/> was not made here, or has been released.</returns>
    public ImFontRef? Find(ImFontPtr font)
    {
        if (font.IsNull)
            return null;
        using (this.syncRoot.EnterScope())
        {
            if (FontImpl.FromNative(font.Handle) is not { } impl)
                return null;
            impl.LastUsedFrame = FontClock.Frame;
            return this.RefOf(impl);
        }
    }

    /// <summary>Pushes the font of a style into the ImGui font stack, at its own size.</summary>
    /// <param name="style">The style. A default line height takes that of the current font.</param>
    /// <returns>A disposable object that pops the font.</returns>
    /// <remarks>Does not wait for the load. Until the family is loaded, and if it could not be loaded, the text is
    /// drawn with the default font.</remarks>
    public ImPopFontDisposable Push(in TextStyle style)
    {
        ThreadSafety.AssertMainThread();
        FontImpl? impl;
        Entry? entry;
        using (this.syncRoot.EnterScope())
        {
            entry = this.impls.TryGetValue(style, out impl)
                        ? impl.Entry
                        : this.entries.GetValueOrDefault(style.FamilyOrDefault);
        }

        return this.Push(style, impl, entry);
    }

    /// <summary>Pushes the current font with one field of its style changed, at the current size.</summary>
    /// <param name="change">The change. Its offset and length are not used.</param>
    /// <remarks>Pop it with <see cref="ImGui.PopFont"/>.</remarks>
    public void PushChange(in TextStyleRange change)
    {
        ThreadSafety.AssertMainThread();
        var current = FontImpl.FromNative(ImGui.GetFont().Handle) ?? this.GetImpl(default);
        ImGui.PushFont(current.With(change).Native, 0);
    }

    /// <summary>Pushes the current font with text read as another markup, at the current size.</summary>
    /// <param name="markup">The markup.</param>
    /// <remarks>Pop it with <see cref="ImGui.PopFont"/>.</remarks>
    public void PushMarkup(TextMarkup markup)
    {
        ThreadSafety.AssertMainThread();
        var current = FontImpl.FromNative(ImGui.GetFont().Handle) ?? this.GetImpl(default);
        ImGui.PushFont(current.WithMarkup(markup).Native, 0);
    }

    /// <summary>Gets the font of a style, making it if needed.</summary>
    /// <param name="style">The style.</param>
    /// <param name="retry">Whether to load the family again if an earlier attempt failed or left fonts out.</param>
    /// <returns>The font, counted as used in this frame.</returns>
    public FontImpl GetImpl(in TextStyle style, bool retry = false)
    {
        var frame = FontClock.Frame;
        using (this.syncRoot.EnterScope())
        {
            if (!this.impls.TryGetValue(style, out var impl))
            {
                var family = style.FamilyOrDefault;
                if (!this.entries.TryGetValue(family, out var entry))
                {
                    entry = new(family);
                    entry.Load = this.StartLoad(entry);
                    this.entries.Add(family, entry);
                    if (family is DalamudDefaultFontId)
                        this.defaultEntry = entry;
                }

                if (this.impls.Count >= MaxImpls)
                    this.ReleaseOldest();
                this.impls.Add(style, impl = new(this, style, entry));
            }

            if (retry && impl.Entry.IsIncomplete)
                impl.Entry.Load = this.StartLoad(impl.Entry);

            impl.LastUsedFrame = impl.Entry.LastUsedFrame = frame;
            return impl;
        }
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.factory.DefaultChainChanged -= this.OnDefaultChainChanged;
        using (this.syncRoot.EnterScope())
        {
            this.impls.Clear();
            this.entries.Clear();
        }
    }

    /// <summary>Pushes a font into the ImGui font stack, at its own size.</summary>
    /// <param name="impl">The font.</param>
    /// <returns>A disposable object that pops the font.</returns>
    /// <seealso cref="Push(in TextStyle)"/>
    internal ImPopFontDisposable Push(FontImpl impl) =>
        impl.Released ? this.Push(impl.Style) : this.Push(impl.Style, impl, impl.Entry);

    /// <summary>Gets the state of every font family alive, for debugging.</summary>
    /// <returns>One entry per family.</returns>
    internal EntryInfo[] Snapshot()
    {
        var now = FontClock.Frame;
        using (this.syncRoot.EnterScope())
        {
            return
            [
                .. this.entries.Values
                       .Select(e => new EntryInfo(
                                   e.Family,
                                   this.impls.Values.Count(i => i.Entry == e),
                                   e.Load.Status switch
                                   {
                                       TaskStatus.RanToCompletion when e.LoadErrors.Length != 0 =>
                                           $"Loaded, {e.LoadErrors.Length} left out",
                                       TaskStatus.RanToCompletion => "Loaded",
                                       TaskStatus.Faulted => $"Failed: {e.Exception?.Message}",
                                       _ => "Loading",
                                   },
                                   this.impls.Values.Any(i => i.Entry == e && i.Ref.TryGetTarget(out _)),
                                   now - e.LastUsedFrame)),
            ];
        }
    }

    /// <summary>Releases fonts that are neither held nor used lately, and tries again to load families that failed
    /// and are still used.</summary>
    /// <remarks>Called by <see cref="Glyphs.GlyphAtlasTextResources.OnFrameRendered"/>, after the clock counted the
    /// frame.</remarks>
    internal void OnFrameRendered()
    {
        var now = FontClock.Frame;
        if (now % SweepFrames != 0)
            return;

        using (this.syncRoot.EnterScope())
        {
            var released = 0;
            foreach (var (style, impl) in this.impls)
            {
                if (now - impl.LastUsedFrame <= GraceFrames || impl.Ref.TryGetTarget(out _))
                    continue;
                this.impls.Remove(style);
                impl.Released = true;
                released++;
            }

            var used = this.impls.Values.Select(static i => i.Entry).ToHashSet();
            foreach (var (family, entry) in this.entries)
            {
                if (!entry.Load.IsCompleted)
                    continue;
                if (!used.Contains(entry) && entry != this.defaultEntry && now - entry.LastUsedFrame > GraceFrames)
                    this.entries.Remove(family);
                else if (entry.Load.IsFaulted && now >= entry.RetryFrame)
                    entry.Load = this.StartLoad(entry);
            }

            if (released != 0)
                Log.Verbose("Released {Count} styles, {Left} remain", released, this.impls.Count);
        }
    }

    // Pushes the font of a style, reusing impl if the style needs no resolving. impl and entry are null if not
    // known yet.
    private ImPopFontDisposable Push(in TextStyle style, FontImpl? impl, Entry? entry)
    {
        ThreadSafety.AssertMainThread();
        var font = FontSizes.Over(style, entry?.Slot.LoadedTarget, FontSizes.Current(this.Root));
        var resolved = style with { LineHeight = font.LineHeight };
        if (font.LineHeight.Unit == StyleUnit.Px)
            resolved.Size = StyleLength.Px(font.Measure);
        if (impl is null || resolved != style)
            impl = this.GetImpl(resolved);
        else
            impl.Entry.LastUsedFrame = FontClock.Frame;
        ImGui.PushFont(impl.Native, font.PushPx);
        return new ImPopFontDisposable(font: true);
    }

    private void ReleaseOldest()
    {
        if (!this.implsFull)
        {
            this.implsFull = true;
            Log.Verbose("{Count} styles are in use. Styles in use are being replaced", this.impls.Count);
        }

        var oldest = this.impls.MinBy(static kv => kv.Value.LastUsedFrame);
        this.impls.Remove(oldest.Key);
        oldest.Value.Released = true;
    }

    // Called in the lock. The slot waits for the task it is given, so the task is made before it starts.
    private Task<CompositeFont> StartLoad(Entry entry)
    {
        var task = new Task<CompositeFont>(() => this.Load(entry));
        entry.Slot.Load = task;
        task.Start(TaskScheduler.Default);
        return task;
    }

    private CompositeFont Load(Entry entry)
    {
        try
        {
            while (true)
            {
                var generation = Volatile.Read(ref this.defaultGeneration);
                var report = new FontFactory.LoadReport();
                var font = this.factory.CreateFont(entry.Family, report);

                // Fonts of the family that could not be loaded are left out. Only a family none of whose fonts loaded
                // fails, as it would draw with nothing but the default font, or with nothing.
                if (report.NoneLoaded)
                {
                    entry.LoadErrors = [.. report.Errors];
                    throw new FontLoadException(
                        entry.Family,
                        report.Errors.Count == 1 ? report.Errors[0] : new AggregateException(report.Errors));
                }

                using (this.syncRoot.EnterScope())
                {
                    // Made again if the default font changed while this one was made from it. A change after this
                    // check finds the font loaded, as OnDefaultChainChanged looks for loaded fonts in the lock.
                    if (generation != Volatile.Read(ref this.defaultGeneration))
                        continue;

                    entry.Slot.Target = font;
                    entry.LoadErrors = [.. report.Errors];
                    entry.CountSuccess();
                    return font;
                }
            }
        }
        catch (Exception e)
        {
            var error = e as FontLoadException ?? new FontLoadException(entry.Family, e);
            var first = entry.CountFailure(FontClock.Frame);
            if (e is not FontLoadException)
            {
                entry.LoadErrors = [error];
                Log.Verbose(e, "Failed to load {Family}. It is drawn with the default font", entry.Family);
            }
            else if (first)
            {
                Log.Verbose(
                    "None of the fonts of {Family} could be loaded. It is drawn with the default font",
                    entry.Family);
            }

            throw error;
        }
    }

    // The font was touched in this frame, so a sweep does not release it before it is held.
    private ImFontRef RefOf(FontImpl impl)
    {
        using (this.syncRoot.EnterScope())
        {
            if (impl.Ref.TryGetTarget(out var existing))
                return existing;
            var fontRef = new ImFontRef(impl);
            impl.Ref.SetTarget(fontRef);
            return fontRef;
        }
    }

    private void OnDefaultChainChanged()
    {
        var generation = Interlocked.Increment(ref this.defaultGeneration);
        this.rootKey.Frame = -1;
        Entry[] loaded;
        using (this.syncRoot.EnterScope())
        {
            loaded = [.. this.entries.Values.Where(static x => x.Slot.LoadedTarget is not null)];

            // A family that failed may load now.
            foreach (var entry in this.entries.Values)
            {
                if (entry.Load.IsFaulted)
                    entry.Load = this.StartLoad(entry);
            }
        }

        // Make the new fonts in the background, and swap them in between frames. Every family takes the locale of the
        // UI language, so all of them are made again.
        _ = Task.Run(() =>
        {
            var rebuilt = new List<(Entry Entry, CompositeFont Font, FontLoadException[] Errors)>(loaded.Length);
            foreach (var entry in loaded)
            {
                // A later change makes them all again.
                if (generation != Volatile.Read(ref this.defaultGeneration))
                    return Task.CompletedTask;

                try
                {
                    var report = new FontFactory.LoadReport();
                    rebuilt.Add((entry, this.factory.CreateFont(entry.Family, report), [.. report.Errors]));
                }
                catch (Exception e)
                {
                    Log.Verbose(e, "Failed to make {Family} again, keeping the old font", entry.Family);
                }
            }

            return this.interfaceManager.RunBeforeImGuiRender(() =>
            {
                // A later change supersedes this one.
                if (generation != Volatile.Read(ref this.defaultGeneration))
                    return;
                foreach (var (entry, font, errors) in rebuilt)
                {
                    entry.Slot.Target = font;
                    entry.LoadErrors = errors;
                }

                this.DefaultFontChanged.InvokeSafely();
            });
        });
    }

    /// <summary>The state of a font family, for debugging.</summary>
    /// <param name="Family">The family.</param>
    /// <param name="Styles">The number of styles alive that use it.</param>
    /// <param name="State">Whether it is loading, loaded, or failed (and why).</param>
    /// <param name="Held">Whether an <see cref="ImFontRef"/> holds a style of it.</param>
    /// <param name="FramesSinceUse">Frames since it was last used.</param>
    internal readonly record struct EntryInfo(
        IFontFamilyId Family,
        int Styles,
        string State,
        bool Held,
        long FramesSinceUse);

    /// <summary>The font of a family.</summary>
    /// <param name="family">The family.</param>
    internal sealed class Entry(IFontFamilyId family)
    {
        private Task<CompositeFont> load = Task.FromResult<CompositeFont>(null!);
        private FontLoadException? exception;
        private volatile FontLoadException[] loadErrors = [];
        private long retryFrame;
        private int failures;

        /// <summary>Gets the family.</summary>
        public IFontFamilyId Family { get; } = family;

        /// <summary>Gets or sets the frame the font was last used in.</summary>
        /// <seealso cref="FontClock.Frame"/>
        public long LastUsedFrame { get; set; }

        /// <summary>Gets or sets the load, which faults with <see cref="FontLoadException"/>.</summary>
        public Task<CompositeFont> Load
        {
            get => this.load;
            set
            {
                this.exception = null;
                this.load = value;
            }
        }

        /// <summary>Gets why the last load failed, or null if it has not failed (yet).</summary>
        /// <remarks>Kept once read, as <see cref="Task.Exception"/> makes a new object on each read.</remarks>
        public FontLoadException? Exception =>
            this.load.IsFaulted
                ? this.exception ??= this.load.Exception?.InnerException as FontLoadException
                : null;

        /// <summary>Gets or sets why fonts of the family were left out, or why the whole font failed.</summary>
        /// <remarks>Set when a load completes. Empty if nothing was left out.</remarks>
        public FontLoadException[] LoadErrors
        {
            get => this.loadErrors;
            set => this.loadErrors = value;
        }

        /// <summary>Gets a value indicating whether a load again may make more of the font.</summary>
        public bool IsIncomplete =>
            this.load.IsFaulted || (this.load.IsCompletedSuccessfully && this.loadErrors.Length != 0);

        /// <summary>Gets the font. It is filled when the load completes.</summary>
        public SlotFont Slot { get; } = new(NameOf(family));

        /// <summary>Gets the frame from which a failed load is tried again, if the font is still used.</summary>
        public long RetryFrame => Interlocked.Read(ref this.retryFrame);

        /// <summary>Counts a failed load, and puts the next attempt off longer each time.</summary>
        /// <param name="frame">The frame it failed in.</param>
        /// <returns>Whether it is the first failure since the font last loaded.</returns>
        public bool CountFailure(long frame)
        {
            var failures = Interlocked.Increment(ref this.failures);
            var shift = Math.Min(failures - 1, MaxRetryShift);
            Interlocked.Exchange(ref this.retryFrame, frame + ((long)RetryFrames << shift));
            return failures == 1;
        }

        /// <summary>Counts a load that succeeded.</summary>
        public void CountSuccess() => Volatile.Write(ref this.failures, 0);

        // The name of the slot. A family that cannot be named fails its load, not the call that asked for it.
        private static string NameOf(IFontFamilyId family)
        {
            try
            {
                return family.ToString() ?? nameof(IFontFamilyId);
            }
            catch (Exception)
            {
                return nameof(IFontFamilyId);
            }
        }
    }
}
