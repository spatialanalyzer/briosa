using Briosa.Worker.Control;

namespace Briosa.Server.Operations.Values;

internal static class StringListMapper
{
    public static WorkerStringListValue RequiredList(IReadOnlyList<string> values, string fieldName)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(values));
        }

        return new(values);
    }
}
