using Grpc.Core;

namespace Dalamud.Broker.Ipc;

internal sealed class BrokerDebuggingService : BrokerDebugging.BrokerDebuggingBase
{
    public override Task<EchoReply> Echo(EchoRequest request, ServerCallContext context)
    {
        return Task.FromResult(new EchoReply
        {
            Message = $"PONG: {request.Message}",
        });
    }
}
