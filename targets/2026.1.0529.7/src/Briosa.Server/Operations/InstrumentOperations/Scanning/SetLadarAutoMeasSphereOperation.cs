using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetLadarAutoMeasSphereOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_ladar_auto_meas_sphere", "Set LADAR AutoMeas Sphere", "SetLadarAutoMeasSphere");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetLadarAutoMeasSphereRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Sphere Radius", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasSphereRadius ? request.SphereRadius : 1.1875), "SetDoubleArg"),
                new("Scan Line Spacing", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasScanLineSpacing ? request.ScanLineSpacing : 0.05), "SetDoubleArg"),
                new("Send Center Point?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasSendCenterPoint ? request.SendCenterPoint : true), "SetBoolArg"),
                new("Send Sphere?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasSendSphere && request.SendSphere), "SetBoolArg"),
                new("Send Measured Cloud?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasSendMeasuredCloud && request.SendMeasuredCloud), "SetBoolArg")
            ], []);
    }

    public static Api.SetLadarAutoMeasSphereResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
