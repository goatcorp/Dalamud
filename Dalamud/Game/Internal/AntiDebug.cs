using System.Collections.Generic;

#if !DEBUG
using Dalamud.Configuration.Internal;
#endif
using Serilog;

namespace Dalamud.Game.Internal;

/// <summary>
/// This class disables anti-debug functionality in the game client.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed class AntiDebug : IInternalDisposableService
{
    private readonly byte[] nop = new byte[] { 0x31, 0xC0, 0x90, 0x90, 0x90, 0x90 };
    private byte[] original;
    private IntPtr debugCheckAddress;

    [ServiceManager.ServiceConstructor]
    private AntiDebug(TargetSigScanner sigScanner)
    {
        try
        {
            this.debugCheckAddress = sigScanner.ScanText("FF 15 ?? ?? ?? ?? 85 C0 74 11 41");
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

    /// <summary>Finalizes an instance of the <see cref="AntiDebug"/> class.</summary>
    ~AntiDebug() => this.Disable();

    /// <summary>
    /// Gets a value indicating whether the anti-debugging is enabled.
    /// </summary>
    public bool IsEnabled { get; private set; } = false;

    /// <inheritdoc />
    void IInternalDisposableService.DisposeService() => this.Disable();

    /// <summary>
    /// Enables the anti-debugging by overwriting code in memory.
    /// </summary>
    public void Enable()
    {
        if (this.IsEnabled)
            return;

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
        if (!this.IsEnabled)
            return;

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
