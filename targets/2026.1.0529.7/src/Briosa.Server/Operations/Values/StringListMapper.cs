using Briosa.Worker.Control;

namespace Briosa.Server.Operations.Values;

internal static class StringListMapper
{
    public static WorkerStringListValue RequiredList(IReadOnlyList<string> values, string fieldName)
    {
        if (values.Count == 0)
        {
            throw RequestFieldViolation.NotEmpty(fieldName);
        }

        return new(values);
    }
}
