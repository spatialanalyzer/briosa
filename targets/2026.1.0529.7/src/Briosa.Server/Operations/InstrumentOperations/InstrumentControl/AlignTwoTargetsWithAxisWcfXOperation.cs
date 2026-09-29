using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class AlignTwoTargetsWithAxisWcfXOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.align_two_targets_with_axis_wcf_x", "Align Two Targets with Axis (WCF - X)",
        "briosa.InstrumentOperations", "AlignTwoTargetsWithAxisWcfX", "/briosa.InstrumentOperations/AlignTwoTargetsWithAxisWcfX",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AlignTwoTargetsWithAxisWcfXRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("First Point On Axis", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.FirstPointOnAxis, "first_point_on_axis"), "SetPointNameArg"),
            new("Second Point On Axis", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.SecondPointOnAxis, "second_point_on_axis"), "SetPointNameArg"),
            new("Initial Measured Group", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.InitialMeasuredGroup, "initial_measured_group", WorkerObjectTypeValue.PointGroup),
                "SetCollectionObjectNameArg2")
        };
        if (request.RotationalTolerance is not null)
        {
            inputs.Add(new("Rotational Tolerance - Optional", WorkerMpValueKind.ToleranceVectorOptions,
                ToleranceVectorOptionsMapper.Required(request.RotationalTolerance, "rotational_tolerance"),
                "SetToleranceVectorOptionsArg"));
        }

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.AlignTwoTargetsWithAxisWcfXResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
