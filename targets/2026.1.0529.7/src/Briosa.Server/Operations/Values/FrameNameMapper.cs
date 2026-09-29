using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class FrameNameMapper
{
    public static WorkerTextValue Required(Api.FrameName? value, string fieldName)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.Name))
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(value));
        }

        return new(value.Name);
    }
}
