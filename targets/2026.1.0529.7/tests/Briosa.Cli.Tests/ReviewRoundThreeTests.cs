using Briosa.Cli.Engine;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Xunit.Abstractions;

namespace Briosa.Cli.Tests;

/// <summary>Findings of the third independent review, each pinned through the real CLI.</summary>
public sealed class ReviewRoundThreeTests(ITestOutputHelper output)
{
    // A: the operations whose result has one field name nested under two or more of
    // its top-level fields, so that one bare-named row could fit several of them.
    [Fact]
    public void TwentySixOperationsHaveANestedNameUnderSeveralResultFields()
    {
        foreach (var (operation, name) in SharedNestedNames.All)
        {
            output.WriteLine($"{operation.OperationId} [{operation.Tier}]: {name}");
        }

        Assert.Equal(26, SharedNestedNames.All.Count);
        Assert.Equal(15, SharedNestedNames.All.Count(entry => entry.Operation.Tier == BridgeTier.ReadOnly));
    }

    // B: the caller denials the real policy makes with this CLI's restriction are
    // among those the oracle derives from the denial factory, and name what the CLI
    // sent: its profile, or the flag it denies.
    [Fact]
    public void EveryCallerDenialThePolicyMakesNamesTheRestrictionSent()
    {
        // The rule, which fields are present, and the caller values; an option field's
        // name is the operation's own and is data.
        static string Shape(PolicyDenialDetails details) =>
            $"{details.Rule} {details.HasDeniedFlag} {details.HasAdmissionProfile} {details.HasRequestOptionField} " +
            $"{(details.HasCallerAdmissionProfile ? details.CallerAdmissionProfile : "-")} " +
            $"{(details.HasCallerDeniedFlag ? details.CallerDeniedFlag : "-")}";
        var factory = GenuineResponses.PolicyDenials.Select(denial => Shape(GenuineResponses.Details(denial))).ToHashSet();
        var policy = GenuineResponses.PolicyCallerDenials.Select(denial => GenuineResponses.Details(denial)).ToArray();
        foreach (var shape in policy.Select(Shape).Distinct())
        {
            output.WriteLine(shape);
        }

        output.WriteLine($"caller denials from the real policy: {policy.Length}");
        Assert.Contains(policy, details => details.Rule == PolicyDenialRule.CallerFlag);
        Assert.Contains(policy, details => details.Rule == PolicyDenialRule.CallerProfile);
        Assert.All(policy, details => Assert.Contains(Shape(details), factory));
        Assert.All(policy, details => Assert.True(details.Rule == PolicyDenialRule.CallerFlag
            ? details.CallerDeniedFlag == "interactive_ui"
            : details.CallerAdmissionProfile == "read-only", details.ToString()));
    }

    public static TheoryData<string> SharedNameOperations() =>
        [.. SharedNestedNames.All.Select(entry => entry.Operation.OperationId)];

    // A: one row never answers for two result fields, whatever its name fits.
    [Theory]
    [MemberData(nameof(SharedNameOperations))]
    public void OneRowNeverAnswersForSeveralResultFields(string operationId)
    {
        var (operation, name) = SharedNestedNames.All.Single(entry => entry.Operation.OperationId == operationId);
        var result = SharedNestedNames.OneRowForSeveralFields(operation, name);

        var envelope = GenuineResponses.Classify(operation, new GenuineReply(result, null));

        Assert.Equal(BridgeOutcome.CompletionEvidenceUnavailable, envelope.Outcome);
        Assert.Equal(Codes.ResultOutputMissing, envelope.Failure!["diagnostic_code"]!.GetValue<string>());
    }

    // A: the reviewer's case, through the real CLI.
    [Fact]
    public async Task OneBareRowForThreeDatumsIsNotASuccess()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.Ok(new GetFeatureCheckDatumReferencesResult
        {
            Execution = new MpExecutionDetails
            {
                State = MpExecutionState.Succeeded,
                MpResultCode = 2,
                OutputRetrievals = { new OutputRetrievalDetails { FieldName = "collection_name", State = OutputRetrievalState.Retrieved } }
            }
        });

        var run = await CliRun.RunAsync(["call", "gdt_operations.get_feature_check_datum_references", "--json", "{}",
            "--endpoint", server.Endpoint]);

