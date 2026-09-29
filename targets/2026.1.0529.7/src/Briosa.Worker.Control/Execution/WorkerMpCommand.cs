using System.Collections.Immutable;

namespace Briosa.Worker.Control;

public sealed record WorkerMpCommand
{
    private readonly ImmutableArray<WorkerMpInputArgument> _inputArguments;
    private readonly ImmutableArray<WorkerMpOutputArgument> _outputArguments;

    public WorkerMpCommand(
        string operationId,
        string stepName,
        IReadOnlyList<WorkerMpInputArgument> inputArguments,
        IReadOnlyList<WorkerMpOutputArgument> outputArguments)
    {
        OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
        StepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
        ArgumentNullException.ThrowIfNull(inputArguments);
        ArgumentNullException.ThrowIfNull(outputArguments);
        _inputArguments = [.. inputArguments];
        _outputArguments = [.. outputArguments];
    }

    public string OperationId { get; }

    public string StepName { get; }

    public IReadOnlyList<WorkerMpInputArgument> InputArguments => _inputArguments;

    public IReadOnlyList<WorkerMpOutputArgument> OutputArguments => _outputArguments;
}
