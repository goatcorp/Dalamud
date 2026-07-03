using System.Threading.Tasks;

namespace Dalamud.NativeUi.BaseTypes.Addon;

/// <summary>
/// .
/// </summary>
internal partial class NativeAddon : IDisposable, IAsyncDisposable
{
    private bool isDisposed;

    /// <summary>
    /// Finalizes an instance of the <see cref="NativeAddon"/> class.
    /// This can only be called by the GC, and shouldn't happen except for OverlayAddons, which gets promptly ignored.
    /// </summary>
    ~NativeAddon()
    {
        this.Log.Warning("Addon Title: {title} InternalName: {internalName} was disposed via GC.", this.Title.ToString(), this.InternalName);
        Task.Run(this.DisposeAsync);
    }

    /// <summary>
    /// Triggers the disposal of this addon.
    /// This will not await the addon to actually close which happens several frames later
    /// due to the addons closing animation. If you need to fully wait for the window to close use <see cref="DisposeAsync"/> and await the result.
    /// </summary>
    public virtual void Dispose()
    {
        if (!this.isDisposed)
        {
            this.Log.Debug("Disposing addon {GetType}", this.GetType());

            this.Close();

            // Close will remove this node automatically on AtkUnitBase.Finalize,
            // However, this is after the plugin unloads,
            // and will trigger a warning in auto-dispose if we don't remove this now.
            GC.SuppressFinalize(this);
        }

        this.isDisposed = true;
    }

    /// <summary>
    /// Triggers the disposal of this addon, and awaits for it to fully close before returning <see cref="ValueTask.CompletedTask"/>.
    /// </summary>
    /// <remarks>
    /// This <em>must not</em> be called from the main thread, or it will deadlock the game.
    /// </remarks>
    /// <returns>A task that is waiting for the window to fully close.</returns>
    public virtual async ValueTask DisposeAsync()
    {
        if (!this.isDisposed)
        {
            this.Log.Debug("Disposing addon {GetType}", this.GetType());

            await this.CloseAsync();

            GC.SuppressFinalize(this);
        }

        this.isDisposed = true;
    }
}
