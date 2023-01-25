using Grpc.Core;

namespace Dalamud.Broker.Ipc;

internal class IpcServiceBinder
{
    private BrokerDebuggingService BrokerDebuggingService { get; }
    
    public IpcServiceBinder(BrokerDebuggingService brokerDebuggingService)
    {
        this.BrokerDebuggingService = brokerDebuggingService;
    }
    
    public void BindServices(ServiceBinderBase binder)
    {
        BrokerDebugging.BindService(binder, this.BrokerDebuggingService);
    }
}
