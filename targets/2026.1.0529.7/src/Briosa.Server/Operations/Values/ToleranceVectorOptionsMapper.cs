using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class ToleranceVectorOptionsMapper
{
    public static WorkerToleranceVectorOptionsValue Required(Api.ToleranceVectorOptions? value, string fieldName)
    {
        if (value is null)
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(value));
        }

        return new(
            FromRequest(value.HighX),
            FromRequest(value.HighY),
            FromRequest(value.HighZ),
            FromRequest(value.HighMagnitude),
            FromRequest(value.LowX),
            FromRequest(value.LowY),
            FromRequest(value.LowZ),
            FromRequest(value.LowMagnitude));
    }

    public static Api.ToleranceVectorOptions ToProtocol(WorkerToleranceVectorOptionsValue value) => new()
    {
        HighX = ToProtocol(value.HighX),
        HighY = ToProtocol(value.HighY),
        HighZ = ToProtocol(value.HighZ),
        HighMagnitude = ToProtocol(value.HighMagnitude),
        LowX = ToProtocol(value.LowX),
        LowY = ToProtocol(value.LowY),
        LowZ = ToProtocol(value.LowZ),
        LowMagnitude = ToProtocol(value.LowMagnitude)
    };

    private static Api.ToleranceLimit ToProtocol(WorkerToleranceLimit value) => new()
    {
        Enabled = value.Enabled,
        Value = value.Value
    };

    private static WorkerToleranceLimit FromRequest(Api.ToleranceLimit? value) => value is null
        ? new(false, 0)
        : new(value.HasEnabled && value.Enabled, value.HasValue ? value.Value : 0);
}
