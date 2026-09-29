using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowHideDimensionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_hide_dimension", "Show / Hide Dimension", "briosa.ViewControl",
        "ShowHideDimension", "/briosa.ViewControl/ShowHideDimension", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowHideDimensionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Dimension Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.DimensionName, "dimension_name"), "SetCollectionObjectNameArg2"),
            new("Show Dimension?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasShowDimension || request.ShowDimension), "SetBoolArg")
        ], []);
    }

    public static Api.ShowHideDimensionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
