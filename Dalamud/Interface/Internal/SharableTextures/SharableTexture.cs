using System.Threading;
using System.Threading.Tasks;

using Dalamud.Storage.Assets;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.SharableTextures;

/// <summary>
/// Represents a texture that may have multiple reference holders (owners).
/// </summary>
internal abstract class SharableTexture : IRefCountable
{
    private const int SelfReferenceDurationTicks = 5000;
    private const long SelfReferenceExpiryExpired = long.MaxValue;

    private static long instanceCounter;

    private readonly object reviveLock = new();

    private bool resourceReleased;
    private int refCount;
    private long selfReferenceExpiry;
    private IDalamudTextureWrap? availableOnAccessWrapForApi9;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharableTexture"/> class.
    /// </summary>
    protected SharableTexture()
    {
        this.InstanceIdForDebug = Interlocked.Increment(ref instanceCounter);
        this.refCount = 1;
        this.selfReferenceExpiry = Environment.TickCount64 + SelfReferenceDurationTicks;
    }

    /// <summary>
    /// Gets the instance ID. Debug use only.
    /// </summary>
    public long InstanceIdForDebug { get; }

    /// <summary>
    /// Gets the remaining time for self reference in milliseconds. Debug use only.
    /// </summary>
    public long SelfReferenceExpiresInForDebug =>
        this.selfReferenceExpiry == SelfReferenceExpiryExpired
            ? 0
            : Math.Max(0, this.selfReferenceExpiry - Environment.TickCount64);

    /// <summary>
    /// Gets the reference count. Debug use only.
    /// </summary>
    public int RefCountForDebug => this.refCount;

    /// <summary>
    /// Gets the source path. Debug use only.
    /// </summary>
    public abstract string SourcePathForDebug { get; }

    /// <summary>
    /// Gets a value indicating whether this instance of <see cref="SharableTexture"/> supports revival.
    /// </summary>
    public bool HasRevivalPossibility => this.RevivalPossibility?.TryGetTarget(out _) is true;

    /// <summary>
    /// Gets or sets the underlying texture wrap.
    /// </summary>
    public Task<IDalamudTextureWrap>? UnderlyingWrap { get; set; }

    /// <summary>
    /// Gets or sets the dispose-suppressing wrap for <see cref="UnderlyingWrap"/>.
    /// </summary>
    protected DisposeSuppressingTextureWrap? DisposeSuppressingWrap { get; set; }

    /// <summary>
    /// Gets or sets a weak reference to an object that demands this objects to be alive.
    /// </summary>
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
        IRefCountable.RefCountResult.AlreadyDisposed => throw new ObjectDisposedException(nameof(SharableTexture)),
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

                        this.ReleaseResources();
                        this.resourceReleased = true;

