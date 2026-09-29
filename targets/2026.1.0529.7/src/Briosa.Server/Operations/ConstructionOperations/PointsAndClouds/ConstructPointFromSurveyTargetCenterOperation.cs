using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointFromSurveyTargetCenterOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_from_survey_target_center", "Construct Point From Survey Target Center",
        "briosa.ConstructionOperations", "ConstructPointFromSurveyTargetCenter", "/briosa.ConstructionOperations/ConstructPointFromSurveyTargetCenter",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointFromSurveyTargetCenterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var surveyTargetType = request.HasSurveyTargetType ? request.SurveyTargetType : Api.SurveyTargetType.Triangle;
        var surveyTargetTypeText = surveyTargetType switch
        {
            Api.SurveyTargetType.Triangle => "Triangle",
            Api.SurveyTargetType.Circle => "Circle",
            _ => throw new ArgumentException("Survey target type must be Triangle or Circle.", nameof(request))
        };
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Containing Target", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CloudContainingTarget, "cloud_containing_target", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2"),
            new("Reference Seed Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.ReferenceSeedPoint, "reference_seed_point"), "SetPointNameArg"),
            new("Survey Target Type", WorkerMpValueKind.Text, new WorkerTextValue(surveyTargetTypeText), "SetStringArg"),
            new("Search Diameter", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasSearchDiameter ? request.SearchDiameter : 0.0), "SetDoubleArg"),
            new("Result Center Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.ResultCenterPointName, "result_center_point_name"), "SetPointNameArg")
        ], []);
    }

    public static Api.ConstructPointFromSurveyTargetCenterResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
