using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;

using ImGuiNET;

using Serilog;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Default implementation for <see cref="FontHandle"/>.
/// </summary>
internal abstract class FontHandle : IFontHandle
{
    private const int NonMainThreadFontAccessWarningCheckInterval = 10000;
    private static readonly ConditionalWeakTable<LocalPlugin, object> NonMainThreadFontAccessWarning = new();
    private static long nextNonMainThreadFontAccessWarningCheck;

    private readonly List<IDisposable> pushedFonts = new(8);

    private IFontHandleManager? manager;
    private long lastCumulativePresentCalls;

    /// <summary>
    /// Initializes a new instance of the <see cref="FontHandle"/> class.
    /// </summary>
    /// <param name="manager">An instance of <see cref="IFontHandleManager"/>.</param>
    protected FontHandle(IFontHandleManager manager)
    {
        this.manager = manager;
    }

    /// <inheritdoc/>
    public event IFontHandle.ImFontChangedDelegate? ImFontChanged;

    /// <summary>
    /// Event to be called on the first <see cref="IDisposable.Dispose"/> call.
    /// </summary>
    protected event Action? Disposed;

    /// <inheritdoc/>
    public Exception? LoadException => this.Manager.Substance?.GetBuildException(this);

    /// <inheritdoc/>
    public bool Available => (this.Manager.Substance?.GetFontPtr(this) ?? default).IsNotNullAndLoaded();

