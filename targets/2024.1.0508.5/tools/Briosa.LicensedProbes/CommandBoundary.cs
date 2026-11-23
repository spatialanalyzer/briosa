namespace Briosa.LicensedProbes;

/// <summary>
/// The top level of every harness command. Each command converts the failures it expects to
/// fixed messages; anything else that would reach it is printed as one fixed line with the
/// exception's type name only, because a message or a stack trace can hold a path or a value.
/// </summary>
internal static class CommandBoundary
{
    public const int UnexpectedFailureExit = 3;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "The last boundary before stderr: only the type name is printed, never the message.")]
    public static async Task<int> RunAsync(Func<Task<int>> command, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(error);
        try
        {
            return await command().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await error.WriteLineAsync(
                $"Briosa.LicensedProbes stopped on an unexpected {exception.GetType().Name}; no further detail is printed.").ConfigureAwait(false);
            return UnexpectedFailureExit;
        }
    }
}
