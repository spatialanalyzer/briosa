using Briosa.Server.Security;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class InstrumentOperationDescriptor
{
    private const string Service = "briosa.InstrumentOperations";

    public static OperationDescriptor ReadOnly(string operationId, string mpStep, string rpc) =>
        new(operationId, mpStep, Service, rpc, $"/{Service}/{rpc}", "read_only",
            Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static OperationDescriptor Mutating(string operationId, string mpStep, string rpc) =>
        new(operationId, mpStep, Service, rpc, $"/{Service}/{rpc}", "state_mutation",
            Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
}