        EvidenceConsistencyTests.AssertConflict(run, Codes.ResultOutputMissing);
    }

    private static readonly Dictionary<string, Func<PolicyDenialDetails, PolicyDenialDetails>> DenialConflicts = new()
    {
        ["a server denial relabelled as the caller's flag"] = genuine =>
        {
            var details = genuine.Clone();
            details.Rule = PolicyDenialRule.CallerFlag;
            return details;
        },
        ["a caller profile other than the one sent"] = _ =>
            new() { Rule = PolicyDenialRule.CallerProfile, CallerAdmissionProfile = "full" },
        ["a caller flag the CLI did not deny"] = _ =>
            new() { Rule = PolicyDenialRule.CallerFlag, CallerDeniedFlag = "physical_motion" },
        ["a caller flag with a server flag"] = _ =>
            new() { Rule = PolicyDenialRule.CallerFlag, CallerDeniedFlag = "interactive_ui", DeniedFlag = "interactive_ui" },
        ["a caller profile denial without its profile"] = _ => new() { Rule = PolicyDenialRule.CallerProfile },
        ["a blank caller profile"] = _ => new() { Rule = PolicyDenialRule.CallerProfile, CallerAdmissionProfile = "" },
        ["a profile denial without its profile"] = _ => new() { Rule = PolicyDenialRule.Profile },
        ["a profile denial with the caller's profile"] = _ =>
            new() { Rule = PolicyDenialRule.Profile, AdmissionProfile = "read-only", CallerAdmissionProfile = "read-only" },
        ["a flag denial without its flag"] = _ => new() { Rule = PolicyDenialRule.Flag },
        ["a flag denial with a profile"] = _ =>
            new() { Rule = PolicyDenialRule.Flag, DeniedFlag = "physical_motion", AdmissionProfile = "standard" },
        ["an override with an option field"] = _ =>
            new() { Rule = PolicyDenialRule.Override, RequestOptionField = "show_results_dialog" },
        ["an unreviewed row with a flag"] = _ => new() { Rule = PolicyDenialRule.Unreviewed, DeniedFlag = "physical_motion" },
        ["an exclusive workflow with a profile"] = _ => new() { Rule = PolicyDenialRule.Exclusive, AdmissionProfile = "full" }
    };

    public static TheoryData<string> DenialConflictLabels() => [.. DenialConflicts.Keys];

    // B: a denial whose detail fields do not fit its rule, or whose caller detail is
    // not the restriction this CLI sent, is conflicting evidence.
    [Theory]
    [MemberData(nameof(DenialConflictLabels))]
    public async Task ADenialThatDoesNotFitItsRuleIsConflictingEvidence(string label)
    {
        var (status, error) = GenuineDenial();
        error.PolicyDenial = DenialConflicts[label](error.PolicyDenial);
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.Error(status, error);

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server));

        EvidenceConsistencyTests.AssertConflict(run, Codes.ErrorInconsistent);
        Assert.DoesNotContain("caller-restriction-denied", CallOutcomeTests.Warnings(run));
    }

    public static TheoryData<string, bool> DenialsThatFit() => new()
    {
        { "caller-flag", true },
        { "caller-profile", true },
        { "override", false },
        { "exclusive", false },
        { "flag", false },
        { "profile", false },
        { "unreviewed", false }
    };

    // B: every rule with its own fields, and caller detail that names what this CLI
    // sent, is a denial.
    [Theory]
    [MemberData(nameof(DenialsThatFit))]
    public async Task ADenialThatFitsItsRuleIsNotStarted(string label, bool callers)
    {
        var (status, error) = GenuineDenial();
        error.PolicyDenial = label switch
        {
            "caller-flag" => new() { Rule = PolicyDenialRule.CallerFlag, CallerDeniedFlag = "interactive_ui", RequestOptionField = "show_results_dialog" },
            "caller-profile" => new() { Rule = PolicyDenialRule.CallerProfile, CallerAdmissionProfile = "read-only" },
            "override" => new() { Rule = PolicyDenialRule.Override },
            "exclusive" => new() { Rule = PolicyDenialRule.Exclusive, RequestOptionField = "keep_running" },
            // A newer server can name a flag this CLI does not know; it is data.
            "flag" => new() { Rule = PolicyDenialRule.Flag, DeniedFlag = "a_newer_flag" },
            "profile" => new() { Rule = PolicyDenialRule.Profile, AdmissionProfile = "standard" },
            _ => new() { Rule = PolicyDenialRule.Unreviewed }
        };
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.Error(status, error);

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server));

        Assert.Equal(6, run.ExitCode);
        Assert.Equal(callers, CallOutcomeTests.Warnings(run).Contains("caller-restriction-denied"));
    }

    // D: no operation call serves reports MP code 4 as a failure, so such a reply to
    // a served read is something the server never sends.
    [Fact]
    public async Task AnMpFailureWithCodeFourIsConflictingEvidenceForAServedRead()
    {
        var genuine = ServerMapperShapes.All.Single(candidate => candidate.Label == "mp-failure").Exception;
        var error = OperationError.Parser.ParseFrom(genuine.Trailers.Single().ValueBytes);
        error.MpExecution.MpResultCode = 4;
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.Error(genuine.StatusCode, error);

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server));

        EvidenceConsistencyTests.AssertConflict(run, Codes.ErrorInconsistent);
    }

    private static (StatusCode Status, OperationError Error) GenuineDenial()
    {
        var genuine = ServerMapperShapes.All.Single(candidate => candidate.Label == "policy-denied").Exception;
        return (genuine.StatusCode, OperationError.Parser.ParseFrom(genuine.Trailers.Single().ValueBytes));
    }
}

