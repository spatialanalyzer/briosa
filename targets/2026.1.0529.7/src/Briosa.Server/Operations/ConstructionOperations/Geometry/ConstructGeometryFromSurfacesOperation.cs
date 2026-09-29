using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructGeometryFromSurfacesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_geometry_from_surfaces", "Construct Geometry From Surfaces",
        "briosa.ConstructionOperations", "ConstructGeometryFromSurfaces",
        "/briosa.ConstructionOperations/ConstructGeometryFromSurfaces",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("geometry_objects", "Geometry Objects", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.ConstructGeometryFromSurfacesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var arguments = new List<WorkerMpInputArgument>
        {
            new("Surfaces", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.Surfaces, "surfaces", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameRefListArg"),
            new("Minimum Diameter", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMinimumDiameter ? request.MinimumDiameter : 0), "SetDoubleArg"),
            new("Maximum Diameter", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMaximumDiameter ? request.MaximumDiameter : 0), "SetDoubleArg")
        };
        if (request.ReferenceFrame is not null)
            arguments.Add(new("Reference Frame", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReferenceFrame, "reference_frame", WorkerObjectTypeValue.Frame), "SetCollectionObjectNameArg2"));
        if (request.DestinationCollectionName is not null)
            arguments.Add(new("Destination Collection Name", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.DestinationCollectionName, "destination_collection_name"), "SetCollectionNameArg"));
        arguments.Add(new("Base Name", WorkerMpValueKind.Text,
            new WorkerTextValue(request.HasBaseName ? request.BaseName : "Geometry Object"), "SetStringArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, arguments,
            [new("Geometry Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.ConstructGeometryFromSurfacesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.ConstructGeometryFromSurfacesResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.GeometryObjects.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
