using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsFromSurfacesOnUvGridOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_from_surfaces_on_uv_grid", "Construct Points From Surfaces On UV Grid",
        "briosa.ConstructionOperations", "ConstructPointsFromSurfacesOnUvGrid",
        "/briosa.ConstructionOperations/ConstructPointsFromSurfacesOnUvGrid",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsFromSurfacesOnUvGridRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var mode = request.EdgePointMode switch
        {
            Api.EdgePointMode.IncludeEdges => WorkerEdgeModeValue.IncludeEdges,
            Api.EdgePointMode.ExcludeEdges => WorkerEdgeModeValue.ExcludeEdges,
            Api.EdgePointMode.EdgesOnly => WorkerEdgeModeValue.EdgesOnly,
            _ => throw new ArgumentException("A supported edge_point_mode is required.", nameof(request))
        };
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Surface List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.SurfaceList, "surface_list"), "SetCollectionObjectNameRefListArg"),
            new("UV Point Group Base Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasUvPointGroupBaseName ? request.UvPointGroupBaseName : "UV Points"), "SetStringArg"),
            new("Make Each Line Separate Group?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasMakeEachLineSeparateGroup && request.MakeEachLineSeparateGroup), "SetBoolArg"),
            new("Number of U Grids", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasNumberOfUGrids ? request.NumberOfUGrids : 5), "SetIntegerArg"),
            new("Number of V Grids", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasNumberOfVGrids ? request.NumberOfVGrids : 5), "SetIntegerArg"),
            new("Edge Point Mode", WorkerMpValueKind.EdgeMode,
                new WorkerChoiceValue<WorkerEdgeModeValue>(mode), "SetEdgeModeArg")
        ], []);
    }

    public static Api.ConstructPointsFromSurfacesOnUvGridResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
