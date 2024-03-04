using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

namespace Dalamud.Interface.Textures.Internal.SharedImmediateTextures;

/// <summary>Represents a texture that may have multiple reference holders (owners).</summary>
internal abstract class SharedImmediateTexture
    : ISharedImmediateTexture, IRefCountable, DynamicPriorityQueueLoader.IThrottleBasisProvider
{
    private const int SelfReferenceDurationTicks = 2000;
    private const long SelfReferenceExpiryExpired = long.MaxValue;

    private static long instanceCounter;

    private readonly object reviveLock = new();

    private bool resourceReleased;
    private int refCount;
    private long selfReferenceExpiry;
    private IDalamudTextureWrap? availableOnAccessWrapForApi9;
    private CancellationTokenSource? cancellationTokenSource;
    private NotOwnedTextureWrap? nonOwningWrap;

    /// <summary>Initializes a new instance of the <see cref="SharedImmediateTexture"/> class.</summary>
    /// <param name="sourcePathForDebug">Name of the underlying resource.</param>
    /// <remarks>The new instance is a placeholder instance.</remarks>
    protected SharedImmediateTexture(string sourcePathForDebug)
    {
        this.SourcePathForDebug = sourcePathForDebug;
        this.InstanceIdForDebug = Interlocked.Increment(ref instanceCounter);
        this.refCount = 0;
        this.selfReferenceExpiry = SelfReferenceExpiryExpired;
        this.ContentQueried = false;
        this.IsOpportunistic = true;
        this.resourceReleased = true;
        this.FirstRequestedTick = this.LatestRequestedTick = Environment.TickCount64;
    }

    /// <summary>Gets the list of owner plugins.</summary>
    public List<LocalPlugin> OwnerPlugins { get; } = new();

    /// <summary>Gets the instance ID. Debug use only.</summary>
    public long InstanceIdForDebug { get; }

    /// <summary>Gets the remaining time for self reference in milliseconds. Debug use only.</summary>
    public long SelfReferenceExpiresInForDebug =>
        this.selfReferenceExpiry == SelfReferenceExpiryExpired
            ? 0
            : Math.Max(0, this.selfReferenceExpiry - Environment.TickCount64);

    /// <summary>Gets the reference count. Debug use only.</summary>
    public int RefCountForDebug => this.refCount;

    /// <summary>Gets the source path. Debug use only.</summary>
    public string SourcePathForDebug { get; }

    /// <summary>Gets a value indicating whether this instance of <see cref="SharedImmediateTexture"/> supports revival.
    /// </summary>
    public bool HasRevivalPossibility => this.RevivalPossibility?.TryGetTarget(out _) is true;

    /// <summary>Gets or sets the underlying texture wrap.</summary>
    public Task<IDalamudTextureWrap>? UnderlyingWrap { get; set; }

    /// <inheritdoc/>
    public bool IsOpportunistic { get; private set; }

    /// <inheritdoc/>
    public long FirstRequestedTick { get; private set; }

    /// <inheritdoc/>
    public long LatestRequestedTick { get; private set; }

    /// <summary>Gets a value indicating whether the content has been queried,
    /// i.e. <see cref="TryGetWrap"/> or <see cref="RentAsync"/> is called.</summary>
    public bool ContentQueried { get; private set; }

    /// <summary>Gets a cancellation token for cancelling load.
    /// Intended to be called from implementors' constructors and <see cref="LoadUnderlyingWrap"/>.</summary>
    protected CancellationToken LoadCancellationToken => this.cancellationTokenSource?.Token ?? default;

    /// <summary>Gets or sets a weak reference to an object that demands this objects to be alive.</summary>
    /// <remarks>
    /// TextureManager must keep references to all shared textures, regardless of whether textures' contents are
    /// flushed, because API9 functions demand that the returned textures may be stored so that they can used anytime,
    /// possibly reviving a dead-inside object. The object referenced by this property is given out to such use cases,
    /// which gets created from <see cref="GetAvailableOnAccessWrapForApi9"/>. If this no longer points to an alive
    /// object, and <see cref="availableOnAccessWrapForApi9"/> is null, then this object is not used from API9 use case.
    /// </remarks>
    private WeakReference<IDalamudTextureWrap>? RevivalPossibility { get; set; }

    /// <inheritdoc/>
    public int AddRef() => this.TryAddRef(out var newRefCount) switch
    {
        IRefCountable.RefCountResult.StillAlive => newRefCount,
        IRefCountable.RefCountResult.AlreadyDisposed => throw new ObjectDisposedException(
                                                            nameof(SharedImmediateTexture)),
        IRefCountable.RefCountResult.FinalRelease => throw new InvalidOperationException(),
        _ => throw new InvalidOperationException(),
    };

    /// <inheritdoc/>
    public int Release()
    {
        switch (IRefCountable.AlterRefCount(-1, ref this.refCount, out var newRefCount))
        {
            case IRefCountable.RefCountResult.StillAlive:
                return newRefCount;

            case IRefCountable.RefCountResult.FinalRelease:
                // This case may not be entered while TryAddRef is in progress.
                // Note that IRefCountable.AlterRefCount guarantees that either TAR or Release will be called for one
                // generation of refCount; they never are called together for the same generation of refCount.
                // If TAR is called when refCount >= 1, and then Release is called, case StillAlive will be run.
                // If TAR is called when refCount == 0, and then Release is called:
                // ... * if TAR was done: case FinalRelease will be run.
                // ... * if TAR was not done: case AlreadyDisposed will be run.
                // ... Because refCount will be altered as the last step of TAR.
                // If Release is called when refCount == 1, and then TAR is called,
                // ... the resource may be released yet, so TAR waits for resourceReleased inside reviveLock,
                // ... while Release releases the underlying resource and then sets resourceReleased inside reviveLock.
                // ... Once that's done, TAR may revive the object safely.
                while (true)
                {
                    lock (this.reviveLock)
                    {
                        if (this.resourceReleased)
                        {
                            // I cannot think of a case that the code entering this code block, but just in case.
                            Thread.Yield();
                            continue;
                        }

                        this.cancellationTokenSource?.Cancel();
                        this.cancellationTokenSource = null;
                        this.nonOwningWrap = null;
                        this.ClearUnderlyingWrap();
                        this.resourceReleased = true;

                        return newRefCount;
                    }
                }

            case IRefCountable.RefCountResult.AlreadyDisposed:
                throw new ObjectDisposedException(nameof(SharedImmediateTexture));

            default:
                throw new InvalidOperationException();
        }
    }

    /// <summary>Releases self-reference, if conditions are met.</summary>
    /// <param name="immediate">If set to <c>true</c>, the self-reference will be released immediately.</param>
    /// <returns>Number of the new reference count that may or may not have changed.</returns>
    public int ReleaseSelfReference(bool immediate)
    {
        while (true)
        {
            var exp = this.selfReferenceExpiry;
            switch (immediate)
            {
                case false when exp > Environment.TickCount64:
                    return this.refCount;
                case true when exp == SelfReferenceExpiryExpired:
                    return this.refCount;
            }

            if (exp != Interlocked.CompareExchange(ref this.selfReferenceExpiry, SelfReferenceExpiryExpired, exp))
                continue;

            this.availableOnAccessWrapForApi9 = null;
            return this.Release();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDalamudTextureWrap GetWrapOrEmpty() => this.GetWrapOrDefault(Service<DalamudAssetManager>.Get().Empty4X4);

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(defaultWrap))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDalamudTextureWrap? GetWrapOrDefault(IDalamudTextureWrap? defaultWrap)
    {
        if (!this.TryGetWrap(out var texture, out _))
            texture = null;
        return texture ?? defaultWrap;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetWrap([NotNullWhen(true)] out IDalamudTextureWrap? texture, out Exception? exception)
    {
        ThreadSafety.AssertMainThread();
        return this.TryGetWrapCore(out texture, out exception);
    }

    /// <inheritdoc/>
    public async Task<IDalamudTextureWrap> RentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            this.AddRef();
        }
        finally
        {
            this.ContentQueried = true;
        }

        if (this.UnderlyingWrap is null)
            throw new InvalidOperationException("AddRef returned but UnderlyingWrap is null?");

        this.IsOpportunistic = false;
        this.LatestRequestedTick = Environment.TickCount64;
        var uw = this.UnderlyingWrap;
        if (cancellationToken != default)
        {
            while (!uw.IsCompleted)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    this.Release();
                    throw new OperationCanceledException(cancellationToken);
                }

                await Task.WhenAny(uw, Task.Delay(1000000, cancellationToken));
            }
        }

        IDalamudTextureWrap dtw;
        try
        {
            dtw = await uw;
        }
        catch
        {
            this.Release();
            throw;
        }

        return new RefCountableWrappingTextureWrap(dtw, this);
    }

    /// <summary>Gets a texture wrap which ensures that the values will be populated on access.</summary>
    /// <returns>The texture wrap, or null if failed.</returns>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    public IDalamudTextureWrap? GetAvailableOnAccessWrapForApi9()
    {
        if (this.availableOnAccessWrapForApi9 is not null)
            return this.availableOnAccessWrapForApi9;

        lock (this.reviveLock)
        {
            if (this.availableOnAccessWrapForApi9 is not null)
                return this.availableOnAccessWrapForApi9;

            if (this.RevivalPossibility?.TryGetTarget(out this.availableOnAccessWrapForApi9) is true)
                return this.availableOnAccessWrapForApi9;

            var newRefTask = this.RentAsync(this.LoadCancellationToken);
            newRefTask.Wait(this.LoadCancellationToken);
            if (!newRefTask.IsCompletedSuccessfully)
                return null;
            newRefTask.Result.Dispose();

            this.availableOnAccessWrapForApi9 = new AvailableOnAccessTextureWrap(this);
            this.RevivalPossibility = new(this.availableOnAccessWrapForApi9);
        }

        return this.availableOnAccessWrapForApi9;
    }

    /// <summary>Adds a plugin to <see cref="OwnerPlugins"/>, in a thread-safe way.</summary>
    /// <param name="plugin">The plugin to add.</param>
    public void AddOwnerPlugin(LocalPlugin plugin)
    {
        lock (this.OwnerPlugins)
        {
            if (!this.OwnerPlugins.Contains(plugin))
            {
                this.OwnerPlugins.Add(plugin);
                this.UnderlyingWrap?.ContinueWith(
                    r =>
                    {
                        if (r.IsCompletedSuccessfully)
                            Service<TextureManager>.Get().Blame(r.Result, plugin);
                    },
                    default(CancellationToken));
            }
        }
    }

    /// <inheritdoc/>
    public override string ToString() => $"{this.GetType().Name}#{this.InstanceIdForDebug}({this.SourcePathForDebug})";

    /// <summary>Cleans up this instance of <see cref="SharedImmediateTexture"/>.</summary>
    protected void ClearUnderlyingWrap()
    {
        _ = this.UnderlyingWrap?.ToContentDisposedTask(true);
        this.UnderlyingWrap = null;
    }

    /// <summary>Attempts to restore the reference to this texture.</summary>
    protected void LoadUnderlyingWrap()
    {
        int addLen;
        lock (this.OwnerPlugins)
        {
            this.UnderlyingWrap = Service<TextureManager>.Get().DynamicPriorityTextureLoader.LoadAsync(
                this,
                this.CreateTextureAsync,
                this.LoadCancellationToken);

            addLen = this.OwnerPlugins.Count;
        }

        if (addLen == 0)
            return;
        this.UnderlyingWrap.ContinueWith(
            r =>
            {
                if (!r.IsCompletedSuccessfully)
                    return;
                foreach (var op in this.OwnerPlugins.Take(addLen))
                    Service<TextureManager>.Get().Blame(r.Result, op);
            },
            default(CancellationToken));
    }

    /// <summary>Creates the texture.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task resulting in a loaded texture.</returns>
    protected abstract Task<IDalamudTextureWrap> CreateTextureAsync(CancellationToken cancellationToken);

    private IRefCountable.RefCountResult TryAddRef(out int newRefCount)
    {
        var alterResult = IRefCountable.AlterRefCount(1, ref this.refCount, out newRefCount);
        if (alterResult != IRefCountable.RefCountResult.AlreadyDisposed)
            return alterResult;

        while (true)
        {
            lock (this.reviveLock)
            {
                if (!this.resourceReleased)
                {
                    Thread.Yield();
                    continue;
                }

                alterResult = IRefCountable.AlterRefCount(1, ref this.refCount, out newRefCount);
                if (alterResult != IRefCountable.RefCountResult.AlreadyDisposed)
                    return alterResult;

                this.cancellationTokenSource = new();
                try
                {
                    this.LoadUnderlyingWrap();
                }
                catch
                {
                    this.cancellationTokenSource = null;
                    throw;
                }

                if (this.RevivalPossibility?.TryGetTarget(out var target) is true)
                    this.availableOnAccessWrapForApi9 = target;

                Interlocked.Increment(ref this.refCount);
                this.resourceReleased = false;
                return IRefCountable.RefCountResult.StillAlive;
            }
        }
    }

    /// <summary><see cref="ISharedImmediateTexture.TryGetWrap"/>, but without checking for thread.</summary>
    private bool TryGetWrapCore(
        [NotNullWhen(true)] out IDalamudTextureWrap? texture,
        out Exception? exception)
    {
        if (this.TryAddRef(out _) != IRefCountable.RefCountResult.StillAlive)
        {
            this.ContentQueried = true;
            texture = null;
            exception = new ObjectDisposedException(this.GetType().Name);
            return false;
        }

        this.ContentQueried = true;
        this.LatestRequestedTick = Environment.TickCount64;

        var nexp = Environment.TickCount64 + SelfReferenceDurationTicks;
        while (true)
        {
            var exp = this.selfReferenceExpiry;
            if (exp != Interlocked.CompareExchange(ref this.selfReferenceExpiry, nexp, exp))
                continue;

            // If below condition is met, the additional reference from above is for the self-reference.
            if (exp == SelfReferenceExpiryExpired)
                _ = this.AddRef();

            // Release the reference for rendering, after rendering ImGui.
            Service<InterfaceManager>.Get().EnqueueDeferredDispose(this);

            var uw = this.UnderlyingWrap;
            if (uw?.IsCompletedSuccessfully is true)
            {
                texture = this.nonOwningWrap ??= new(uw.Result, this);
                exception = null;
                return true;
            }

            texture = null;
            exception = uw?.Exception;
            return false;
        }
    }

    /// <summary>Same with <see cref="DisposeSuppressingTextureWrap"/>, but with a custom implementation of
    /// <see cref="CreateWrapSharingLowLevelResource"/>.</summary>
    private sealed class NotOwnedTextureWrap : DisposeSuppressingTextureWrap
    {
        private readonly IRefCountable owner;

        /// <summary>Initializes a new instance of the <see cref="NotOwnedTextureWrap"/> class.</summary>
        /// <param name="wrap">The inner wrap.</param>
        /// <param name="owner">The reference counting owner.</param>
        public NotOwnedTextureWrap(IDalamudTextureWrap wrap, IRefCountable owner)
            : base(wrap)
        {
            this.owner = owner;
        }

        /// <inheritdoc/>
        public override IDalamudTextureWrap CreateWrapSharingLowLevelResource()
        {
            var wrap = this.GetWrap();
            this.owner.AddRef();
            return new RefCountableWrappingTextureWrap(wrap, this.owner);
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(NotOwnedTextureWrap)}({this.owner})";
    }

    /// <summary>Reference counting texture wrap, to be used with <see cref="RentAsync"/>.</summary>
    private sealed class RefCountableWrappingTextureWrap : ForwardingTextureWrap
    {
        private IDalamudTextureWrap? innerWrap;
        private IRefCountable? owner;

        /// <summary>Initializes a new instance of the <see cref="RefCountableWrappingTextureWrap"/> class.</summary>
        /// <param name="wrap">The inner wrap.</param>
        /// <param name="owner">The reference counting owner.</param>
        public RefCountableWrappingTextureWrap(IDalamudTextureWrap wrap, IRefCountable owner)
        {
            this.innerWrap = wrap;
            this.owner = owner;
        }

        /// <summary>Finalizes an instance of the <see cref="RefCountableWrappingTextureWrap"/> class.</summary>
        ~RefCountableWrappingTextureWrap() => this.Dispose(false);

        /// <inheritdoc/>
        public override IDalamudTextureWrap CreateWrapSharingLowLevelResource()
        {
            var ownerCopy = this.owner;
            var wrapCopy = this.innerWrap;
            if (ownerCopy is null || wrapCopy is null)
                throw new ObjectDisposedException(nameof(RefCountableWrappingTextureWrap));

            ownerCopy.AddRef();
            return new RefCountableWrappingTextureWrap(wrapCopy, ownerCopy);
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(RefCountableWrappingTextureWrap)}({this.owner})";

        /// <inheritdoc/>
        protected override bool TryGetWrap(out IDalamudTextureWrap? wrap) => (wrap = this.innerWrap) is not null;

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            while (true)
            {
                if (this.owner is not { } ownerCopy)
                    return;
                if (ownerCopy != Interlocked.CompareExchange(ref this.owner, null, ownerCopy))
                    continue;

                // Note: do not dispose this; life of the wrap is managed by the owner.
                this.innerWrap = null;
                ownerCopy.Release();
            }
        }
    }

    /// <summary>A texture wrap that revives and waits for the underlying texture as needed on every access.</summary>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    private sealed class AvailableOnAccessTextureWrap : ForwardingTextureWrap
    {
        private readonly SharedImmediateTexture inner;

        /// <summary>Initializes a new instance of the <see cref="AvailableOnAccessTextureWrap"/> class.</summary>
        /// <param name="inner">The shared texture.</param>
        public AvailableOnAccessTextureWrap(SharedImmediateTexture inner) => this.inner = inner;

        /// <inheritdoc/>
        public override IDalamudTextureWrap CreateWrapSharingLowLevelResource()
        {
            this.inner.AddRef();
            try
            {
                if (!this.inner.TryGetWrapCore(out var wrap, out _))
                {
                    this.inner.UnderlyingWrap?.Wait();

                    if (!this.inner.TryGetWrapCore(out wrap, out _))
                    {
                        // Calling dispose on Empty4x4 is a no-op, so we can just return that.
                        this.inner.Release();
                        return Service<DalamudAssetManager>.Get().Empty4X4;
                    }
                }

                return new RefCountableWrappingTextureWrap(wrap, this.inner);
            }
            catch
            {
                this.inner.Release();
                throw;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(AvailableOnAccessTextureWrap)}({this.inner})";

        /// <inheritdoc/>
        protected override bool TryGetWrap(out IDalamudTextureWrap? wrap)
        {
            if (this.inner.TryGetWrapCore(out var t, out _))
                wrap = t;

            this.inner.UnderlyingWrap?.Wait();
            wrap = this.inner.nonOwningWrap ?? Service<DalamudAssetManager>.Get().Empty4X4;
            return true;
        }
    }
}
