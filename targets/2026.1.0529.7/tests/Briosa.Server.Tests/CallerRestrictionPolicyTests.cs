using Briosa.Server.Operations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

/// <summary>
/// The caller restriction inside the one evaluator (#443). The exhaustive tests make
/// "narrowing only" a checked property over the whole registry and every reviewed
/// request option, rather than a claim.
/// </summary>
public sealed class CallerRestrictionPolicyTests(ITestOutputHelper output)
{
    private const string QueryPointsToObjects = "analysis_operations.query_points_to_objects";
    private const string SetPointOfViewFromInstrumentUpdates = "view_control.set_point_of_view_from_instrument_updates";
    private const string GetWorkingDirectory = "file_operations.get_working_directory";
    private const string StartInstrumentInterface = "instrument_operations.start_instrument_interface";

    /// <summary>
    /// For every server profile combined with each representative flag and override
    /// setting, every registered operation (its reviewed row, its request with every
    /// option off, with each option alone on, with each option omitted, with every
    /// option on, and without a request), and every caller restriction (each profile or
    /// none, with no flag, each single flag, or every flag denied):
    /// admitted with the restriction exactly when admitted without it and admitted by a
    /// server configured with only the caller's limits. A server denial is unchanged,
    /// an admission the restriction keeps is unchanged, and a caller denial names the
    /// step the evaluator takes for the caller's limits alone.
    /// </summary>
    [Fact]
    public void ARestrictionAdmitsExactlyWhatBothTheServerAndTheCallersOwnLimitsAdmit()
    {
        var cases = Cases();
        var restrictions = Restrictions();
        var servers = Servers();
        var alone = restrictions
            .Select(restriction => CallerAlone(restriction))
            .Select(policy => cases.Select(candidate => candidate.Decide(policy, null)).ToArray())
            .ToArray();

        var failures = new List<string>();
        var tally = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var (name, server) in servers)
        {
            for (var index = 0; index < cases.Count; index++)
            {
                var without = cases[index].Decide(server, null);
                for (var restriction = 0; restriction < restrictions.Count; restriction++)
                {
                    var with = cases[index].Decide(server, restrictions[restriction]);
                    var (category, problem) = Check(without, with, alone[restriction][index], restrictions[restriction]);
                    tally[category] = tally.GetValueOrDefault(category) + 1;
                    if (problem is not null && failures.Count < 25)
                    {
                        failures.Add($"{name} | {cases[index].Label} | {restrictions[restriction].Format()}: {problem}");
                    }
                }
            }
        }

        Assert.Empty(failures);

        // The shape is pinned: four server profiles times seven settings, and five
        // caller profile choices times thirteen flag choices, less the empty restriction.
        Assert.Equal(28, servers.Length);
        Assert.Equal(64, restrictions.Count);
        AssertCoverage(cases);
        var evaluated = (long)servers.Length * cases.Count * restrictions.Count;
        Assert.Equal(evaluated, tally.Values.Sum());

        // Every outcome occurs, so no branch of the property holds vacuously.
        foreach (var category in new[]
        {
            "server-denied", "admitted", "caller-flag", "caller-flag-option", "caller-profile", "caller-profile-option"
        })
        {
            Assert.True(tally.GetValueOrDefault(category) > 0, category);
        }