/// <summary>
/// The registered operations whose result has a field name nested under two or more
/// of its top-level fields, found from the result descriptors alone.
/// </summary>
internal static class SharedNestedNames
{
    public static IReadOnlyList<(GenuineOperation Operation, string Name)> All { get; } = Find();

    /// <summary>
    /// An OK result whose rows name every top-level field, but with one bare-named
    /// row standing in for every field that has the shared name inside it.
    /// </summary>
    public static IMessage OneRowForSeveralFields(GenuineOperation operation, string name)
    {
        var result = (IMessage)Activator.CreateInstance(operation.Result.ClrType)!;
        var details = new MpExecutionDetails { State = MpExecutionState.Succeeded, MpResultCode = 2 };
        details.OutputRetrievals.Add(new OutputRetrievalDetails { FieldName = name, State = OutputRetrievalState.Retrieved });
        foreach (var field in OutputFields(operation.Result).Where(field => !Below(field).Contains(name)))
        {
            var own = operation.Contracts.Select(contract => contract.FieldName).First(candidate =>
                candidate == field.Name || candidate.StartsWith(field.Name + ".", StringComparison.Ordinal) ||
                !candidate.Contains('.', StringComparison.Ordinal) && Below(field).Contains(candidate));
            details.OutputRetrievals.Add(new OutputRetrievalDetails { FieldName = own, State = OutputRetrievalState.Retrieved });
        }

        ProtocolCatalog.FindExecutionField(operation.Result)!.Accessor.SetValue(result, details);
        return result;
    }

    private static List<(GenuineOperation, string)> Find()
    {
        var found = new List<(GenuineOperation, string)>();
        foreach (var operation in GenuineResponses.Operations)
        {
            var below = OutputFields(operation.Result).Select(Below).ToArray();
            var shared = below.SelectMany(names => names).Distinct(StringComparer.Ordinal)
                .Where(name => below.Count(names => names.Contains(name)) >= 2)
                .Order(StringComparer.Ordinal).FirstOrDefault();
            if (shared is not null)
            {
                found.Add((operation, shared));
            }
        }

        return found;
    }

    private static IEnumerable<FieldDescriptor> OutputFields(MessageDescriptor result) =>
        result.Fields.InFieldNumberOrder().Where(field => field.FieldNumber != ProtocolCatalog.ExecutionFieldNumber);

    // Every field name nested anywhere inside a top-level field.
    private static HashSet<string> Below(FieldDescriptor field)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        void Walk(MessageDescriptor message)
        {
            if (!visited.Add(message.FullName))
            {
                return;
            }

            foreach (var nested in message.Fields.InFieldNumberOrder())
            {
                names.Add(nested.Name);
                if (nested.FieldType == FieldType.Message)
                {
                    Walk(nested.MessageType);
                }
            }
        }

        if (field.FieldType == FieldType.Message)
        {
            Walk(field.MessageType);
        }

        return names;
    }
}
