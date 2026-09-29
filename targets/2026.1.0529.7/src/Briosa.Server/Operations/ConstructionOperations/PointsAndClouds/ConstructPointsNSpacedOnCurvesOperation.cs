using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsNSpacedOnCurvesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_n_spaced_on_curves", "Construct Points N-Spaced on Curves",
        "briosa.ConstructionOperations", "ConstructPointsNSpacedOnCurves", "/briosa.ConstructionOperations/ConstructPointsNSpacedOnCurves",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsNSpacedOnCurvesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("B-Spline List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.BSplineList, "b_spline_list"), "SetCollectionObjectNameRefListArg"),
            new("Number of Evenly Spaced Points", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasNumberOfEvenlySpacedPoints ? request.NumberOfEvenlySpacedPoints : 10), "SetIntegerArg"),
            new("Resultant Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultantGroupName, "resultant_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Resultant Point Name Prefix", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasResultantPointNamePrefix ? request.ResultantPointNamePrefix : string.Empty), "SetStringArg")
        ], []);
    }

    public static Api.ConstructPointsNSpacedOnCurvesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
