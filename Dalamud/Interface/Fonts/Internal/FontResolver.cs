using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

    // Frames a family is kept after its last use, about 5 minutes at 60 frames per second. Loading it again costs far
    // more than making a style of it, and windows closed for a while use it again.
    private const int FamilyGraceFrames = 18000;

    private const int SweepFrames = 60;

    // Frames until a font that failed to load is tried again while it is used. Each failure doubles it, up to
    // 2^MaxRetryShift times.
    private const int RetryFrames = 600;
    private const int MaxRetryShift = 6;

    private const int ExplicitRetryFrames = 60;

    // Maximum cached styles before evicting the least recently used entry even while in use.
    private const int MaxImpls = 65536;

    // Maximum idle loaded families retained before evicting the least recently used entry.
    private const int MaxIdleEntries = 32;

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

    /// <summary>Raised once the fonts that depend on the default font have been rebuilt.</summary>
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
            var key = (Frame: FontClock.Frame, Target: this.defaultEntry?.LoadedTarget, FontSizes.DeviceScale);
            if (ThreadSafety.IsMainThread && this.rootKey == key)
                return this.root;
            var root = this.RootOver(key.Target, key.DeviceScale);
            if (ThreadSafety.IsMainThread)
                (this.root, this.rootKey) = (root, key);
            return root;
        }
    }

    /// <summary>Gets the default font, waiting for it to load if needed.</summary>
    public CompositeFont DefaultTarget =>
        (this.defaultEntry ?? this.GetImpl(default).Entry).Target;

    /// <summary>Gets the font of a style, loading its family in the background if needed, without waiting.</summary>
    /// <param name="style">The style.</param>
    /// <param name="retry">Whether to load again if an earlier attempt failed or left fonts of the family out.</param>
    /// <returns>The font. It draws with the default font until loaded. <see cref="ImFontRef.LoadTask"/> says when it
    /// is loaded, or why it could not be.</returns>
    public ImFontRef Get(in TextStyle style, bool retry = false) => this.GetOrCreateRef(this.GetImpl(style, retry));

    /// <summary>Gets the font behind an ImGui font made by this resolver.</summary>
    /// <param name="font">The ImGui font.</param>
    /// <returns>The font, or null if <paramref name="font"/> is null, is not Dalamud's, or is not of this frame, or if
    /// called off the main thread.</returns>
    public ImFontRef? Find(ImFontPtr font)
    {
        if (FontImpl.FindExact(font.Handle) is not { } impl)
            return null;
        impl.LastUsedFrame = FontClock.Frame;
        return this.GetOrCreateRef(impl);
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
    public void PushChange(in TextStyleRange change) => PushCurrent(this.CurrentOrDefault().With(change));

    /// <summary>Pushes a style made from the style of the current font, at the current size.</summary>
    /// <param name="style">The style. Its size and line height are not resolved again.</param>
    /// <remarks>Pop it with <see cref="ImGui.PopFont"/>.</remarks>
    public void PushDerived(in TextStyle style) => PushCurrent(this.GetImpl(style));

    /// <summary>Pushes the current font with text read as another markup, at the current size.</summary>
    /// <param name="markup">The markup.</param>
    /// <remarks>Pop it with <see cref="ImGui.PopFont"/>.</remarks>
    public void PushMarkup(TextMarkup markup) => PushCurrent(this.CurrentOrDefault().WithMarkup(markup));

    /// <summary>Gets the font of a style, making it if needed.</summary>
    /// <param name="style">The style.</param>
    /// <param name="retry">Whether to load the family again if an earlier attempt failed or left fonts out. It is
    /// ignored for about a second after the last load completed.</param>
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

            if (retry && impl.Entry.IsIncomplete && frame - impl.Entry.CompletedFrame >= ExplicitRetryFrames)
                impl.Entry.Load = this.StartLoad(impl.Entry);

            impl.LastUsedFrame = impl.Entry.LastUsedFrame = frame;
            return impl;
        }
    }

    /// <summary>Gets the sizes of the default font at a pixel density, waiting for it to load if needed.</summary>
    /// <param name="density">Device pixels per pre-scale pixel.</param>
    /// <returns>The sizes.</returns>
    public FontSizes.Sizes RootAt(float density) => this.RootOver(this.DefaultTarget, density);

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

    /// <summary>Reloads cached font families affected by added or removed registrations.</summary>
    /// <param name="registrations">The registrations.</param>
    /// <param name="registered">Whether they were registered. If not, their fonts draw with the default font at once.
    /// </param>
    internal void OnRegistrationsChanged(IReadOnlyCollection<FontRegistration> registrations, bool registered)
    {
        using (this.syncRoot.EnterScope())
        {
            foreach (var entry in this.entries.Values)
            {
                if (!entry.Family.Registrations.Any(registrations.Contains))
                    continue;
                if (registered && entry.Load.IsCompleted && !entry.IsIncomplete)
                    continue;

                entry.Generation++;
                if (!registered)
                {
                    entry.LoadedTarget = null;
                    entry.LoadErrors = [];
                }

                entry.Load = this.StartLoad(entry);
            }
        }
    }

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
                                       TaskStatus.Faulted =>
                                           $"Failed: {e.Load.Exception?.InnerException?.Message}",
                                       _ => "Loading",
                                   },
                                   this.impls.Values.Any(i => i.Entry == e && i.Ref.TryGetTarget(out _)),
                                   now - e.LastUsedFrame)),
            ];
        }
    }

    /// <summary>Releases fonts that are neither held nor used lately, and tries again to load families that failed
    /// and are still used.</summary>
    /// <remarks>Called by <see cref="Interface.Internal.InterfaceManager"/> once a frame is rendered, after the clock
    /// counted the frame.</remarks>
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
                // Uses that touch only the style, such as cached transitions and markups, count for the family too.
                impl.Entry.LastUsedFrame = Math.Max(impl.Entry.LastUsedFrame, impl.LastUsedFrame);
                if (now - impl.LastUsedFrame <= GraceFrames || impl.Ref.TryGetTarget(out _))
                    continue;
                this.impls.Remove(style);
                impl.Released = true;
                released++;
            }

            var used = this.UsedEntries();
            var idle = 0;
            foreach (var (family, entry) in this.entries)
            {
                if (!entry.Load.IsCompleted)
                    continue;
                if (used.Contains(entry))
                {
                    if (entry.Load.IsFaulted && now >= entry.RetryFrame)
                        entry.Load = this.StartLoad(entry);
                }
                else if (now - entry.LastUsedFrame > FamilyGraceFrames)
                {
                    this.entries.Remove(family);
                }
                else
                {
                    idle++;
                }
            }

            // Cap idle families because each retains loaded fonts and font data copies.
            for (; idle > MaxIdleEntries; idle--)
            {
                var oldest = this.entries.Values
                                 .Where(e => e.Load.IsCompleted && !used.Contains(e))
                                 .MinBy(static e => e.LastUsedFrame);
                if (oldest is null)
                    break;
                this.entries.Remove(oldest.Family);
            }

            if (released != 0)
                Log.Verbose("Released {Count} styles, {Left} remain", released, this.impls.Count);
        }
    }

    private static void PushCurrent(FontImpl impl) => ImGui.PushFont(impl.Native, 0);

    private FontImpl CurrentOrDefault()
    {
        ThreadSafety.AssertMainThread();
        return FontImpl.Current ?? this.GetImpl(default);
    }

    // Caller must hold syncRoot.
    private HashSet<Entry> UsedEntries()
    {
        var used = this.impls.Values.Select(static i => i.Entry).ToHashSet();
        if (this.defaultEntry is not null)
            used.Add(this.defaultEntry);
        return used;
    }

    // The sizes of the default font over the given font, at a pixel density. target is null if not loaded yet.
    private FontSizes.Sizes RootOver(CompositeFont? target, float density) =>
        FontSizes.Over(
            this.factory.DefaultTextStyle with { Size = StyleLength.Px(this.factory.DefaultSizePx) },
            target,
            default,
            density);

    // Pushes the font of a style, reusing impl if the style needs no resolving. impl and entry are null if not
    // known yet.
    private ImPopFontDisposable Push(in TextStyle style, FontImpl? impl, Entry? entry)
    {
        ThreadSafety.AssertMainThread();
        var font = FontSizes.Over(style, entry?.LoadedTarget, FontSizes.Current(this.Root));

        var resolved = style with { LineHeight = font.LineHeight, Size = StyleLength.Px(font.Measure) };
        if (impl is null || resolved != style)
            impl = this.GetImpl(resolved);
        else
            impl.Entry.LastUsedFrame = FontClock.Frame;
        ImGui.PushFont(impl.Native, font.PushPx);
        return new(font: true);
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

    // Called in the lock. Failures are observed, as Load logs them and the entry keeps them. Nothing may be waiting for
    // a retried load, and an unobserved failure would be logged as an error again.
    private Task<CompositeFont> StartLoad(Entry entry) => Task.Run(() => this.Load(entry)).ObserveFaults();

    private CompositeFont Load(Entry entry)
    {
        var entryGeneration = entry.Generation;
        FontLoadException[]? noneLoaded = null;
        try
        {
            while (true)
            {
                var generation = Volatile.Read(ref this.defaultGeneration);
                var report = new FontFactory.LoadReport();
                var font = this.factory.CreateFont(entry.Family, report);

                // Partial failures omit failed members. Only fail the family if none of its own fonts loaded.
                if (report.NoneLoaded)
                {
                    noneLoaded = [.. report.Errors];
                    throw new FontLoadException(
                        entry.Family,
                        report.Errors.Count == 1 ? report.Errors[0] : new AggregateException(report.Errors));
                }

                using (this.syncRoot.EnterScope())
                {
                    // Retry if the default chain changed mid-load. Changes after this lock see LoadedTarget set and rebuild it.
                    if (generation != Volatile.Read(ref this.defaultGeneration))
                        continue;

                    if (entryGeneration != entry.Generation)
                        return font;

                    entry.LoadedTarget = font;
                    entry.LoadErrors = [.. report.Errors];
                    entry.CountSuccess(FontClock.Frame);
                    return font;
                }
            }
        }
        catch (Exception e)
        {
            var error = e as FontLoadException ?? new FontLoadException(entry.Family, e);
            var first = false;
            using (this.syncRoot.EnterScope())
            {
                if (entryGeneration == entry.Generation)
                {
                    if (noneLoaded is not null)
                        entry.LoadErrors = noneLoaded;
                    else if (e is not FontLoadException)
                        entry.LoadErrors = [error];
                    first = entry.CountFailure(FontClock.Frame);
                }
            }

            if (e is not FontLoadException)
            {
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
    private ImFontRef GetOrCreateRef(FontImpl impl)
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
            // Idle families are dropped rather than made again.
            var used = this.UsedEntries();
            foreach (var (family, entry) in this.entries)
            {
                if (entry.Load.IsCompleted && !used.Contains(entry))
                    this.entries.Remove(family);
            }

            loaded = [.. this.entries.Values.Where(static x => x.LoadedTarget is not null)];

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
            var rebuilt =
                new List<(Entry Entry, int Generation, CompositeFont Font, FontLoadException[] Errors)>(loaded.Length);
            foreach (var entry in loaded)
            {
                // A later change makes them all again.
                if (generation != Volatile.Read(ref this.defaultGeneration))
                    return Task.CompletedTask;

                try
                {
                    var entryGeneration = entry.Generation;
                    var report = new FontFactory.LoadReport();
                    var font = this.factory.CreateFont(entry.Family, report);
                    if (report.NoneLoaded)
                        continue;

                    rebuilt.Add((entry, entryGeneration, font, [.. report.Errors]));
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
                foreach (var (entry, entryGeneration, font, errors) in rebuilt)
                {
                    if (entryGeneration != entry.Generation)
                        continue;
                    entry.LoadedTarget = font;
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
        private volatile Task<CompositeFont> load = Task.FromResult<CompositeFont>(null!);
        private volatile FontLoadException[] loadErrors = [];
        private volatile CompositeFont? loadedTarget;
        private long retryFrame;
        private long completedFrame;
        private int failures;
        private int generation;

        /// <summary>Gets the family.</summary>
        public IFontFamilyId Family { get; } = family;

        /// <summary>Gets or sets the frame the font was last used in.</summary>
        /// <seealso cref="FontClock.Frame"/>
        public long LastUsedFrame { get; set; }

        /// <summary>Gets or sets the generation incremented when a referenced registration is added or removed.</summary>
        /// <remarks>Changed in the lock of the resolver. A load that began before a change does not set the font.
        /// </remarks>
        public int Generation
        {
            get => Volatile.Read(ref this.generation);
            set => Volatile.Write(ref this.generation, value);
        }

        /// <summary>Gets or sets the load, which faults with <see cref="FontLoadException"/>.</summary>
        public Task<CompositeFont> Load
        {
            get => this.load;
            set => this.load = value;
        }

        /// <summary>Gets or sets why fonts of the family were left out, or why the whole font failed.</summary>
        /// <remarks>Set when a load completes. Empty if nothing was left out.</remarks>
        public FontLoadException[] LoadErrors
        {
            get => this.loadErrors;
            set => this.loadErrors = value;
        }

        /// <summary>Gets a value indicating whether retrying could load additional member fonts.</summary>
        public bool IsIncomplete =>
            this.load.IsFaulted || (this.load.IsCompletedSuccessfully && this.loadErrors.Length != 0);

        /// <summary>Gets or sets the loaded font, or null if no load has succeeded yet.</summary>
        public CompositeFont? LoadedTarget
        {
            get => this.loadedTarget;
            set => this.loadedTarget = value;
        }

        /// <summary>Gets the loaded font, waiting for the load if needed.</summary>
        public CompositeFont Target => this.LoadedTarget ?? this.Join();

        /// <summary>Gets the frame from which a failed load is tried again, if the font is still used.</summary>
        public long RetryFrame => Interlocked.Read(ref this.retryFrame);

        /// <summary>Gets the frame the last load completed in.</summary>
        public long CompletedFrame => Interlocked.Read(ref this.completedFrame);

        /// <summary>Gets the loaded font, waiting for the load if needed, or null if it failed.</summary>
        /// <returns>The loaded font, or null if the load failed.</returns>
        public CompositeFont? TryWaitTarget()
        {
            return this.LoadedTarget ?? this.Wait(this.load);
        }

        /// <summary>Records a load failure and schedules the next retry with exponential backoff.</summary>
        /// <param name="frame">The frame it failed in.</param>
        /// <returns>Whether it is the first failure since the font last loaded.</returns>
        public bool CountFailure(long frame)
        {
            var failures = Interlocked.Increment(ref this.failures);
            var shift = Math.Min(failures - 1, MaxRetryShift);
            Interlocked.Exchange(ref this.retryFrame, frame + ((long)RetryFrames << shift));
            Interlocked.Exchange(ref this.completedFrame, frame);
            return failures == 1;
        }

        /// <summary>Counts a load that succeeded.</summary>
        /// <param name="frame">The frame it completed in.</param>
        public void CountSuccess(long frame)
        {
            Volatile.Write(ref this.failures, 0);
            Interlocked.Exchange(ref this.completedFrame, frame);
        }

        // Waits for the load, on first use of a family that is still loading. The load runs on the thread pool and
        // does not need the main thread.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private CompositeFont Join()
        {
            var task = this.load;
            if (this.Wait(task) is { } loaded)
                return loaded;

            // Re-wrap the underlying cause in a fresh FontLoadException for this caller's stack trace.
            var cause = task.Exception?.InnerException;
            if (cause is FontLoadException { InnerException: { } inner })
                cause = inner;
            throw new FontLoadException(this.Family, cause ?? new InvalidOperationException("It is not loaded."));
        }

        private CompositeFont? Wait(Task<CompositeFont> task)
        {
            ((Task)task).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing).GetAwaiter().GetResult();
            return this.LoadedTarget;
        }
    }
}
