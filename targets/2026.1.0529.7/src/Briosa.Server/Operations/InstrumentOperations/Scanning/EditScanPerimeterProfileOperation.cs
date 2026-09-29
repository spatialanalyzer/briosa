using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class EditScanPerimeterProfileOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.edit_scan_perimeter_profile", "Edit Scan Perimeter Profile", "EditScanPerimeterProfile");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.EditScanPerimeterProfileRequest request)
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
                new("Profile name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasProfileName ? request.ProfileName : string.Empty), "SetStringArg"),
                new("Clear Profile?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasClearProfile || request.ClearProfile), "SetBoolArg"),
                new("Create New Profile?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasCreateNewProfile && request.CreateNewProfile), "SetBoolArg")
            ], []);
    }

    public static Api.EditScanPerimeterProfileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
