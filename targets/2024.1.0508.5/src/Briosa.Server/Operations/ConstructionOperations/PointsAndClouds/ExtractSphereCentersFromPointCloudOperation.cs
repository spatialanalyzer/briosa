using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ExtractSphereCentersFromPointCloudOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.extract_sphere_centers_from_point_cloud", "Extract Sphere Centers from Point Cloud",
        "briosa.ConstructionOperations", "ExtractSphereCentersFromPointCloud", "/briosa.ConstructionOperations/ExtractSphereCentersFromPointCloud",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("number_of_points_extracted", "Number of Points Extracted", WorkerMpValueKind.WholeNumber)];

    public static WorkerMpCommand CreateCommand(Api.ExtractSphereCentersFromPointCloudRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CloudName, "cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2"),
            new("Desired Diameter", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasDesiredDiameter ? request.DesiredDiameter : 0), "SetDoubleArg"),
            new("Extraction Tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasExtractionTolerance ? request.ExtractionTolerance : 0), "SetDoubleArg"),
            new("Minimum Point Count", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasMinimumPointCount ? request.MinimumPointCount : 50), "SetIntegerArg"),
            new("Group Name for Points", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupNameForPoints, "group_name_for_points", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Perform Final Fit", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasPerformFinalFit || request.PerformFinalFit), "SetBoolArg"),
            new("Final Fit Cone Angle", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasFinalFitConeAngle ? request.FinalFitConeAngle : 120), "SetDoubleArg")
        ], [new("Number of Points Extracted", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.ExtractSphereCentersFromPointCloudResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            Execution = completed.Details,
            NumberOfPointsExtracted = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value
        };
}
