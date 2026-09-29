using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructBSplinesFromLinesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_b_splines_from_lines", "Construct B-Splines From Lines",
        "briosa.ConstructionOperations", "ConstructBSplinesFromLines", "/briosa.ConstructionOperations/ConstructBSplinesFromLines",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("b_spline_list", "B-Spline List", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.ConstructBSplinesFromLinesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var arguments = new List<WorkerMpInputArgument>();
        if (request.HasResultingBSplineNamePrefix)
        {
            arguments.Add(new("Resulting B-Spline Name prefix (Optional)", WorkerMpValueKind.Text,
                new WorkerTextValue(request.ResultingBSplineNamePrefix), "SetStringArg"));
        }
        arguments.Add(new("Line List", WorkerMpValueKind.CollectionObjectNameList,
            CollectionObjectNameMapper.RequiredList(request.LineList, "line_list"), "SetCollectionObjectNameRefListArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, arguments,
            [new("B-Spline List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.ConstructBSplinesFromLinesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.ConstructBSplinesFromLinesResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.BSplineList.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
