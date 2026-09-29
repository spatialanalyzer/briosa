using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal static class GetIthVectorFromVectorNameRefListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "vector_operations.get_ith_vector_from_vector_name_ref_list",
        "Get i-th Vector From Vector Name Ref List", "briosa.VectorOperations",
        "GetIthVectorFromVectorNameRefList",
        "/briosa.VectorOperations/GetIthVectorFromVectorNameRefList",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("vector_group_name", "Vector Group Name", WorkerMpValueKind.CollectionObjectName),
        new("vector_name", "Vector Name", WorkerMpValueKind.Text),
        new("begin_in_working", "Begin in Working", WorkerMpValueKind.Vector),
        new("end_in_working", "End in Working", WorkerMpValueKind.Vector),
        new("total_delta_in_working", "Total Delta in Working", WorkerMpValueKind.Vector),
        new("ijk_unit_vector_in_working", "ijk Unit Vector in Working", WorkerMpValueKind.Vector),
        new("magnitude", "Magnitude", WorkerMpValueKind.FloatingPoint)
    ];
    public static WorkerMpCommand CreateCommand(Api.GetIthVectorFromVectorNameRefListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Vector Name List", WorkerMpValueKind.VectorNameList,
                VectorNameMapper.RequiredList(request.VectorNameList, "vector_name_list"), "SetVectorNameRefListArg"),
            new("Vector Index", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.VectorIndex), "SetIntegerArg")
        ],
        [
            new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                "GetCollectionObjectNameArg", WorkerObjectTypeValue.VectorGroup),
            new("Vector Name", WorkerMpValueKind.Text, "GetStringArg"),
            new("Begin in Working", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("End in Working", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("Total Delta in Working", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("ijk Unit Vector in Working", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("Magnitude", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
        ]);
    }
    public static Api.GetIthVectorFromVectorNameRefListResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            VectorGroupName = CollectionObjectNameMapper.ToProtocol(values[0].RequireValue<WorkerCollectionObjectNameValue>()),
            VectorName = values[1].RequireValue<WorkerTextValue>().Value,
            BeginInWorking = VectorMapper.ToProtocol(values[2].RequireValue<WorkerVectorValue>()),
            EndInWorking = VectorMapper.ToProtocol(values[3].RequireValue<WorkerVectorValue>()),
            TotalDeltaInWorking = VectorMapper.ToProtocol(values[4].RequireValue<WorkerVectorValue>()),
            IjkUnitVectorInWorking = VectorMapper.ToProtocol(values[5].RequireValue<WorkerVectorValue>()),
            Magnitude = values[6].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
