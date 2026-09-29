using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class ConstructPerimetersFromSurfaceFaceListOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.construct_perimeters_from_surface_face_list",
        "Construct Perimeters from Surface Face List", "ConstructPerimetersFromSurfaceFaceList");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("scan_perimeters", "Scan perimeter list", WorkerMpValueKind.CollectionObjectNameList),
        new("exclusion_perimeters", "Exclusion perimeter list", WorkerMpValueKind.CollectionObjectNameList)
    ];

    public static WorkerMpCommand CreateCommand(Api.ConstructPerimetersFromSurfaceFaceListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Selected Surface Faces", WorkerMpValueKind.Text,
                new WorkerTextValue(request.SurfaceFaces?.Value ?? string.Empty), "SetStringArg")],
            [
                new("Scan perimeter list", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg"),
                new("Exclusion perimeter list", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")
            ]);
    }

    public static Api.ConstructPerimetersFromSurfaceFaceListResult CreateResult(SuccessfulOperationExecution completed)
    {
        var perimeters = new Api.PerimeterLists();
        perimeters.ScanPerimeters.AddRange(completed.Execution.OutputValues[0]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values.Select(CollectionObjectNameMapper.ToProtocol));
        perimeters.ExclusionPerimeters.AddRange(completed.Execution.OutputValues[1]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values.Select(CollectionObjectNameMapper.ToProtocol));
        return new() { Perimeters = perimeters, Execution = completed.Details };
    }
}
