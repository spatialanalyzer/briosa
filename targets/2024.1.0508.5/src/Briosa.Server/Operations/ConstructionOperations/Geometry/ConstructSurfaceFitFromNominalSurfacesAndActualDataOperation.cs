using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructSurfaceFitFromNominalSurfacesAndActualDataOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_surface_fit_from_nominal_surfaces_and_actual_data", "Construct Surface Fit From Nominal Surfaces and Actual Data",
        "briosa.ConstructionOperations", "ConstructSurfaceFitFromNominalSurfacesAndActualData",
        "/briosa.ConstructionOperations/ConstructSurfaceFitFromNominalSurfacesAndActualData",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructSurfaceFitFromNominalSurfacesAndActualDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Nominal Surface", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NominalSurface, "nominal_surface", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2"),
            new("Actual Data Point List", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.ActualDataPointList, "actual_data_point_list"), "SetPointNameRefListArg"),
            new("Resulting Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingSurfaceName, "resulting_surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructSurfaceFitFromNominalSurfacesAndActualDataResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
