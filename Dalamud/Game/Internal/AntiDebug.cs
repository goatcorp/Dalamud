using System.Collections.Generic;

using Serilog;
#if !DEBUG
using Dalamud.Configuration.Internal;
#endif

namespace Dalamud.Game.Internal;

/// <summary>
/// This class disables anti-debug functionality in the game client.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed partial class AntiDebug : IServiceType
{
    private readonly byte[] nop = new byte[] { 0x31, 0xC0, 0x90, 0x90, 0x90, 0x90 };
    private byte[] original;
    private IntPtr debugCheckAddress;

    [ServiceManager.ServiceConstructor]
    private AntiDebug(TargetSigScanner targetSigScanner)
    {
        try
        {
            this.debugCheckAddress = targetSigScanner.ScanText("FF 15 ?? ?? ?? ?? 85 C0 74 11 41");
        }
        catch (KeyNotFoundException)
        {
            this.debugCheckAddress = IntPtr.Zero;
        }

        Log.Verbose($"Debug check address 0x{this.debugCheckAddress.ToInt64():X}");

        if (!this.IsEnabled)
        {
#if DEBUG
                this.Enable();
#else
            if (Service<DalamudConfiguration>.Get().IsAntiAntiDebugEnabled)
                this.Enable();
#endif
        }
    }

    /// <summary>
    /// Gets a value indicating whether the anti-debugging is enabled.
    /// </summary>
    public bool IsEnabled { get; private set; } = false;

    /// <summary>
    /// Enables the anti-debugging by overwriting code in memory.
    /// </summary>
    public void Enable()
    {
        this.original = new byte[this.nop.Length];
        if (this.debugCheckAddress != IntPtr.Zero && !this.IsEnabled)
        {
            Log.Information($"Overwriting debug check at 0x{this.debugCheckAddress.ToInt64():X}");
            SafeMemory.ReadBytes(this.debugCheckAddress, this.nop.Length, out this.original);
            SafeMemory.WriteBytes(this.debugCheckAddress, this.nop);
        }
        else
        {
            Log.Information("Debug check already overwritten?");
        }

        this.IsEnabled = true;
    }

    /// <summary>
    /// Disable the anti-debugging by reverting the overwritten code in memory.
    /// </summary>
    public void Disable()
    {
        if (this.debugCheckAddress != IntPtr.Zero && this.original != null)
        {
            Log.Information($"Reverting debug check at 0x{this.debugCheckAddress.ToInt64():X}");
            SafeMemory.WriteBytes(this.debugCheckAddress, this.original);
        }
        else
        {
            Log.Information("Debug check was not overwritten?");
        }

        this.IsEnabled = false;
    }
}

/// <summary>
/// Implementing IDisposable.
/// </summary>
internal sealed partial class AntiDebug : IDisposable
{
    private bool disposed = false;

    /// <summary>
    /// Finalizes an instance of the <see cref="AntiDebug"/> class.
    /// </summary>
    ~AntiDebug() => this.Dispose(false);

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">If this was disposed through calling Dispose() or from being finalized.</param>
    private void Dispose(bool disposing)
    {
        if (this.disposed)
            return;

        if (disposing)
        {
            // If anti-debug is enabled and is being disposed, odds are either the game is exiting, or Dalamud is being reloaded.
            // If it is the latter, there's half a chance a debugger is currently attached. There's no real need to disable the
            // check in either situation anyways. However if Dalamud is being reloaded, the sig may fail so may as well undo it.
            this.Disable();
        }

        this.disposed = true;
    }
}
