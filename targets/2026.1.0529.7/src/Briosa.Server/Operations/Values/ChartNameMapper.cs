using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class ChartNameMapper
{
    public static WorkerTextValue Required(Api.ChartName? value, string fieldName)
    {
        if (value is null)
        {
            throw RequestFieldViolation.Required(fieldName);
        }

        if (string.IsNullOrWhiteSpace(value.Name))
        {
            throw RequestFieldViolation.NotBlank(RequestFieldViolation.Path(fieldName, "name"));
        }

        return new(value.Name);
    }
}
