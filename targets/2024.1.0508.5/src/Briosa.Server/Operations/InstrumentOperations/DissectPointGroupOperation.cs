using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class DissectPointGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.dissect_point_group", "Dissect Point Group", "DissectPointGroup");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DissectPointGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Group to Dissect", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupToDissect, "group_to_dissect",
                    WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Base Name for Disected Groups", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasBaseNameForDissectedGroups ? request.BaseNameForDissectedGroups : string.Empty),
                "SetStringArg")
        ], []);
    }

    public static Api.DissectPointGroupResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