                        return newRefCount;
                    }
                }

            case IRefCountable.RefCountResult.AlreadyDisposed:
                throw new ObjectDisposedException(nameof(SharableTexture));

            default:
                throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Releases self-reference, if conditions are met.
    /// </summary>
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

    /// <summary>
    /// Gets the texture if immediately available. The texture is guarnateed to be available for the rest of the frame.
    /// Invocation from non-main thread will exhibit an undefined behavior.
    /// </summary>
    /// <returns>The texture if available; <c>null</c> if not.</returns>
    public IDalamudTextureWrap? GetImmediate()
    {
        if (this.TryAddRef(out _) != IRefCountable.RefCountResult.StillAlive)
            return null;

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

            return this.DisposeSuppressingWrap;
        }
    }

    /// <summary>
    /// Creates a new reference to this texture. The texture is guaranteed to be available until
    /// <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    /// <returns>The task containing the texture.</returns>
    public Task<IDalamudTextureWrap> CreateNewReference()
    {
        this.AddRef();
        if (this.UnderlyingWrap is null)
            throw new InvalidOperationException("AddRef returned but UnderlyingWrap is null?");

        return this.UnderlyingWrap.ContinueWith(
            r =>
            {
                if (r.IsCompletedSuccessfully)
                    return Task.FromResult((IDalamudTextureWrap)new RefCountableWrappingTextureWrap(r.Result, this));

                this.Release();
                return r;
            }).Unwrap();
    }

    /// <summary>
    /// Gets a texture wrap which ensures that the values will be populated on access.
    /// </summary>
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

            var newRefTask = this.CreateNewReference();
            newRefTask.Wait();
            if (!newRefTask.IsCompletedSuccessfully)
                return null;
            newRefTask.Result.Dispose();

            this.availableOnAccessWrapForApi9 = new AvailableOnAccessTextureWrap(this);
            this.RevivalPossibility = new(this.availableOnAccessWrapForApi9);
        }

        return this.availableOnAccessWrapForApi9;
    }

    /// <summary>
    /// Cleans up this instance of <see cref="SharableTexture"/>.
    /// </summary>
    protected abstract void ReleaseResources();

    /// <summary>
    /// Attempts to restore the reference to this texture.
    /// </summary>
    protected abstract void ReviveResources();

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

                this.ReviveResources();
                if (this.RevivalPossibility?.TryGetTarget(out var target) is true)
                    this.availableOnAccessWrapForApi9 = target;

                Interlocked.Increment(ref this.refCount);
                this.resourceReleased = false;
                return IRefCountable.RefCountResult.StillAlive;
            }
        }
    }

    private sealed class RefCountableWrappingTextureWrap : IDalamudTextureWrap
    {
        private IDalamudTextureWrap? innerWrap;
        private IRefCountable? owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefCountableWrappingTextureWrap"/> class.
        /// </summary>
        /// <param name="wrap">The inner wrap.</param>
        /// <param name="owner">The reference counting owner.</param>
        public RefCountableWrappingTextureWrap(IDalamudTextureWrap wrap, IRefCountable owner)
        {
            this.innerWrap = wrap;
            this.owner = owner;
        }

        ~RefCountableWrappingTextureWrap() => this.Dispose();

        /// <inheritdoc/>
        public IntPtr ImGuiHandle => this.InnerWrapNonDisposed.ImGuiHandle;

        /// <inheritdoc/>
        public int Width => this.InnerWrapNonDisposed.Width;

        /// <inheritdoc/>
        public int Height => this.InnerWrapNonDisposed.Height;

        private IDalamudTextureWrap InnerWrapNonDisposed =>
            this.innerWrap ?? throw new ObjectDisposedException(nameof(RefCountableWrappingTextureWrap));

        /// <inheritdoc/>
        public void Dispose()
        {
            while (true)
            {
                if (this.owner is not { } ownerCopy)
                    return;
                if (ownerCopy != Interlocked.CompareExchange(ref this.owner, null, ownerCopy))
                    continue;
                this.innerWrap = null;
                ownerCopy.Release();
                GC.SuppressFinalize(this);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(RefCountableWrappingTextureWrap)}({this.owner})";
    }

    private sealed class AvailableOnAccessTextureWrap : IDalamudTextureWrap
    {
        private readonly SharableTexture inner;

        public AvailableOnAccessTextureWrap(SharableTexture inner) => this.inner = inner;

        /// <inheritdoc/>
        public IntPtr ImGuiHandle => this.GetActualTexture().ImGuiHandle;

        /// <inheritdoc/>
        public int Width => this.GetActualTexture().Width;

        /// <inheritdoc/>
        public int Height => this.GetActualTexture().Height;

        /// <inheritdoc/>
        public void Dispose()
        {
            // ignore
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(AvailableOnAccessTextureWrap)}({this.inner})";

        private IDalamudTextureWrap GetActualTexture()
        {
            if (this.inner.GetImmediate() is { } t)
                return t;

            this.inner.UnderlyingWrap?.Wait();
            return this.inner.DisposeSuppressingWrap ?? Service<DalamudAssetManager>.Get().Empty4X4;
        }
    }
}
