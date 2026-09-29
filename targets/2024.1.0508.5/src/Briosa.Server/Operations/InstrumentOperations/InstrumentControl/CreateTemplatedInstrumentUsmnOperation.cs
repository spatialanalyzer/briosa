using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class CreateTemplatedInstrumentUsmnOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.create_templated_instrument_usmn", "Create Templated Instrument (USMN)",
        "CreateTemplatedInstrumentUsmn");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CreateTemplatedInstrumentUsmnRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument Template Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.InstrumentTemplateName, "instrument_template_name"),
                "SetCollectionObjectNameArg2"),
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                CollectionInstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Overal Instrument Weight", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasOverallInstrumentWeight ? request.OverallInstrumentWeight : 1d), "SetDoubleArg"),
            new("Moving", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasMoving || request.Moving), "SetBoolArg"),
            new("Enable X", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasEnableX || request.EnableX), "SetBoolArg"),
            new("Enable Y", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasEnableY || request.EnableY), "SetBoolArg"),
            new("Enable Z", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasEnableZ || request.EnableZ), "SetBoolArg"),
            new("Enable Rx", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasEnableRx || request.EnableRx), "SetBoolArg"),
            new("Enable Ry", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasEnableRy || request.EnableRy), "SetBoolArg"),
            new("Enable Rz", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasEnableRz || request.EnableRz), "SetBoolArg"),
            new("Enable Scale", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasEnableScale && request.EnableScale), "SetBoolArg"),
            new("Enable Component Weights", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasEnableComponentWeights || request.EnableComponentWeights), "SetBoolArg"),
            new("Component 1 (Azimuth) Weight", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasComponent1Weight ? request.Component1Weight : 1d), "SetDoubleArg"),
            new("Component 2 (Elevation) Weight", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasComponent2Weight ? request.Component2Weight : 1d), "SetDoubleArg"),
            new("Component 3 (Distance) Weight", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasComponent3Weight ? request.Component3Weight : 1d), "SetDoubleArg")
        ], []);
    }

    public static Api.CreateTemplatedInstrumentUsmnResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