    /// <summary>
    /// Gets the associated <see cref="IFontHandleManager"/>.
    /// </summary>
    /// <exception cref="ObjectDisposedException">When the object has already been disposed.</exception>
    protected IFontHandleManager Manager =>
        this.manager
        ?? throw new ObjectDisposedException(
            this.GetType().Name,
            "Did you write `using (fontHandle)` instead of `using (fontHandle.Push())`?");

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.manager is null)
            return;

        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Invokes <see cref="IFontHandle.ImFontChanged"/>.
    /// </summary>
    /// <param name="font">The font, locked during the call of <see cref="ImFontChanged"/>.</param>
    public void InvokeImFontChanged(ILockedImFont font)
    {
        try
        {
            this.ImFontChanged?.Invoke(this, font);
        }
        catch (Exception e)
        {
            Log.Error(e, $"{nameof(this.InvokeImFontChanged)}: error");
        }
    }

    /// <summary>
    /// Obtains an instance of <see cref="ImFontPtr"/> corresponding to this font handle,
    /// to be released after rendering the current frame.
    /// </summary>
    /// <returns>The font pointer, or default if unavailble.</returns>
    /// <remarks>
    /// Behavior is undefined on access outside the main thread.
    /// </remarks>
    public ImFontPtr LockUntilPostFrame()
    {
        if (this.TryLock(out _) is not { } locked)
            return default;

        if (!ThreadSafety.IsMainThread && nextNonMainThreadFontAccessWarningCheck < Environment.TickCount64)
        {
            nextNonMainThreadFontAccessWarningCheck =
                Environment.TickCount64 + NonMainThreadFontAccessWarningCheckInterval;
            var stack = new StackTrace();
            if (Service<PluginManager>.GetNullable()?.FindCallingPlugin(stack) is { } plugin)
            {
                if (!NonMainThreadFontAccessWarning.TryGetValue(plugin, out _))
                {
                    NonMainThreadFontAccessWarning.Add(plugin, new());
                    Log.Warning(
                        "[IM] {pluginName}: Accessing fonts outside the main thread is deprecated.\n{stack}",
                        plugin.Name,
                        stack);
                }
            }
            else
            {
                // Dalamud internal should be made safe right now
                throw new InvalidOperationException("Attempted to access fonts outside the main thread.");
            }
        }

        Service<InterfaceManager>.Get().EnqueueDeferredDispose(locked);
        return locked.ImFont;
    }

    /// <summary>
    /// Attempts to lock the fully constructed instance of <see cref="ImFontPtr"/> corresponding to the this
    /// <see cref="IFontHandle"/>, for use in any thread.<br />
    /// Modification of the font will exhibit undefined behavior if some other thread also uses the font.
    /// </summary>
    /// <param name="errorMessage">The error message, if any.</param>
    /// <returns>
    /// An instance of <see cref="ILockedImFont"/> that <b>must</b> be disposed after use on success;
    /// <c>null</c> with <paramref name="errorMessage"/> populated on failure.
    /// </returns>
    public ILockedImFont? TryLock(out string? errorMessage)
    {
        IFontHandleSubstance? prevSubstance = default;
        while (true)
        {
            if (this.manager is not { } nonDisposedManager)
            {
                errorMessage = "The font handle has been disposed.";
                return null;
            }

            var substance = nonDisposedManager.Substance;

            // Does the associated IFontAtlas have a built substance?
            if (substance is null)
            {
                errorMessage = "The font atlas has not been built yet.";
                return null;
            }

            // Did we loop (because it did not have the requested font),
            // and are the fetched substance same between loops?
            if (substance == prevSubstance)
            {
                errorMessage = "The font atlas did not built the requested handle yet.";
                return null;
            }

            prevSubstance = substance;

            // Try to lock the substance.
            try
            {
                substance.DataRoot.AddRef();
            }
            catch (ObjectDisposedException)
            {
                // If it got invalidated, it's probably because a new substance is incoming. Try again.
                continue;
            }

            var fontPtr = substance.GetFontPtr(this);
            if (fontPtr.IsNull())
            {
                // The font for the requested handle is unavailable. Release the reference and try again.
                substance.DataRoot.Release();
                continue;
            }

            // Transfer the ownership of reference.
            errorMessage = null;
            return new LockedImFont(fontPtr, substance.DataRoot);
        }
    }

    /// <inheritdoc/>
    public ILockedImFont Lock() =>
        this.TryLock(out var errorMessage) ?? throw new InvalidOperationException(errorMessage);

    /// <inheritdoc/>
    public IDisposable Push()
    {
        ThreadSafety.AssertMainThread();

        // Warn if the client is not properly managing the pushed font stack.
        var cumulativePresentCalls = Service<InterfaceManager>.Get().CumulativePresentCalls;
        if (this.lastCumulativePresentCalls != cumulativePresentCalls)
        {
            this.lastCumulativePresentCalls = cumulativePresentCalls;
            if (this.pushedFonts.Count > 0)
            {
                Log.Warning(
                    $"{nameof(this.Push)} has been called, but the handle-private stack was not empty. " +
                    $"You might be missing a call to {nameof(this.Pop)}.");
                this.pushedFonts.Clear();
            }
        }

        var font = default(ImFontPtr);
        if (this.TryLock(out _) is { } locked)
        {
            font = locked.ImFont;
            Service<InterfaceManager>.Get().EnqueueDeferredDispose(locked);
        }

        var rented = SimplePushedFont.Rent(this.pushedFonts, font);
        this.pushedFonts.Add(rented);
        return rented;
    }

    /// <inheritdoc/>
    public void Pop()
    {
        ThreadSafety.AssertMainThread();
        this.pushedFonts[^1].Dispose();
    }

    /// <inheritdoc/>
    public Task<IFontHandle> WaitAsync()
    {
        if (this.Available)
            return Task.FromResult<IFontHandle>(this);

        var tcs = new TaskCompletionSource<IFontHandle>();
        this.ImFontChanged += OnImFontChanged;
        this.Disposed += OnDisposed;
        if (this.Available)
            OnImFontChanged(this, null);
        return tcs.Task;

        void OnImFontChanged(IFontHandle unused, ILockedImFont? unused2)
        {
            if (tcs.Task.IsCompletedSuccessfully)
                return;

            this.ImFontChanged -= OnImFontChanged;
            this.Disposed -= OnDisposed;
            try
            {
                tcs.SetResult(this);
            }
            catch
            {
                // ignore
            }
        }

        void OnDisposed()
        {
            if (tcs.Task.IsCompletedSuccessfully)
                return;

            this.ImFontChanged -= OnImFontChanged;
            this.Disposed -= OnDisposed;
            try
            {
                tcs.SetException(new ObjectDisposedException(nameof(GamePrebakedFontHandle)));
            }
            catch
            {
                // ignore
            }
        }
    }

    /// <summary>
    /// Implementation for <see cref="IDisposable.Dispose"/>.
    /// </summary>
    /// <param name="disposing">If <c>true</c>, then the function is being called from <see cref="IDisposable.Dispose"/>.</param>
    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Interlocked.Exchange(ref this.manager, null) is not { } managerToDisassociate)
                return;

            if (this.pushedFonts.Count > 0)
                Log.Warning($"{nameof(IFontHandle)}.{nameof(IDisposable.Dispose)}: fonts were still in a stack.");

            managerToDisassociate.FreeFontHandle(this);
            this.Disposed?.InvokeSafely();
            this.Disposed = null;
            this.ImFontChanged = null;
        }
    }
}
