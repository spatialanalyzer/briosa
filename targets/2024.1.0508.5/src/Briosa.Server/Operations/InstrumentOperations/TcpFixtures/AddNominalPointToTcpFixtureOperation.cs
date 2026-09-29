using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class AddNominalPointToTcpFixtureOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.add_nominal_point_to_tcp_fixture", "Add Nominal Point to TCP Fixture", "AddNominalPointToTcpFixture");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AddNominalPointToTcpFixtureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("TCP Fixture", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.TcpFixture, "tcp_fixture"), "SetCollectionObjectNameArg2"),
                new("Nominal Point Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.NominalPointName), "SetStringArg"),
                new("Nominal Point Location", WorkerMpValueKind.Vector,
                    VectorMapper.Required(request.NominalPointLocation, "nominal_point_location"), "SetVectorArg"),
                new("Var XX", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.VarXx), "SetDoubleArg"),
                new("Var YY", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.VarYy), "SetDoubleArg"),
                new("Var ZZ", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.VarZz), "SetDoubleArg"),
                new("CoVar XY", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.CovarXy), "SetDoubleArg"),
                new("CoVar XZ", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.CovarXz), "SetDoubleArg"),
                new("CoVar YZ", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.CovarYz), "SetDoubleArg")
            ], []);
    }

    public static Api.AddNominalPointToTcpFixtureResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
