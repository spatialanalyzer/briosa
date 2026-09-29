using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class ConstructTcpFixtureOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.construct_tcp_fixture", "Construct TCP Fixture", "ConstructTcpFixture");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resulting_tcp_fixture", "Resulting TCP Fixture", WorkerMpValueKind.CollectionObjectName)];

    public static WorkerMpCommand CreateCommand(Api.ConstructTcpFixtureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Requested TCP Fixture", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RequestedTcpFixture, "requested_tcp_fixture"),
                    "SetCollectionObjectNameArg2"),
                new("Point Match Threshold", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.PointMatchThreshold), "SetDoubleArg"),
                new("Replace Existing TCP Fixture", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasReplaceExistingTcpFixture && request.ReplaceExistingTcpFixture), "SetBoolArg")
            ],
            [new("Resulting TCP Fixture", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg")]);
    }

    public static Api.ConstructTcpFixtureResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ResultingTcpFixture = CollectionObjectNameMapper.ToProtocol(
            completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameValue>()),
        Execution = completed.Details
    };
}
