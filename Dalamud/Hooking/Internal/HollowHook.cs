namespace Dalamud.Hooking.Internal;

/// <summary>
/// Class facilitating hooks without a backend as part of a <see cref="HookStacker{T}"/> object.
/// </summary>
/// <typeparam name="T">Delegate of the hook.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="HollowHook{T}"/> class.
/// </remarks>
/// <param name="address">A memory address to install a hook.</param>
/// <param name="stacker">The hook stacker this hook is part of.</param>
internal class HollowHook<T>(nint address, HookStacker<T> stacker) : Hook<T>(address) where T : Delegate
{
    private T originalFunction;
    private bool isHookEnabled;
    private HookStacker<T> stacker = stacker;

    /// <summary>
    /// Action triggered when the original function changes.
    /// </summary>
    internal event Action? OriginalChanged;

    /// <inheritdoc/>
    public override T Original
    {
        get
        {
            this.CheckDisposed();
            return this.originalFunction;
        }
    }

    /// <inheritdoc/>
    public override bool IsEnabled
    {
        get
        {
            this.CheckDisposed();
            return this.isHookEnabled;
        }
    }

    /// <inheritdoc/>
    public override string BackendName => this.stacker.BackendName;

    /// <inheritdoc/>
    public override void Dispose()
    {
        if (this.IsDisposed)
            return;

        this.Disable();

        this.stacker.Remove(this);

        base.Dispose();
    }

    /// <inheritdoc/>
    public override void Enable()
    {
        this.CheckDisposed();
        this.isHookEnabled = true;
        this.stacker.UpdateDelegates();
    }

    /// <inheritdoc/>
    public override void Disable()
    {
        this.CheckDisposed();
        this.isHookEnabled = false;
        this.stacker.UpdateDelegates();
    }

    /// <summary>
    /// Sets the original function field.
    /// </summary>
    /// <param name="value">Original function. Delegate must have a same original function prototype.</param>
    internal void SetOriginal(T value)
    {
        this.CheckDisposed();
        this.originalFunction = value;
        this.OriginalChanged?.Invoke();
    }
}
