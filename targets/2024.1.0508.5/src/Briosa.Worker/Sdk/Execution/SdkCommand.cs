using Briosa.Worker.Control;
namespace Briosa.Worker.Sdk;

internal sealed class SdkCommand
{
    public SdkCommand(string operationId)
        : this(operationId, operationId, [], [])
    {
    }

    public SdkCommand(
        string operationId,
        string stepName,
        IReadOnlyList<SdkInputArgument> inputArguments,
        IReadOnlyList<WorkerMpOutputArgument> outputArguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);
        ArgumentNullException.ThrowIfNull(inputArguments);
        ArgumentNullException.ThrowIfNull(outputArguments);
        OperationId = operationId;
        StepName = stepName;
        InputArguments = [.. inputArguments];
        OutputArguments = [.. outputArguments];
    }

    public string OperationId { get; }

    public string StepName { get; }

    public IReadOnlyList<SdkInputArgument> InputArguments { get; }

    public IReadOnlyList<WorkerMpOutputArgument> OutputArguments { get; }
}
