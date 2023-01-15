using System.Diagnostics;
using Grpc.Core;
using Jab;

namespace Dalamud.Broker.Ipc;

[ServiceProvider]
[Singleton<DebugService>]
internal partial class IpcService
{
    public void BindServices(ServiceBinderBase binder)
    {
        BrokerDebugging.BindService(binder, this.GetService<DebugService>());
    }
}
