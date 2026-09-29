using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal static class GetIthVectorFromVectorGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "vector_operations.get_ith_vector_from_vector_group", "Get i-th Vector From Vector Group",
        "briosa.VectorOperations", "GetIthVectorFromVectorGroup",
        "/briosa.VectorOperations/GetIthVectorFromVectorGroup",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("vector_name", "Vector Name", WorkerMpValueKind.Text),
        new("begin_in_working", "Begin in Working", WorkerMpValueKind.Vector),
        new("end_in_working", "End in Working", WorkerMpValueKind.Vector),
        new("total_delta_in_working", "Total Delta in Working", WorkerMpValueKind.Vector),
        new("ijk_unit_vector_in_working", "ijk Unit Vector in Working", WorkerMpValueKind.Vector),
        new("magnitude", "Magnitude", WorkerMpValueKind.FloatingPoint)
    ];
    public static WorkerMpCommand CreateCommand(Api.GetIthVectorFromVectorGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroupName, "vector_group_name"), "SetCollectionObjectNameArg2"),
            new("Vector Index", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.VectorIndex), "SetIntegerArg")
        ],
        [
            new("Vector Name", WorkerMpValueKind.Text, "GetStringArg"),
            new("Begin in Working", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("End in Working", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("Total Delta in Working", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("ijk Unit Vector in Working", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("Magnitude", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
        ]);
    }
    public static Api.GetIthVectorFromVectorGroupResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            VectorName = values[0].RequireValue<WorkerTextValue>().Value,
            BeginInWorking = VectorMapper.ToProtocol(values[1].RequireValue<WorkerVectorValue>()),
            EndInWorking = VectorMapper.ToProtocol(values[2].RequireValue<WorkerVectorValue>()),
            TotalDeltaInWorking = VectorMapper.ToProtocol(values[3].RequireValue<WorkerVectorValue>()),
            IjkUnitVectorInWorking = VectorMapper.ToProtocol(values[4].RequireValue<WorkerVectorValue>()),
            Magnitude = values[5].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
