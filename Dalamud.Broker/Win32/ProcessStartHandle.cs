using Microsoft.Win32.SafeHandles;

namespace Dalamud.Broker.Win32;

internal sealed class ProcessStartHandle
{
    public required SafeProcessHandle Process { get; init; }
    public required SafeProcessHandle Thread { get; init; }
}
