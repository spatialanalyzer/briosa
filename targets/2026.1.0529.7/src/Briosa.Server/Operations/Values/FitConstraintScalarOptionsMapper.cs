using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class FitConstraintScalarOptionsMapper
{
    public static WorkerFitConstraintScalarOptionsValue ToWorker(Api.FitConstraintScalarOptions? value) =>
        new(ToWorker(value?.High), ToWorker(value?.Low));

    public static Api.FitConstraintScalarOptions ToProtocol(WorkerFitConstraintScalarOptionsValue value) => new()
    {
        High = ToProtocol(value.High),
        Low = ToProtocol(value.Low)
    };

    // Each omitted leaf keeps the reviewed disabled/zero default independently.
    private static WorkerToleranceLimit ToWorker(Api.ScalarToleranceLimit? value) =>
        new(value?.Enabled ?? false, value?.Value ?? 0);

    private static Api.ScalarToleranceLimit ToProtocol(WorkerToleranceLimit value) => new()
    {
        Enabled = value.Enabled,
        Value = value.Value
    };
}
