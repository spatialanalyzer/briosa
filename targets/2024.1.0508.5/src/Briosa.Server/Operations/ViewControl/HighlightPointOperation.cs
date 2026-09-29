using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class HighlightPointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.highlight_point", "Highlight Point", "briosa.ViewControl",
        "HighlightPoint", "/briosa.ViewControl/HighlightPoint", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.HighlightPointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var point = request.PointName;
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Name (Empty to clear all)", WorkerMpValueKind.PointName,
                new WorkerPointNameValue(point?.CollectionName ?? string.Empty,
                    point?.GroupName ?? string.Empty, point?.TargetName ?? string.Empty), "SetPointNameArg"),
            new("Show Point?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasShowPoint && request.ShowPoint), "SetBoolArg")
        ], []);
    }

    public static Api.HighlightPointResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
