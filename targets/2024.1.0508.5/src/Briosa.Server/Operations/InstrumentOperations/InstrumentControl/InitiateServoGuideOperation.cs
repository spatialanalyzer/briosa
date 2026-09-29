using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class InitiateServoGuideOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.initiate_servo_guide", "Initiate Servo-Guide", "InitiateServoGuide");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.InitiateServoGuideRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Nominal Points", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.NominalPoints, "nominal_points"), "SetPointNameRefListArg"),
                new("Group name suffix", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasGroupNameSuffix ? request.GroupNameSuffix : string.Empty), "SetStringArg"),
                new("Target name suffix", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasTargetNameSuffix ? request.TargetNameSuffix : string.Empty), "SetStringArg"),
                new("Tolerance", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasTolerance ? request.Tolerance : 0), "SetDoubleArg")
            ], []);
    }

    public static Api.InitiateServoGuideResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
