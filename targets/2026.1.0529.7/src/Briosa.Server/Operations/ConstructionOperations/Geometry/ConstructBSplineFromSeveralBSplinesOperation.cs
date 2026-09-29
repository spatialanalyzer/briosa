using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructBSplineFromSeveralBSplinesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_b_spline_from_several_b_splines", "Construct B-Spline From Several B-Splines",
        "briosa.ConstructionOperations", "ConstructBSplineFromSeveralBSplines", "/briosa.ConstructionOperations/ConstructBSplineFromSeveralBSplines",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructBSplineFromSeveralBSplinesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Resulting B-Spline Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingBSplineName, "resulting_b_spline_name", WorkerObjectTypeValue.BSpline), "SetCollectionObjectNameArg2"),
            new("B-Spline List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.BSplineList, "b_spline_list"), "SetCollectionObjectNameRefListArg"),
            new("Close Resulting B-Spline", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasCloseResultingBSpline && request.CloseResultingBSpline), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructBSplineFromSeveralBSplinesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
