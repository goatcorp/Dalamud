using Dalamud.Broker.Ipc;
using Dalamud.Broker.Win32;
using Jab;

namespace Dalamud.Broker.Commands;

internal static partial class LaunchCommand
{
    [ServiceProvider]
    [Singleton<AppContainerService>]
    private partial class ServiceProvider
    {
        
    }
}
