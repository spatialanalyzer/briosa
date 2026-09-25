using Google.Protobuf;

namespace Briosa.Desktop;

public static class SafeText
{
    public static string Label(Enum? value)
    {
        if (value is null || !Enum.IsDefined(value.GetType(), value)) return "Unavailable";
        var text = value.ToString();
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            if (i > 0 && char.IsUpper(text[i]) && char.IsLower(text[i - 1])) result.Append(' ');
            result.Append(i == 0 ? text[i] : char.ToLowerInvariant(text[i]));
        }
        return result.ToString();
    }
    public static string Code(string? value) => value is { Length: > 0 and <= 128 } &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_') ? value : "unavailable";
    public static string Version(string? value) => value is { Length: > 0 and <= 96 } &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '+') ? value : "Unavailable";
}