        output.WriteLine(
            $"operations {SpatialAnalyzerApi.Operations.Count}, options {OperationConditionalOptions.Entries.Count}, " +
            $"cases {cases.Count}, servers {servers.Length}, restrictions {restrictions.Count}, evaluated {evaluated}");
        foreach (var (category, count) in tally.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            output.WriteLine($"{category} {count}");
        }
    }

    /// <summary>
    /// Without a restriction, every decision over the same servers and cases is the
    /// one the released precedence (Server 0.10.1, steps 1 to 7) makes, transcribed
    /// independently below: same kind, diagnostic code, rule, duration class, and
    /// denial, and no caller restriction recorded.
    /// </summary>
    [Fact]
    public void WithoutARestrictionEveryDecisionIsTheReleasedPrecedence()
    {
        var cases = Cases();
        var failures = new List<string>();
        var compared = 0;
        foreach (var (profile, settings) in ServerConfigurations())
        {
            var policy = OperationPolicyTests.CreatePolicy(profile: profile, settings: settings);
            var configuration = new ReleasedConfiguration(profile, settings);
            var operations = policy.Operations.ToDictionary(operation => operation.OperationId, StringComparer.Ordinal);
            foreach (var candidate in cases)
            {
                var actual = candidate.Decide(policy, null);
                var expected = Released(configuration, operations[candidate.OperationId], candidate.Classification());
                if (actual.Kind != expected.Kind || actual.DiagnosticCode != expected.Code ||
                    actual.PolicyRule != expected.Rule || actual.DurationClass != expected.Duration ||
                    !Equals(actual.Denial, expected.Denial) || actual.CallerRestriction is not null ||
                    actual.CallerRestrictionOutcome != OperationCallerRestrictionOutcome.Absent)
                {
                    if (failures.Count < 25)
                    {
                        failures.Add($"{profile} {string.Join(',', settings.Keys.Take(3))} | {candidate.Label}: " +
                            $"{actual.Kind}/{actual.DiagnosticCode}/{actual.PolicyRule} != " +
                            $"{expected.Kind}/{expected.Code}/{expected.Rule}");
                    }
                }

                compared++;
            }
        }

        Assert.Empty(failures);
        Assert.Equal(28 * cases.Count, compared);
    }

    [Fact]
    public void NeitherAFlagSetToAllowNorAnAllowOverrideWidensTheCaller()
    {
        var request = OperationConditionalOptionTests.RequestPopulator.Create<Api.QueryPointsToObjectsRequest>();
        request.ShowResultsDialog = true;
        foreach (var server in new[]
        {
            OperationPolicyTests.CreatePolicy(settings: new() { ["Flags:interactive_ui"] = "allow" }),
            OperationPolicyTests.CreatePolicy(settings: new()
            {
                ["Overrides:analysis_operations:query_points_to_objects"] = "allow"
            }),
            OperationPolicyTests.CreatePolicy(profile: "full", settings: new()
            {
                ["Flags:interactive_ui"] = "allow",
                ["Overrides:analysis_operations:query_points_to_objects"] = "allow"
            })
        })
        {
            var admitted = server.EvaluateRequest(QueryPointsToObjects, request);
            Assert.Equal(OperationPolicyDecisionKind.Allowed, admitted.Kind);
            Assert.Equal(OperationDurationClass.Interactive, admitted.DurationClass);

            var flag = server.EvaluateRequest(QueryPointsToObjects, request, Parse("profile=read-only; deny=interactive_ui"));
            Assert.Equal(OperationPolicyDecisionKind.Denied, flag.Kind);
            Assert.Equal("caller-restriction-option-denied", flag.DiagnosticCode);
            Assert.Equal("option.show_results_dialog/caller.flag.interactive_ui", flag.PolicyRule);
            Assert.Equal(new Api.PolicyDenialDetails
            {
                Rule = Api.PolicyDenialRule.CallerFlag,
                CallerDeniedFlag = "interactive_ui",
                RequestOptionField = "show_results_dialog"
            }, flag.Denial!.ToDetails());
            Assert.Equal(OperationCallerRestrictionOutcome.Denied, flag.CallerRestrictionOutcome);

            var profile = server.EvaluateRequest(QueryPointsToObjects, request, Parse("profile=read-only"));
            Assert.Equal("caller-restriction-option-denied", profile.DiagnosticCode);
            Assert.Equal("option.show_results_dialog/caller.profile.read-only", profile.PolicyRule);
            Assert.Equal(new Api.PolicyDenialDetails
            {
                Rule = Api.PolicyDenialRule.CallerProfile,
                CallerAdmissionProfile = "read-only",
                RequestOptionField = "show_results_dialog"
            }, profile.Denial!.ToDetails());

            // The same request without the dialog is admitted with the server's own rule.
            request.ShowResultsDialog = false;
            var quiet = server.EvaluateRequest(QueryPointsToObjects, request, Parse("profile=read-only; deny=interactive_ui"));
            var quietWithout = server.EvaluateRequest(QueryPointsToObjects, request);
            Assert.Equal(OperationPolicyDecisionKind.Allowed, quiet.Kind);
            Assert.Equal(quietWithout.PolicyRule, quiet.PolicyRule);
            Assert.Equal(quietWithout.DurationClass, quiet.DurationClass);
            Assert.Equal(OperationCallerRestrictionOutcome.Admitted, quiet.CallerRestrictionOutcome);
            request.ShowResultsDialog = true;
        }
    }

    [Fact]
    public void AnOptionThatOpensUiWhenOmittedIsDeniedByTheCaller()
    {
        // The real omission-activated option belongs to an exclusive workflow, which the
        // server denies before a caller restriction is read. The decision is unchanged.
        var omitted = OperationConditionalOptionTests.RequestPopulator.Create<Api.SetPointOfViewFromInstrumentUpdatesRequest>();
        omitted.ClearDisplayViewControl();
        var server = OperationPolicyTests.CreatePolicy(profile: "full", settings: new()
        {
            ["Flags:interactive_ui"] = "allow",
            ["Overrides:view_control:set_point_of_view_from_instrument_updates"] = "allow"
        });
        var without = server.EvaluateRequest(SetPointOfViewFromInstrumentUpdates, omitted);
        var with = server.EvaluateRequest(SetPointOfViewFromInstrumentUpdates, omitted,
            Parse("profile=read-only; deny=interactive_ui"));
        Assert.Equal("operation-isolation-unsupported", without.DiagnosticCode);
        Assert.Equal(Api.PolicyDenialRule.Exclusive, with.Denial!.ToRule());
        Assert.Equal(without.Denial, with.Denial);
        Assert.Equal(without.PolicyRule, with.PolicyRule);
        Assert.Equal(OperationCallerRestrictionOutcome.NotEvaluated, with.CallerRestrictionOutcome);

        // The same omission rule on an admissible operation, through a test table: the
        // omitted field turns on operator UI, and the caller denies it by name.
        var sendsTrue = new OperationConditionalOption(
            QueryPointsToObjects, "show_results_dialog", "Show Results Dialog?",
            OperationOptionEffect.InteractiveUi, OperationOptionTrigger.CallerOption,
            OperationOptionCondition.WhenTrue, OperationOptionAbsence.SendsTrue);
        var request = OperationConditionalOptionTests.RequestPopulator.Create<Api.QueryPointsToObjectsRequest>();
        request.ClearShowResultsDialog();
        var policy = OperationPolicyTests.CreatePolicy(
            settings: new() { ["Flags:interactive_ui"] = "allow" },
            requestClassifier: new OperationRequestClassifier([sendsTrue]));
        Assert.Equal(OperationPolicyDecisionKind.Allowed, policy.EvaluateRequest(QueryPointsToObjects, request).Kind);
        var denied = policy.EvaluateRequest(QueryPointsToObjects, request, Parse("deny=interactive_ui"));
        Assert.Equal("caller-restriction-option-denied", denied.DiagnosticCode);
        Assert.Equal(new Api.PolicyDenialDetails
        {
            Rule = Api.PolicyDenialRule.CallerFlag,
            CallerDeniedFlag = "interactive_ui",
            RequestOptionField = "show_results_dialog"
        }, denied.Denial!.ToDetails());
    }

    [Fact]
    public void ACallerProfileWiderThanTheServersAdmitsNothingMoreAndKeepsItsOwnLimits()
    {
        var server = OperationPolicyTests.CreatePolicy(profile: "read-only", settings: new()
        {
            ["Flags:interactive_ui"] = "allow"
        });
        var full = Parse("profile=full");
        foreach (var operation in server.Operations)
        {
            var with = server.Evaluate(operation.OperationId, full);
            Assert.True(with.Kind != OperationPolicyDecisionKind.Allowed ||
                server.Evaluate(operation.OperationId).Kind == OperationPolicyDecisionKind.Allowed, operation.OperationId);
        }

        // A read-only working directory query stays admitted under the server's rule.
        var query = server.Evaluate(GetWorkingDirectory, full);
        Assert.Equal(OperationPolicyDecisionKind.Allowed, query.Kind);
        Assert.Equal("profile.read-only", query.PolicyRule);

        // No profile admits interactive_ui, so the caller's full profile denies an
        // interactive read that the server's interactive opt-in admits.
        var interactive = server.Operations.First(operation =>
            OperationClassification.Find(operation.OperationId).Row is { Risks: OperationRisks.InteractiveUi } &&
            server.Evaluate(operation.OperationId).Kind == OperationPolicyDecisionKind.Allowed).OperationId;
        var select = server.Evaluate(interactive, full);
        Assert.Equal("caller-restriction-denied", select.DiagnosticCode);
        Assert.Equal("caller.profile.full", select.PolicyRule);
        Assert.Equal(new Api.PolicyDenialDetails { Rule = Api.PolicyDenialRule.CallerProfile, CallerAdmissionProfile = "full" },
            select.Denial!.ToDetails());
    }

    [Fact]
    public void TheReadOnlyEffectFilterAppliesToTheCallersProfile()
    {
        var server = OperationPolicyTests.CreatePolicy(profile: "standard");
        var mutation = server.Operations.First(operation =>
            operation.Effect == Api.OperationEffect.Mutating &&
            OperationClassification.Find(operation.OperationId).Row is { Risks: OperationRisks.None } &&
            server.Evaluate(operation.OperationId).Kind == OperationPolicyDecisionKind.Allowed);

        var decision = server.Evaluate(mutation.OperationId, Parse("profile=read-only"));

        Assert.Equal("caller-restriction-denied", decision.DiagnosticCode);
        Assert.Equal(new Api.PolicyDenialDetails
        {
            Rule = Api.PolicyDenialRule.CallerProfile,
            CallerAdmissionProfile = "read-only"
        }, decision.Denial!.ToDetails());
        Assert.Equal(OperationPolicyDecisionKind.Allowed, server.Evaluate(mutation.OperationId, Parse("deny=destructive")).Kind);
    }

    [Fact]
    public void ExclusiveWorkflowsAndUnreviewedOperationsStayDeniedUnderEveryRestriction()
    {
        var server = OperationPolicyTests.CreatePolicy(profile: "full", settings: new()
        {
            ["Flags:interactive_ui"] = "allow",
            ["Flags:device_session"] = "allow",
            ["Overrides:instrument_operations:start_instrument_interface"] = "allow"
        });
        var unreviewed = OperationPolicyTests.CreatePolicy(
            profile: "full",
            settings: new() { ["Overrides:file_operations:get_working_directory"] = "allow" },
            classify: candidate => candidate == GetWorkingDirectory
                ? OperationClassificationLookup.Unreviewed
                : OperationClassification.Find(candidate));

        foreach (var restriction in Restrictions())
        {
            var exclusive = server.Evaluate(StartInstrumentInterface, restriction);
            Assert.Equal("operation-isolation-unsupported", exclusive.DiagnosticCode);
            Assert.Equal(OperationPolicyDenial.Exclusive(), exclusive.Denial);
            Assert.Equal(OperationCallerRestrictionOutcome.NotEvaluated, exclusive.CallerRestrictionOutcome);

            var review = unreviewed.Evaluate(GetWorkingDirectory, restriction);
            Assert.Equal("operation-risk-unreviewed", review.DiagnosticCode);
            Assert.Equal(OperationPolicyDenial.Unreviewed(), review.Denial);
        }
    }

    [Fact]
    public void TheRestrictionIsNotPartOfThePolicyFingerprint()
    {
        var policy = OperationPolicyTests.CreatePolicy(settings: new() { ["Flags:interactive_ui"] = "allow" });
        var before = policy.Fingerprint;
        foreach (var restriction in Restrictions())
        {
            _ = policy.Evaluate(QueryPointsToObjects, restriction);
            _ = policy.EvaluateRequestOptions(QueryPointsToObjects, restriction);
        }

        Assert.Equal(before, policy.Fingerprint);
        Assert.Equal(before, OperationPolicyTests.CreatePolicy(settings: new() { ["Flags:interactive_ui"] = "allow" }).Fingerprint);
        Assert.Equal(1, OperationPolicy.SemanticsVersion);
    }

    [Fact]
    public void DiscoveryAnswersWithoutTheRestrictionAndTheOptionDecisionsCanTakeOne()
    {
        var policy = OperationPolicyTests.CreatePolicy(settings: new() { ["Flags:interactive_ui"] = "allow" });
        var restriction = Parse("profile=read-only; deny=interactive_ui");

        var option = Assert.Single(policy.EvaluateRequestOptions(QueryPointsToObjects));
        Assert.Equal(OperationPolicyDecisionKind.Allowed, option.Decision.Kind);
        var restricted = Assert.Single(policy.EvaluateRequestOptions(QueryPointsToObjects, restriction));
        Assert.Equal(OperationPolicyDenialReason.CallerFlag, restricted.Decision.Denial!.Reason);

        // Discovery itself reads no header and lists the server's own decisions.
        var listed = new ServerDiscoveryService(new NullStatus(), new BuildIdentityProvider(), policy)
            .CreateCapabilities(includeDenied: true).Operations
            .Single(operation => operation.OperationId == QueryPointsToObjects);
        Assert.Equal(Api.OperationAdmission.Admitted, Assert.Single(listed.RequestOptions.Options).Admission);
    }

    [Fact]
    public async Task TheEnforcerAppliesTheRestrictionToBothEvaluationsAndDispatchesOnlyAnAdmission()
    {
        var policy = OperationPolicyTests.CreatePolicy(settings: new() { ["Flags:interactive_ui"] = "allow" });
        var dispatcher = new MappingDispatcher();
        var executor = new PolicyEnforcingWorkerCommandExecutor(dispatcher, dispatcher, policy,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance));
        var request = OperationConditionalOptionTests.RequestPopulator.Create<Api.QueryPointsToObjectsRequest>();
        request.ShowResultsDialog = true;
        var builder = OperationConditionalOptionTests.OperationBuilder.For(QueryPointsToObjects);

        var denied = await executor.ExecuteAsync(new WorkerCommandSubmission(QueryPointsToObjects,
            () => throw new InvalidOperationException("A denied request was mapped."), Request: request,
            CallerRestriction: Parse("deny=interactive_ui")), Guid.NewGuid()).ConfigureAwait(true);
        Assert.Equal(WorkerExecutionStatus.PolicyDenied, denied.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, denied.ExecutionDisposition);
        Assert.Equal("caller-restriction-option-denied", denied.DiagnosticCode);
        Assert.Equal(OperationPolicyDenialReason.CallerFlag, denied.PolicyDenial!.Reason);
        Assert.Equal(0, dispatcher.Submissions);

        // No caller profile, and no denied flag the request has: the admission stands.
        var restriction = Parse("deny=destructive,physical_motion");
        var admitted = await executor.ExecuteAsync(new WorkerCommandSubmission(QueryPointsToObjects,
            () => builder.Create(request), Request: request, CallerRestriction: restriction), Guid.NewGuid())
            .ConfigureAwait(true);
        Assert.Equal(WorkerExecutionStatus.Overloaded, admitted.Status);
        Assert.Equal(1, dispatcher.Submissions);
        Assert.Equal(1, dispatcher.Mapped);
        Assert.Same(restriction, dispatcher.Last!.CallerRestriction);
        Assert.Equal(OperationDurationClass.Interactive, dispatcher.Last.DurationClass);
    }

    internal static OperationCallerRestriction Parse(string value) =>
        OperationCallerRestriction.Parse(value) ?? throw new ArgumentException($"'{value}' is not a valid restriction.");

    /// <summary>Each caller profile or none, with no flag, each single flag, or every flag denied.</summary>
    internal static IReadOnlyList<OperationCallerRestriction> Restrictions()
    {
        var flagChoices = OperationRiskVocabulary.Members.Prepend(OperationRisks.None).Append(OperationRiskVocabulary.All);
        return OperationAdmissionProfile.All.Cast<OperationAdmissionProfile?>().Prepend(null)
            .SelectMany(profile => flagChoices.Select(flags => (profile, flags)))
            .Where(choice => choice.profile is not null || choice.flags != OperationRisks.None)
            .Select(choice => OperationCallerRestriction.Create(choice.profile, choice.flags))
            .ToArray();
    }

    // Each setting is combined with every profile.
    private static (string Profile, Dictionary<string, string?> Settings)[] ServerConfigurations()
    {
        var everyFlag = OperationRiskVocabulary.Members.ToDictionary(
            flag => $"Flags:{OperationRiskVocabulary.GetName(flag)}", _ => (string?)"allow", StringComparer.Ordinal);
        var everyOverride = SpatialAnalyzerApi.Operations.ToDictionary(
            operation => "Overrides:" + operation.OperationId.Replace('.', ':'), _ => (string?)"allow", StringComparer.Ordinal);
        var optionOverrides = OperationRequestClassifier.Default.OperationIds.ToDictionary(
            operationId => "Overrides:" + operationId.Replace('.', ':'), _ => (string?)"allow", StringComparer.Ordinal);
        optionOverrides["Flags:interactive_ui"] = "deny";
        var settings = new[]
        {
            new Dictionary<string, string?>(StringComparer.Ordinal),
            new Dictionary<string, string?>(StringComparer.Ordinal) { ["Flags:interactive_ui"] = "allow" },
            new Dictionary<string, string?>(StringComparer.Ordinal) { ["Flags:interactive_ui"] = "deny" },
            everyFlag,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Flags:filesystem_write"] = "deny",
                ["Flags:external_io"] = "allow",
                ["Flags:interactive_ui"] = "allow"
            },
            everyOverride,
            optionOverrides
        };
        return OperationAdmissionProfile.All
            .SelectMany(profile => settings.Select(setting => (profile.Name, setting)))
            .ToArray();
    }

    private static (string Name, OperationPolicy Policy)[] Servers() =>
        ServerConfigurations()
            .Select((configuration, index) => (
                $"{configuration.Profile}#{index % 7}",
                OperationPolicyTests.CreatePolicy(profile: configuration.Profile, settings: configuration.Settings)))
            .ToArray();

    /// <summary>A server configured with only the caller's limits: its profile, or none, and its denied flags.</summary>
    private static OperationPolicy CallerAlone(OperationCallerRestriction restriction)
    {
        var settings = OperationRiskVocabulary.Members
            .Where(flag => restriction.DeniedFlags.HasFlag(flag))
            .ToDictionary(flag => $"Flags:{OperationRiskVocabulary.GetName(flag)}", _ => (string?)"deny",
                StringComparer.Ordinal);
        if (restriction.Profile is null && !restriction.DeniedFlags.HasFlag(OperationRisks.InteractiveUi))
        {
            // No caller profile limits any risk: full with the interactive opt-in admits every flag.
            settings["Flags:interactive_ui"] = "allow";
        }

        return OperationPolicyTests.CreatePolicy(profile: restriction.Profile?.Name ?? "full", settings: settings);
    }

    private static (string Category, string? Problem) Check(
        OperationPolicyDecision without,
        OperationPolicyDecision with,
        OperationPolicyDecision alone,
        OperationCallerRestriction restriction)
    {
        var admittedWith = with.Kind == OperationPolicyDecisionKind.Allowed;
        var admittedWithout = without.Kind == OperationPolicyDecisionKind.Allowed;
        var admittedAlone = alone.Kind == OperationPolicyDecisionKind.Allowed;
        if (!ReferenceEquals(with.CallerRestriction, restriction))
        {
            return ("unrecorded", "the decision does not record the restriction");
        }

        if (admittedWith && !admittedWithout)
        {
            return ("wider", "admitted with the restriction but not without it");
        }

        if (admittedWith && !admittedAlone)
        {
            return ("wider", "admitted with the restriction but not by the caller's own limits");
        }

        if (admittedWith != (admittedWithout && admittedAlone))
        {
            return ("narrower", "denied although both the server and the caller's own limits admit it");
        }

        if (!admittedWithout)
        {
            return SameDecision(with, without) && with.CallerRestrictionOutcome == OperationCallerRestrictionOutcome.NotEvaluated
                ? ("server-denied", null)
                : ("server-denied", "a denial by the server's configuration changed");
        }

        if (admittedWith)
        {
            return SameDecision(with, without) && with.CallerRestrictionOutcome == OperationCallerRestrictionOutcome.Admitted
                ? ("admitted", null)
                : ("admitted", "an admission the restriction keeps changed");
        }

        // The caller denied: the step the evaluator takes for the caller's limits alone.
        var field = alone.Denial!.RequestOptionField;
        var (category, expected) = alone.Denial.Reason switch
        {
            OperationPolicyDenialReason.Flag => (field is null ? "caller-flag" : "caller-flag-option",
                OperationPolicyDenial.CallerFlag(alone.Denial.DeniedFlag!.Value, field)),
            OperationPolicyDenialReason.Profile => (field is null ? "caller-profile" : "caller-profile-option",
                OperationPolicyDenial.CallerProfile(alone.Denial.AdmissionProfile!, field)),
            _ => ("caller-unexpected", null)
        };
        var expectedCode = alone.DiagnosticCode switch
        {
            "operation-option-denied" => "caller-restriction-option-denied",
            "operation-policy-denied" => "caller-restriction-denied",
            _ => null
        };
        return expected is not null && Equals(with.Denial, expected) && with.DiagnosticCode == expectedCode &&
            with.CallerRestrictionOutcome == OperationCallerRestrictionOutcome.Denied &&
            ReferenceEquals(with.Operation, without.Operation) && with.DurationClass is null
            ? (category, null)
            : (category, $"caller denial {with.PolicyRule}/{with.DiagnosticCode} does not match the caller's own " +
                $"limits {alone.PolicyRule}/{alone.DiagnosticCode}");
    }

    private static bool SameDecision(OperationPolicyDecision first, OperationPolicyDecision second) =>
        first.Kind == second.Kind &&
        first.DiagnosticCode == second.DiagnosticCode &&
        first.PolicyRule == second.PolicyRule &&
        first.DurationClass == second.DurationClass &&
        Equals(first.Denial, second.Denial) &&
        ReferenceEquals(first.Operation, second.Operation);

    private static void AssertCoverage(IReadOnlyList<Case> cases)
    {
        Assert.Equal(
            SpatialAnalyzerApi.Operations.Select(operation => operation.OperationId).Order(StringComparer.Ordinal),
            cases.Where(candidate => candidate.Kind == CaseKind.Row).Select(candidate => candidate.OperationId)
                .Order(StringComparer.Ordinal));
        foreach (var kind in new[] { CaseKind.Enabled, CaseKind.Omitted })
        {
            Assert.Equal(
                OperationConditionalOptions.Entries.Select(entry => $"{entry.OperationId}.{entry.Field}").Order(StringComparer.Ordinal),
                cases.Where(candidate => candidate.Kind == kind).Select(candidate => $"{candidate.OperationId}.{candidate.Field}")
                    .Order(StringComparer.Ordinal));
        }

        var withOptions = OperationRequestClassifier.Default.OperationIds.Count();
        foreach (var kind in new[] { CaseKind.AllOff, CaseKind.AllOn, CaseKind.Missing })
        {
            Assert.Equal(withOptions, cases.Count(candidate => candidate.Kind == kind));
        }

        // Every option turned on alone enables exactly that option.
        Assert.All(cases.Where(candidate => candidate.Kind == CaseKind.Enabled), candidate => Assert.Equal(
            candidate.Field,
            Assert.Single(candidate.Classification()!.EnabledOptions).Field));
    }

    internal enum CaseKind
    {
        Row,
        AllOff,
        Enabled,
        Omitted,
        AllOn,
        Missing
    }

    internal sealed record Case(CaseKind Kind, string OperationId, string? Field, IMessage? Request)
    {
        public string Label => Field is null ? $"{OperationId} ({Kind})" : $"{OperationId}.{Field} ({Kind})";

        public OperationPolicyDecision Decide(OperationPolicy policy, OperationCallerRestriction? restriction) =>
            Kind == CaseKind.Row
                ? policy.Evaluate(OperationId, restriction)
                : policy.EvaluateRequest(OperationId, Request, restriction);

        public OperationRequestClassification? Classification() =>
            Kind == CaseKind.Row ? null : OperationRequestClassifier.Default.Classify(OperationId, Request);
    }

    /// <summary>
    /// Every registered operation's reviewed row, and for each operation with reviewed
    /// options: every option off, each option alone on, each option omitted with the
    /// others off, every option on, and no request at all.
    /// </summary>
    internal static IReadOnlyList<Case> Cases()
    {
        var cases = new List<Case>();
        foreach (var operation in SpatialAnalyzerApi.Operations)
        {
            cases.Add(new(CaseKind.Row, operation.OperationId, null, null));
            var options = OperationRequestClassifier.Default.OptionsFor(operation.OperationId);
            if (options.Count == 0)
            {
                continue;
            }

            cases.Add(new(CaseKind.AllOff, operation.OperationId, null, Request(operation.OperationId, enabled: null)));
            foreach (var option in options)
            {
                cases.Add(new(CaseKind.Enabled, operation.OperationId, option.Field,
                    Request(operation.OperationId, enabled: option)));
                var omitted = Request(operation.OperationId, enabled: null);
                omitted.Descriptor.FindFieldByName(option.Field).Accessor.Clear(omitted);
                cases.Add(new(CaseKind.Omitted, operation.OperationId, option.Field, omitted));
            }

            var all = Request(operation.OperationId, enabled: null);
            foreach (var option in options)
            {
                Set(all.Descriptor.FindFieldByName(option.Field), all, option, enabled: true);
            }

            cases.Add(new(CaseKind.AllOn, operation.OperationId, null, all));
            cases.Add(new(CaseKind.Missing, operation.OperationId, null, null));
        }

        return cases;
    }

    // A populated request with every reviewed option off, except enabled when given.
    private static IMessage Request(string operationId, OperationConditionalOption? enabled)
    {
        var builder = OperationConditionalOptionTests.OperationBuilder.For(operationId);
        var request = OperationConditionalOptionTests.RequestPopulator.Create(builder.Request);
        foreach (var option in OperationRequestClassifier.Default.OptionsFor(operationId))
        {
            Set(builder.Request.FindFieldByName(option.Field), request, option, enabled: option == enabled);
        }

        return request;
    }

    private static void Set(FieldDescriptor field, IMessage request, OperationConditionalOption option, bool enabled)
    {
        switch (option.Condition)
        {
            case OperationOptionCondition.WhenTrue:
                field.Accessor.SetValue(request, enabled);
                break;
            case OperationOptionCondition.WhenFalse:
                field.Accessor.SetValue(request, !enabled);
                break;
            case OperationOptionCondition.WhenPresent when enabled:
                field.Accessor.SetValue(request, OperationConditionalOptionTests.RequestPopulator.Create(field.MessageType));
                break;
            case OperationOptionCondition.WhenPresent:
                field.Accessor.Clear(request);
                break;
            case OperationOptionCondition.WhenNonEmpty:
                field.Accessor.SetValue(request, enabled ? "Camera View" : string.Empty);
                break;
            case OperationOptionCondition.WhenOneOf:
                var value = field.EnumType.Values.First(candidate =>
                    candidate.Number != 0 &&
                    option.EnablingValues.Contains(candidate.Name, StringComparer.Ordinal) == enabled);
                field.Accessor.SetValue(request, Enum.ToObject(field.EnumType.ClrType, value.Number));
                break;
            default:
                throw new InvalidOperationException($"Unreviewed condition for {option.OperationId}.{option.Field}.");
        }
    }

    private sealed record ReleasedConfiguration
    {
        public ReleasedConfiguration(string profile, Dictionary<string, string?> settings)
        {
            Profile = OperationAdmissionProfile.Find(profile)!;
            foreach (var (key, value) in settings)
            {
                if (key.StartsWith("Flags:", StringComparison.Ordinal))
                {
                    var flag = OperationRiskVocabulary.Members.Single(member =>
                        OperationRiskVocabulary.GetName(member) == key["Flags:".Length..]);
                    if (value == "allow")
                    {
                        Allowed |= flag;
                    }
                    else
                    {
                        Denied |= flag;
                    }
                }
                else
                {
                    Overrides[key["Overrides:".Length..].Replace(':', '.')] = value == "allow";
                }
            }
        }

        public OperationAdmissionProfile Profile { get; }

        public OperationRisks Allowed { get; }

        public OperationRisks Denied { get; }

        public Dictionary<string, bool> Overrides { get; } = new(StringComparer.Ordinal);
    }

    // The precedence Server 0.10.1 released, transcribed from its policy guide.
    private static (OperationPolicyDecisionKind Kind, string Code, string Rule, OperationDurationClass? Duration,
        OperationPolicyDenial? Denial) Released(
        ReleasedConfiguration configuration,
        OperationDescriptor operation,
        OperationRequestClassification? classification)
    {
        static (OperationPolicyDecisionKind, string, string, OperationDurationClass?, OperationPolicyDenial?) Deny(
            string code, OperationPolicyDenial denial) =>
            (OperationPolicyDecisionKind.Denied, code, denial.PolicyRule, null, denial);

        var lookup = OperationClassification.Find(operation.OperationId);
        if (lookup.Row is not { } row || !lookup.IsReviewed || operation.Effect == Api.OperationEffect.Unknown ||
            operation.ReplaySafety is Api.ReplaySafety.Unspecified or Api.ReplaySafety.Unknown ||
            operation.RiskFlags.Contains("unknown", StringComparer.Ordinal))
        {
            return Deny("operation-risk-unreviewed", OperationPolicyDenial.Unreviewed());
        }

        if (operation.ExecutionScope is Api.OperationExecutionScope.Unspecified or Api.OperationExecutionScope.Unknown)
        {
            return Deny("operation-isolation-unreviewed", OperationPolicyDenial.Unreviewed());
        }

        if (classification is { IsClassified: false })
        {
            return Deny("operation-request-unclassified", OperationPolicyDenial.Unreviewed());
        }

        if (operation.ExecutionScope == Api.OperationExecutionScope.ExclusiveWorkflow)
        {
            return Deny("operation-isolation-unsupported", OperationPolicyDenial.Exclusive());
        }

        var background = classification?.EnabledOptions.FirstOrDefault(option => option.Effect == OperationOptionEffect.BackgroundWork);
        if (background is not null)
        {
            return Deny("operation-option-isolation-unsupported", OperationPolicyDenial.Exclusive(background.Field));
        }

        var ui = classification?.EnabledOptions.FirstOrDefault(option => option.Effect == OperationOptionEffect.InteractiveUi);
        var risks = ui is null ? row.Risks : row.Risks | OperationRisks.InteractiveUi;
        var duration = ui is null ? row.Duration : OperationDurationClass.Interactive;
        var prefix = ui is null ? string.Empty : $"option.{ui.Field}/";
        if (configuration.Overrides.TryGetValue(operation.OperationId, out var allow))
        {
            return allow
                ? (OperationPolicyDecisionKind.Allowed, "operation-policy-allowed", prefix + "override", duration, null)
                : Deny("operation-policy-denied", OperationPolicyDenial.Override());
        }

        var denied = risks & configuration.Denied;
        if (denied != OperationRisks.None)
        {
            var first = OperationRiskVocabulary.Members.First(flag => denied.HasFlag(flag));
            return (row.Risks & configuration.Denied) == OperationRisks.None
                ? Deny("operation-option-denied", OperationPolicyDenial.Flag(first, ui!.Field))
                : Deny("operation-policy-denied", OperationPolicyDenial.Flag(first));
        }

        var profile = configuration.Profile;
        bool Admits(OperationRisks candidate) =>
            (candidate & ~(profile.AdmittedRisks | configuration.Allowed)) == OperationRisks.None &&
            (!profile.ReadOnlyEffectOnly || operation.Effect == Api.OperationEffect.ReadOnly);
        if (Admits(risks))
        {
            return (OperationPolicyDecisionKind.Allowed, "operation-policy-allowed", prefix + $"profile.{profile.Name}",
                duration, null);
        }

        return ui is not null && Admits(row.Risks)
            ? Deny("operation-option-denied", OperationPolicyDenial.Profile(profile.Name, ui.Field))
            : Deny("operation-policy-denied", OperationPolicyDenial.Profile(profile.Name));
    }

    private sealed class NullStatus : IWorkerStatusProvider
    {
        public WorkerLifecycleSnapshot Current { get; } = new(
            WorkerLifecycleState.Stopped, Generation: 0, ProcessId: null, RecoveryCount: 0,
            WorkerTerminationKind.None, "caller-restriction-test", Connection: null, DateTimeOffset.UnixEpoch);
    }

    /// <summary>Maps the admitted request, as the supervisor does, and records the submission.</summary>
    private sealed class MappingDispatcher : IWorkerCommandDispatcher, IWorkerStatusProvider
    {
        public int Submissions { get; private set; }

        public int Mapped { get; private set; }

        public WorkerCommandSubmission? Last { get; private set; }

        public WorkerLifecycleSnapshot Current { get; } = new(
            WorkerLifecycleState.Stopped, Generation: 0, ProcessId: null, RecoveryCount: 0,
            WorkerTerminationKind.None, "caller-restriction-test", Connection: null, DateTimeOffset.UnixEpoch);

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerCommandSubmission submission,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            Submissions++;
            Last = submission;
            _ = submission.CreateCommand();
            Mapped++;
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Overloaded, WorkerExecutionDisposition.NotStarted,
                Execution: null, Connection: null, "recorded", Generation: 0, correlationId));
        }
    }
}
