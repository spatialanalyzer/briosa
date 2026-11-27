using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

/// <summary>
/// The 0..255 check that every MP color argument applies to its channels, reporting
/// the first channel above the range by its path under <c>fieldName</c>.
/// </summary>
internal static class ColorChannelRange
{
    public static void Require(Api.Color color, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(color);
        if (color.Red > byte.MaxValue)
            throw RequestFieldViolation.Range(RequestFieldViolation.Path(fieldName, "red"));
        if (color.Green > byte.MaxValue)
            throw RequestFieldViolation.Range(RequestFieldViolation.Path(fieldName, "green"));
        if (color.Blue > byte.MaxValue)
            throw RequestFieldViolation.Range(RequestFieldViolation.Path(fieldName, "blue"));
    }
}
