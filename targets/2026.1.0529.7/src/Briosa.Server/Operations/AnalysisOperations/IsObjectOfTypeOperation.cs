using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class IsObjectOfTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.is_object_of_type", "Is Object of Type",
        "briosa.AnalysisOperations", "IsObjectOfType", "/briosa.AnalysisOperations/IsObjectOfType",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant", "Resultant", WorkerMpValueKind.Logical)];

    public static WorkerMpCommand CreateCommand(Api.IsObjectOfTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var objectType = request.HasObjectType ? request.ObjectType : Api.ObjectType.Any;
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Object Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ObjectName, "object_name"), "SetCollectionObjectNameArg2"),
                new("Object Type", WorkerMpValueKind.ObjectType,
                    ObjectTypeMapper.Required(objectType, "object_type"), "SetObjectTypeArg")
            ],
            [new("Resultant", WorkerMpValueKind.Logical, "GetBoolArg")]);
    }

    public static Api.IsObjectOfTypeResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Resultant = completed.Execution.OutputValues[0].RequireValue<WorkerBooleanValue>().Value,
        Execution = completed.Details
    };
}
