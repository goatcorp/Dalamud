using Microsoft.Win32.SafeHandles;

namespace Dalamud.Broker.Win32;

internal record struct ProcessHandle(SafeProcessHandle Process, SafeProcessHandle Thread);
