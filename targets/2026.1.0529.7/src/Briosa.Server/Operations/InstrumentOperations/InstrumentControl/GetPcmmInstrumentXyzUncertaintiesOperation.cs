using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetPcmmInstrumentXyzUncertaintiesOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.ReadOnly(
        "instrument_operations.get_pcmm_instrument_xyz_uncertainties",
        "Get PCMM Instrument XYZ Uncertainties", "GetPcmmInstrumentXyzUncertainties");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("x_uncertainty", "X Uncertainty", WorkerMpValueKind.FloatingPoint),
        new("y_uncertainty", "Y Uncertainty", WorkerMpValueKind.FloatingPoint),
        new("z_uncertainty", "Z Uncertainty", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetPcmmInstrumentXyzUncertaintiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [
                new("X Uncertainty", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Y Uncertainty", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Z Uncertainty", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetPcmmInstrumentXyzUncertaintiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            XUncertainty = values[0].RequireValue<WorkerDoubleValue>().Value,
            YUncertainty = values[1].RequireValue<WorkerDoubleValue>().Value,
            ZUncertainty = values[2].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
