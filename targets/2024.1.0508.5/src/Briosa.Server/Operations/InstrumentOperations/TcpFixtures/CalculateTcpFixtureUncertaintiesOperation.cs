using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class CalculateTcpFixtureUncertaintiesOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.ReadOnly(
        "instrument_operations.calculate_tcp_fixture_uncertainties", "Calculate TCP Fixture Uncertainties", "CalculateTcpFixtureUncertainties");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("uncertainties", "Solution Valid", WorkerMpValueKind.Logical),
        new("uncertainties", "Refined TCP In Working Frame", WorkerMpValueKind.Transform),
        new("uncertainties", "Uncertainties in TCP Fixture Frame", WorkerMpValueKind.DoubleArray),
        new("uncertainties", "Uncertainties in Working Frame", WorkerMpValueKind.DoubleArray),
        new("uncertainties", "RMS Error", WorkerMpValueKind.FloatingPoint),
        new("uncertainties", "MAX Abs Error", WorkerMpValueKind.FloatingPoint),
        new("uncertainties", "Goodness Of Fit", WorkerMpValueKind.FloatingPoint),
        new("uncertainties", "Robustness", WorkerMpValueKind.FloatingPoint),
        new("uncertainties", "Result Notes", WorkerMpValueKind.EditText)
    ];

    public static WorkerMpCommand CreateCommand(Api.CalculateTcpFixtureUncertaintiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("TCP Fixture", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.TcpFixture, "tcp_fixture"), "SetCollectionObjectNameArg2"),
                new("TCP In Working Frame", WorkerMpValueKind.Transform,
                    TransformMapper.Required(request.TcpInWorking, "tcp_in_working"), "SetTransformArg"),
                new("TCP Measurements", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.TcpMeasurements, "tcp_measurements"), "SetPointNameRefListArg")
            ],
            [
                new("Solution Valid", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Refined TCP In Working Frame", WorkerMpValueKind.Transform, "GetTransformArg"),
                new("Uncertainties in TCP Fixture Frame", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg"),
                new("Uncertainties in Working Frame", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg"),
                new("RMS Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("MAX Abs Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Goodness Of Fit", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Robustness", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Result Notes", WorkerMpValueKind.EditText, "GetEditTextArg")
            ]);
    }

    public static Api.CalculateTcpFixtureUncertaintiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        var uncertainties = new Api.TcpFixtureUncertainties
        {
            SolutionValid = values[0].RequireValue<WorkerBooleanValue>().Value,
            RefinedTcpInWorking = TransformMapper.ToProtocol(values[1].RequireValue<WorkerTransformValue>()),
            UncertaintiesInTcpFixtureFrame = ToDoubleVector6(values[2]),
            UncertaintiesInWorkingFrame = ToDoubleVector6(values[3]),
            RmsError = values[4].RequireValue<WorkerDoubleValue>().Value,
            MaximumAbsoluteError = values[5].RequireValue<WorkerDoubleValue>().Value,
            GoodnessOfFit = values[6].RequireValue<WorkerDoubleValue>().Value,
            Robustness = values[7].RequireValue<WorkerDoubleValue>().Value
        };
        uncertainties.ResultNotes.AddRange(values[8].RequireValue<WorkerStringListValue>().Values);
        return new() { Uncertainties = uncertainties, Execution = completed.Details };
    }

    private static Api.DoubleVector6 ToDoubleVector6(WorkerMpOutputValue value)
    {
        var vector = new Api.DoubleVector6();
        vector.Values.AddRange(value.RequireValue<WorkerDoubleArrayValue>().Values);
        return vector;
    }
}
