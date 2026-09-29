using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructBSplinesFromSurfacesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_b_splines_from_surfaces", "Construct B-Splines From Surfaces",
        "briosa.ConstructionOperations", "ConstructBSplinesFromSurfaces",
        "/briosa.ConstructionOperations/ConstructBSplinesFromSurfaces",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("b_spline_list", "B-Spline List", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.ConstructBSplinesFromSurfacesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var arguments = new List<WorkerMpInputArgument>();
        if (request.HasResultingBSplineNamePrefix)
            arguments.Add(new("Resulting B-Spline Name prefix (Optional)", WorkerMpValueKind.Text,
                new WorkerTextValue(request.ResultingBSplineNamePrefix), "SetStringArg"));
        arguments.Add(new("Surface List", WorkerMpValueKind.CollectionObjectNameList,
            CollectionObjectNameMapper.RequiredList(request.SurfaceList, "surface_list"), "SetCollectionObjectNameRefListArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, arguments,
            [new("B-Spline List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.ConstructBSplinesFromSurfacesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.ConstructBSplinesFromSurfacesResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.BSplineList.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
