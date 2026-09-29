using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructCirclesLinesFromSurfacesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_circles_lines_from_surfaces", "Construct Circles (Lines) From Surfaces",
        "briosa.ConstructionOperations", "ConstructCirclesLinesFromSurfaces",
        "/briosa.ConstructionOperations/ConstructCirclesLinesFromSurfaces",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("geometry_objects", "Geometry Objects", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.ConstructCirclesLinesFromSurfacesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var mode = request.CircleLineMode switch
        {
            Api.CircleLineMode.Circle => "Circle",
            Api.CircleLineMode.Line => "Line",
            _ => throw new ArgumentException("A supported circle_line_mode is required.", nameof(request))
        };
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Surfaces", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.Surfaces, "surfaces"), "SetCollectionObjectNameRefListArg"),
            new("Minimum Diameter", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMinimumDiameter ? request.MinimumDiameter : 0), "SetDoubleArg"),
            new("Maximum Diameter", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMaximumDiameter ? request.MaximumDiameter : 0), "SetDoubleArg"),
            new("Tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasTolerance ? request.Tolerance : 0.02), "SetDoubleArg"),
            new("Single Surface?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasSingleSurface && request.SingleSurface), "SetBoolArg"),
            new("Circle Line Mode", WorkerMpValueKind.Text, new WorkerTextValue(mode), "SetStringArg"),
            new("Destination Collection Name", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.DestinationCollectionName, "destination_collection_name"), "SetCollectionNameArg"),
            new("Base Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasBaseName ? request.BaseName : "Geometry Object"), "SetStringArg")
        ], [new("Geometry Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.ConstructCirclesLinesFromSurfacesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.ConstructCirclesLinesFromSurfacesResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.GeometryObjects.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
