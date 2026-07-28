using System.Text.Json;
using Google.Protobuf.Reflection;

namespace Briosa.Protocol.Tests;

public sealed class ClientConformanceFixtureTests
{
    private static readonly string[] ExpectedLiveScenarios =
    [
        "cancellation",
        "deadline",
        "mp-failure",
        "output-failure",
        "policy-denied",
        "ready",
        "unavailable",
        "unsupported-version",
        "watchdog-recovery"
    ];

    [Fact]
    public void LiveFixtureDefinesTheCompletePackagedHostMatrix()
    {
        using var document = ReadFixture("live-scenarios.json");
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("schema_version").GetInt32());
        Assert.Equal(
            "briosa.client.live.v1",
            root.GetProperty("fixture_set_id").GetString());
        Assert.Equal(
            "file_operations.get_working_directory",
            root.GetProperty("operation_id").GetString());
        Assert.Equal(
            "briosa-operation-error-bin",
            root.GetProperty("error_trailer").GetString());

        var scenarios = root.GetProperty("scenarios").EnumerateArray().ToArray();
        Assert.Equal(
            ExpectedLiveScenarios,
            scenarios.Select(scenario => scenario.GetProperty("id").GetString())
                .Order(StringComparer.Ordinal));
        Assert.Equal(
            scenarios.Length,
            scenarios.Select(scenario => scenario.GetProperty("id").GetString())
                .Distinct(StringComparer.Ordinal)
                .Count());

        var knownFailureKinds = EnumNames(
            Briosa.Core.V1Alpha1.OperationError.Descriptor
                .FindFieldByNumber(2).EnumType);
        foreach (var scenario in scenarios)
        {
            var expected = scenario.GetProperty("expected");
            Assert.Contains(
                GetRequiredString(expected, "grpc_status"),
                CanonicalGrpcStatuses);
            Assert.All(
                expected.GetProperty("failure_kinds").EnumerateArray(),
                value => Assert.Contains(value.GetString()!, knownFailureKinds));
        }
    }

    [Fact]
    public void TypedErrorCasesUseCurrentEnumsAndNeverAuthorizeAutomaticReplay()
    {
        using var document = ReadFixture("operation-error-cases.json");
        var root = document.RootElement;
        var cases = root.GetProperty("cases").EnumerateArray().ToArray();

        Assert.Equal(1, root.GetProperty("schema_version").GetInt32());
        Assert.Equal(
            "briosa.client.operation-errors.v1",
            root.GetProperty("fixture_set_id").GetString());
        Assert.True(cases.Length >= 7);
        Assert.Equal(
            cases.Length,
            cases.Select(item => item.GetProperty("id").GetString())
                .Distinct(StringComparer.Ordinal)
                .Count());

        var errorDescriptor = Briosa.Core.V1Alpha1.OperationError.Descriptor;
        var knownFailureKinds = EnumNames(errorDescriptor.FindFieldByNumber(2).EnumType);
        var knownDispositions = EnumNames(errorDescriptor.FindFieldByNumber(7).EnumType);
        var knownRecovery = EnumNames(errorDescriptor.FindFieldByNumber(8).EnumType);
        var knownReplay = EnumNames(errorDescriptor.FindFieldByNumber(9).EnumType);
        var knownSafety = EnumNames(errorDescriptor.FindFieldByNumber(10).EnumType);

        foreach (var item in cases)
        {
            Assert.Contains(
                GetRequiredString(item, "grpc_status"),
                CanonicalGrpcStatuses);
            var error = item.GetProperty("operation_error");
            Assert.Contains(GetRequiredString(error, "kind"), knownFailureKinds);
            Assert.Contains(
                GetRequiredString(error, "execution_disposition"),
                knownDispositions);
            Assert.Contains(
                GetRequiredString(error, "recovery_guidance"),
                knownRecovery);
            Assert.Contains(
                GetRequiredString(error, "replay_guidance"),
                knownReplay);
            Assert.Contains(GetRequiredString(error, "replay_safety"), knownSafety);

            var behavior = item.GetProperty("client_behavior");
            Assert.False(behavior.GetProperty("automatic_replay").GetBoolean());
            if (error.GetProperty("execution_disposition").GetString() ==
                    "EXECUTION_DISPOSITION_STARTED_OUTCOME_UNKNOWN" &&
                error.GetProperty("replay_safety").GetString() is
                    "REPLAY_SAFETY_UNSAFE" or "REPLAY_SAFETY_UNKNOWN")
            {
                Assert.Equal(
                    "REPLAY_GUIDANCE_RECONCILE_BEFORE_REPLAY",
                    error.GetProperty("replay_guidance").GetString());
                Assert.True(
                    behavior.GetProperty("reconciliation_required").GetBoolean());
            }
        }
    }

    [Fact]
    public void FixturesContainNoOperationalValues()
    {
        var fixtureRoot = FindFixtureRoot();
        var text = string.Join(
            '\n',
            Directory.EnumerateFiles(fixtureRoot, "*.json", SearchOption.AllDirectories)
                .Where(path => path.Contains(
                    $"{Path.DirectorySeparatorChar}v1{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal))
                .Order(StringComparer.Ordinal)
                .Select(File.ReadAllText));

        Assert.DoesNotMatch("[A-Za-z]:\\\\", text);
        Assert.DoesNotContain("credential", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("license_data", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("geometry", text, StringComparison.OrdinalIgnoreCase);
    }

    private static readonly string[] CanonicalGrpcStatuses =
    [
        "CANCELLED",
        "DATA_LOSS",
        "DEADLINE_EXCEEDED",
        "FAILED_PRECONDITION",
        "OK",
        "PERMISSION_DENIED",
        "UNAVAILABLE",
        "UNIMPLEMENTED"
    ];

    private static HashSet<string> EnumNames(EnumDescriptor descriptor) =>
        descriptor.Values.Select(value => value.Name).ToHashSet(StringComparer.Ordinal);

    private static string GetRequiredString(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetString()
        ?? throw new InvalidDataException($"'{propertyName}' must be a string.");

    private static JsonDocument ReadFixture(string fileName) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(FindFixtureRoot(), "v1", fileName)));

    private static string FindFixtureRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
            !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(
            directory?.FullName ?? throw new DirectoryNotFoundException(
                "Could not locate the Briosa repository root."),
            "conformance");
    }
}
