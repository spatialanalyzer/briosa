using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class SetGeometryRelationshipFitProfileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.set_geometry_relationship_fit_profile", "Set Geometry Relationship Fit Profile",
        "briosa.AnalysisOperations", "SetGeometryRelationshipFitProfile",
        "/briosa.AnalysisOperations/SetGeometryRelationshipFitProfile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetGeometryRelationshipFitProfileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Geometry Type", WorkerMpValueKind.GeometryType,
                    GeometryTypeMapper.Required(request.GeometryType, "geometry_type"), "SetGeometryTypeArg"),
                new("Relationship Ref List", WorkerMpValueKind.CollectionItemNameList,
                    CollectionItemNameMapper.RequiredList(request.RelationshipRefList, "relationship_ref_list"),
                    "SetCollectionObjectNameRefListArg"),
                new("Fit Profile Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasFitProfileName ? request.FitProfileName : string.Empty), "SetStringArg"),
                new("Apply Cardinal Point Settings", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasApplyCardinalPointSettings && request.ApplyCardinalPointSettings), "SetBoolArg")
            ], []);
    }

    public static Api.SetGeometryRelationshipFitProfileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
