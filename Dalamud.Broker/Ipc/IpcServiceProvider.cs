using System.Diagnostics;
using Grpc.Core;
using Jab;

namespace Dalamud.Broker.Ipc;

// [Singleton<DebugService>]
internal partial class IpcServiceProvider
{
    public void BindServices(ServiceBinderBase binder)
    {
        // BrokerDebugging.BindService(binder, this.GetService<DebugService>());
    }
}
