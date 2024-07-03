using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Textures.TextureWraps.Internal;
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
    private readonly List<LocalPlugin> ownerPlugins = new();

    private bool resourceReleased;
    private int refCount;
    private long selfReferenceExpiry;
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
        this.PublicUseInstance = new(this);
    }

    /// <summary>Gets a wrapper for this instance which disables resource reference management.</summary>
    public PureImpl PublicUseInstance { get; }

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

    /// <summary>Adds a plugin to <see cref="ownerPlugins"/>, in a thread-safe way.</summary>
    /// <param name="plugin">The plugin to add.</param>
    public void AddOwnerPlugin(LocalPlugin plugin)
    {
        lock (this.ownerPlugins)
        {
            if (!this.ownerPlugins.Contains(plugin))
            {
                this.ownerPlugins.Add(plugin);
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
        lock (this.ownerPlugins)
        {
            this.UnderlyingWrap = Service<TextureManager>.Get().DynamicPriorityTextureLoader.LoadAsync(
                this,
                this.CreateTextureAsync,
                this.LoadCancellationToken);

            addLen = this.ownerPlugins.Count;
        }

        if (addLen == 0)
            return;
        this.UnderlyingWrap.ContinueWith(
            r =>
            {
                if (!r.IsCompletedSuccessfully)
                    return;
                lock (this.ownerPlugins)
                {
                    foreach (var op in this.ownerPlugins.Take(addLen))
                        Service<TextureManager>.Get().Blame(r.Result, op);
                }
            },
            default(CancellationToken));
    }

    /// <summary>Creates the texture immediately.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task resulting in a loaded texture.</returns>
    /// <remarks>This function is intended to be called from texture load scheduler.
    /// See <see cref="LoadUnderlyingWrap"/> and note that this function is being used as the callback from
    /// <see cref="DynamicPriorityQueueLoader.LoadAsync{T}"/>.</remarks>
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

    /// <summary>A wrapper around <see cref="SharedImmediateTexture"/>, to prevent external consumers from mistakenly
    /// calling <see cref="IDisposable.Dispose"/> or <see cref="IRefCountable.Release"/>.</summary>
    internal sealed class PureImpl : ISharedImmediateTexture
    {
        private readonly SharedImmediateTexture inner;

        /// <summary>Initializes a new instance of the <see cref="PureImpl"/> class.</summary>
        /// <param name="inner">The actual instance.</param>
        public PureImpl(SharedImmediateTexture inner) => this.inner = inner;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDalamudTextureWrap GetWrapOrEmpty() =>
            this.inner.GetWrapOrEmpty();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull(nameof(defaultWrap))]
        public IDalamudTextureWrap? GetWrapOrDefault(IDalamudTextureWrap? defaultWrap = null) =>
            this.inner.GetWrapOrDefault(defaultWrap);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetWrap([NotNullWhen(true)] out IDalamudTextureWrap? texture, out Exception? exception) =>
            this.inner.TryGetWrap(out texture, out exception);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<IDalamudTextureWrap> RentAsync(CancellationToken cancellationToken = default) =>
            this.inner.RentAsync(cancellationToken);

        /// <inheritdoc cref="SharedImmediateTexture.AddOwnerPlugin"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddOwnerPlugin(LocalPlugin plugin) =>
            this.inner.AddOwnerPlugin(plugin);

        /// <inheritdoc/>
        public override string ToString() => $"{this.inner}({nameof(PureImpl)})";
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
}
