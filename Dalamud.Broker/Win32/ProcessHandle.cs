using Microsoft.Win32.SafeHandles;

namespace Dalamud.Broker.Win32;

internal sealed class ProcessHandle : IDisposable
{
    public required SafeProcessHandle Process { get; init; }
    public required SafeProcessHandle Thread { get; init; }

    public void Dispose()
    {
        this.Process.Dispose();
        this.Thread.Dispose();
    }
}
