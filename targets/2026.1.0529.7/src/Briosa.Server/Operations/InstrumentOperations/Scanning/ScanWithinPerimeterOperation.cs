using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class ScanWithinPerimeterOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.scan_within_perimeter", "Scan within perimeter", "ScanWithinPerimeter");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ScanWithinPerimeterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument to scan", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Scan perimeter list", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.ScanPerimeters, "scan_perimeters"), "SetCollectionObjectNameRefListArg"),
                new("Exclusion perimeter list", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.ExclusionPerimeters, "exclusion_perimeters"), "SetCollectionObjectNameRefListArg"),
                new("Parameter set name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasParameterSetName ? request.ParameterSetName : string.Empty), "SetStringArg"),
                new("Group name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.PointGroup, "point_group", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
                new("Wait for Completion", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasWaitForCompletion || request.WaitForCompletion), "SetBoolArg")
            ], []);
    }

    public static Api.ScanWithinPerimeterResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
