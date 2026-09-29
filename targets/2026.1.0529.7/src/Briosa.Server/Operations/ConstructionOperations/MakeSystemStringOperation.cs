using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeSystemStringOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_system_string", "Make a System String", "briosa.ConstructionOperations",
        "MakeSystemString", "/briosa.ConstructionOperations/MakeSystemString", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_string", "Resultant String", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.MakeSystemStringRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.HasStringContent || request.StringContent == Api.SystemString.Unspecified ||
            !Enum.IsDefined(request.StringContent))
            throw new ArgumentException("Request field 'string_content' is required.", nameof(request));
        var inputs = new List<WorkerMpInputArgument>
        {
            new("String Content", WorkerMpValueKind.SystemString,
                new WorkerChoiceValue<WorkerSystemStringValue>((WorkerSystemStringValue)((int)request.StringContent - 1)),
                "SetSystemStringArg")
        };
        if (request.HasFormatString)
            inputs.Add(new("Format String (Optional)", WorkerMpValueKind.Text,
                new WorkerTextValue(request.FormatString), "SetStringArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs,
            [new("Resultant String", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.MakeSystemStringResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            ResultantString = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
            Execution = completed.Details
        };
}
