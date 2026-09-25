using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;

namespace Briosa.Server.Operations.WaveA;

internal static class MpOperationServiceExecutor
{
    public static Task<TResponse> ExecuteAsync<TRequest, TResponse>(
        OperationExecutor executor,
        TRequest request,
        ServerCallContext context,
        string operationId)
        where TRequest : class, IMessage<TRequest>
        where TResponse : class, IMessage<TResponse>, new()
    {
        ArgumentNullException.ThrowIfNull(executor);
        var operation = MpOperationCatalog.Get(operationId);
        return executor.ExecuteAsync(
            request,
            context,
            operation.Descriptor,
            operation.CreateCommand,
            operation.OutputContracts,
            operation.CreateResult<TResponse>);
    }
}
