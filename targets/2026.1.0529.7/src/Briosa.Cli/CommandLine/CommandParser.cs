namespace Briosa.Cli.CommandLine;

/// <summary>A command line that matched one command's grammar.</summary>
internal sealed class ParsedCommand
{
    public ParsedCommand(string name) => Name = name;

    public string Name { get; }

    public List<string> Arguments { get; } = [];

    public Dictionary<string, List<string>> Options { get; } = new(StringComparer.Ordinal);

    public HashSet<string> Switches { get; } = new(StringComparer.Ordinal);

    public string? Option(string name) => Options.TryGetValue(name, out var values) ? values[0] : null;

    public IReadOnlyList<string> Values(string name) => Options.TryGetValue(name, out var values) ? values : [];
}

/// <summary>
/// A small strict parser. Options are written <c>--name value</c>; the next argument
/// is always the value, even when it starts with <c>--</c>. There is no
/// <c>--name=value</c> form, no abbreviation, and no option is accepted twice unless
/// it is documented as repeatable. A rejection names a code, never the argument.
/// </summary>
internal static class CommandParser
{
    public const string EndpointOption = "--endpoint";

    private static readonly IReadOnlyDictionary<string, CommandGrammar> Grammars =
        new Dictionary<string, CommandGrammar>(StringComparer.Ordinal)
        {
            ["help"] = new(0, [], [], []),
            ["version"] = new(0, [], [], []),
            ["status"] = new(0, [EndpointOption], [], []),
            ["ops categories"] = new(0, [EndpointOption], [], []),
            ["ops search"] = new(0,
                [EndpointOption, "--query", "--category", "--effect", "--tier", "--offset", "--limit"],
                ["--risk-flag"],
                ["--include-denied", "--validated-only"]),
            ["ops describe"] = new(1, [EndpointOption], [], []),
            ["call"] = new(1,
                [EndpointOption, "--json", "--input", "--output", "--max-inline-bytes", "--deadline-seconds"],
                [],
                [])
        };

    public static IReadOnlyCollection<string> Commands => Grammars.Keys.ToArray();

    public static ParsedCommand? Parse(IReadOnlyList<string> arguments, out string? errorCode) =>
        Parse(arguments, out errorCode, out _);

    /// <summary>
    /// Parses the command line. <paramref name="recognized"/> names the command even when
    /// its options do not parse, so a refusal can still be reported for that command.
    /// </summary>
    public static ParsedCommand? Parse(IReadOnlyList<string> arguments, out string? errorCode, out string? recognized)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        errorCode = null;
        recognized = null;
        if (arguments.Count == 0 || arguments[0] is "--help" or "-h")
        {
            return arguments.Count <= 1 ? new ParsedCommand("help") : Fail("usage-unexpected-argument", out errorCode);
        }

        var consumed = 1;
        var name = arguments[0];
        if (string.Equals(name, "ops", StringComparison.Ordinal))
        {
            if (arguments.Count < 2)
            {
                return Fail("usage-missing-command", out errorCode);
            }

            name = "ops " + arguments[1];
            consumed = 2;
        }

        if (!Grammars.TryGetValue(name, out var grammar))
        {
            return Fail("usage-unknown-command", out errorCode);
        }

        recognized = name;
        var command = new ParsedCommand(name);
        for (var index = consumed; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                if (command.Arguments.Count >= grammar.Arguments)
                {
                    return Fail("usage-unexpected-argument", out errorCode);
                }

                command.Arguments.Add(argument);
                continue;
            }

            if (grammar.Switches.Contains(argument))
            {
                if (!command.Switches.Add(argument))
                {
                    return Fail("usage-duplicate-option", out errorCode);
                }

                continue;
            }

            var repeatable = grammar.Repeatable.Contains(argument);
            if (!repeatable && !grammar.Options.Contains(argument))
            {
                return Fail("usage-unknown-option", out errorCode);
            }

            if (index + 1 >= arguments.Count)
            {
                return Fail("usage-missing-value", out errorCode);
            }

            if (command.Options.TryGetValue(argument, out var values))
            {
                if (!repeatable)
                {
                    return Fail("usage-duplicate-option", out errorCode);
                }

                values.Add(arguments[++index]);
            }
            else
            {
                command.Options[argument] = [arguments[++index]];
            }
        }

        return command.Arguments.Count < grammar.Arguments ? Fail("usage-missing-argument", out errorCode) : command;
    }

    private static ParsedCommand? Fail(string code, out string? errorCode)
    {
        errorCode = code;
        return null;
    }

    private sealed record CommandGrammar(
        int Arguments,
        IReadOnlySet<string> Options,
        IReadOnlySet<string> Repeatable,
        IReadOnlySet<string> Switches)
    {
        public CommandGrammar(int arguments, string[] options, string[] repeatable, string[] switches)
            : this(arguments, options.ToHashSet(StringComparer.Ordinal), repeatable.ToHashSet(StringComparer.Ordinal),
                switches.ToHashSet(StringComparer.Ordinal))
        {
        }
    }
}
