using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeCollectionObjectNameRefListByTypeAndColorOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_collection_object_name_ref_list_by_type_and_color", "Make a Collection Object Name Ref List - By Type and Color",
        "briosa.ConstructionOperations", "MakeCollectionObjectNameRefListByTypeAndColor", "/briosa.ConstructionOperations/MakeCollectionObjectNameRefListByTypeAndColor",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_collection_object_name_list", "Resultant Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeCollectionObjectNameRefListByTypeAndColorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var color = request.ObjectColor;
        if (color is not null && (color.Red > byte.MaxValue || color.Green > byte.MaxValue || color.Blue > byte.MaxValue))
            throw new ArgumentOutOfRangeException(nameof(request), "Color channels must be in 0..255.");
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection", WorkerMpValueKind.Text, new WorkerTextValue(request.Collection), "SetStringArg"),
            new("Object Type", WorkerMpValueKind.ObjectType, ObjectTypeMapper.Required(request.ObjectType), "SetObjectTypeArg"),
            new("Object Color", WorkerMpValueKind.RgbColor,
                color is null ? new WorkerRgbColorValue(255, 0, 0) : new WorkerRgbColorValue((byte)color.Red, (byte)color.Green, (byte)color.Blue), "SetColorArg")
        ], [new("Resultant Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.MakeCollectionObjectNameRefListByTypeAndColorResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeCollectionObjectNameRefListByTypeAndColorResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.ResultantCollectionObjectNameList.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
