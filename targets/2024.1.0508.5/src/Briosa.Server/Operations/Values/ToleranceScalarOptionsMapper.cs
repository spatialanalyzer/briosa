using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class ToleranceScalarOptionsMapper
{
    public static WorkerToleranceScalarOptionsValue ToWorker(Api.ToleranceScalarOptions? value) =>
        new(ToWorker(value?.High), ToWorker(value?.Low));

    public static Api.ToleranceScalarOptions ToProtocol(WorkerToleranceScalarOptionsValue value) => new()
    {
        High = ToProtocol(value.High),
        Low = ToProtocol(value.Low)
    };

    private static WorkerToleranceLimit ToWorker(Api.ScalarToleranceLimit? value) =>
        new(value?.HasEnabled == true && value.Enabled, value?.HasValue == true ? value.Value : 0);

    private static Api.ScalarToleranceLimit ToProtocol(WorkerToleranceLimit value) => new()
    {
        Enabled = value.Enabled,
        Value = value.Value
    };
}
