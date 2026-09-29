using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class ScanCadFacesOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.scan_cad_faces", "Scan CAD Faces", "ScanCadFaces");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ScanCadFacesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument to scan", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Surface Faces", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.SurfaceFaces?.Value ?? string.Empty), "SetStringArg"),
                new("Parameter set name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasParameterSetName ? request.ParameterSetName : string.Empty), "SetStringArg"),
                new("Enable exclusions?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasEnableExclusions || request.EnableExclusions), "SetBoolArg"),
                new("Wait for Completion", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasWaitForCompletion || request.WaitForCompletion), "SetBoolArg")
            ], []);
    }

    public static Api.ScanCadFacesResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
