using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Briosa.Server.Tests;

/// <summary>
/// Cross-checks every implemented operation's SDK step, argument labels, setter/getter method, and
/// direction against the target's committed exact-target evidence (#272, #290). It compares only the
/// implemented subset; it is not an operation generator or inventory-completeness gate.
/// </summary>
/// <remarks>
/// <para>
/// This file is identical in every target. The target-local partial
/// <c>ExactTargetEvidenceCrossCheckTests.Evidence.cs</c> reads that target's evidence into
/// <see cref="OperationEvidence"/>: whether the step text agrees, and each argument's setter and getter
/// label, status, and method. Labels are compared ordinally without normalization: trailing spaces in
/// SDK names are real. An unavailable (<c>NOT_SUPPORTED</c>) binding keeps its label with a null
/// method, so any implemented binding for it is reported for review.
/// </para>
/// <para>
/// Implemented bindings are observed by invoking each operation's own <c>CreateCommand</c> with
/// requests populated from the protobuf request descriptor, one variation at a time (every enum
/// choice, every oneof case, both Boolean values, optional fields present and absent, and list
/// lengths for fixed-size values). That reads the labels, setters, and getters the worker would
/// actually receive, including those built by shared helpers or constants, instead of re-parsing
/// C# source. A label reached only through a value combination that no single variation produces
/// would not be observed, so a target-local source scan requires every literal label to be observed.
/// </para>
/// <para>
/// Reviewed deviations live in the target's <c>EvidenceDeviations.json</c>. A <c>reviewed</c> entry
/// suppresses exactly one finding (command, argument, implemented binding, kind). A
/// <c>reviewed_families</c> rule suppresses every finding of one kind whose implemented binding and
/// evidence method both match, and records how many distinct findings it covers; it is for a
/// documented adapter rule, not for grouping unrelated findings. An entry that no longer matches, a
/// rule whose count changes, and a finding suppressed twice all fail.
/// </para>
/// </remarks>
public sealed partial class ExactTargetEvidenceCrossCheckTests
{
    private const int MaximumMessageDepth = 8;
    private static readonly int[] RepeatedCounts = [1, 6, 16];

    private static readonly string[] FindingKinds =
    [
        "step",
        "setter-label",
        "setter-method",
        "setter-not-supported",
        "setter-direction",
        "getter-label",
        "getter-method",
        "getter-not-supported",
        "getter-direction"
    ];

    private const string EntryGroup = "reviewed";
    private const string FamilyGroup = "reviewed_families";

    private static readonly Lazy<CrossCheck> Result = new(Run);

    [Fact]
    public void EveryRegisteredOperationIsExercised()
    {
        var result = Result.Value;

        Assert.True(result.Unexercised.Count == 0,
            "No generated request variation produced a command; add a representative request:" +
            Environment.NewLine + string.Join(Environment.NewLine, result.Unexercised));
        Assert.True(result.StepTextDisagreements.Count == 0,
            "CreateCommand sent a step other than Descriptor.MpStep:" +
            Environment.NewLine + string.Join(Environment.NewLine, result.StepTextDisagreements));
        Assert.Equal(
            SpatialAnalyzerApi.Operations.Select(operation => operation.OperationId).Order(StringComparer.Ordinal),
            result.ExercisedOperationIds.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ImplementedBindingsMatchExactTargetEvidenceOrAReviewedDeviation()
    {
        var deviations = LoadDeviations();
        var unreviewed = Result.Value.Findings
            .Where(finding => !deviations.Any(deviation => deviation.Matches(finding)))
            .ToArray();

        Assert.True(unreviewed.Length == 0,
            $"{unreviewed.Length} implemented binding(s) disagree with exact-target evidence. Correct the " +
            "operation, or add a reviewed entry with rationale and provenance to EvidenceDeviations.json:" +
            Environment.NewLine + string.Join(Environment.NewLine, unreviewed.Select(Describe)));
    }

    [Fact]
    public void EveryReviewedDeviationStillApplies()
    {
        var findings = Result.Value.Findings;
        var deviations = LoadDeviations();
        var stale = new List<string>();
        foreach (var deviation in deviations)
        {
            var matched = findings.Where(deviation.Matches).Select(finding => finding.Key).Distinct().Count();
            if (matched == 0 || (deviation.FindingCount is { } expected && matched != expected))
            {
                stale.Add($"{Describe(deviation)} matches {matched} finding(s)");
            }
        }

        var overlapping = findings
            .Where(finding => deviations.Count(deviation => deviation.Matches(finding)) > 1)
            .Select(Describe)
            .ToArray();

        Assert.True(stale.Count == 0,
            $"{stale.Count} reviewed deviation(s) no longer match the implemented bindings; delete them or " +
            "re-review the family count:" + Environment.NewLine + string.Join(Environment.NewLine, stale));
        Assert.True(overlapping.Length == 0,
            "These findings are suppressed by more than one deviation; delete the redundant entry:" +
            Environment.NewLine + string.Join(Environment.NewLine, overlapping));
    }

    [Fact]
    public void ReviewedDeviationsAreWellFormedAndUnique()
    {
        var deviations = LoadDeviations();

        Assert.NotEmpty(deviations);
        Assert.All(deviations, deviation =>
        {
            Assert.Contains(deviation.Group, new[] { EntryGroup, FamilyGroup });
            Assert.Contains(deviation.Kind, FindingKinds);
            Assert.False(string.IsNullOrWhiteSpace(deviation.Rationale));
            Assert.False(string.IsNullOrWhiteSpace(deviation.Provenance));
            if (deviation.Group == FamilyGroup)
            {
                Assert.Null(deviation.Command);
                Assert.Null(deviation.Argument);
                Assert.NotEqual("step", deviation.Kind);
                Assert.False(string.IsNullOrWhiteSpace(deviation.Binding));
                Assert.False(string.IsNullOrWhiteSpace(deviation.EvidenceMethod));
                Assert.True(deviation.FindingCount > 0);
                return;
            }

            Assert.False(string.IsNullOrWhiteSpace(deviation.Command));
            Assert.Null(deviation.EvidenceMethod);
            Assert.Null(deviation.FindingCount);
            Assert.Equal(deviation.Kind == "step", deviation.Argument is null);
            if (deviation.Kind == "step")
            {
                Assert.Null(deviation.Binding);
            }
        });
        Assert.Equal(deviations.Length, deviations
            .Select(deviation => (deviation.Command, deviation.Argument, deviation.Binding, deviation.Kind, deviation.EvidenceMethod))
            .Distinct()
            .Count());
    }

    [Theory]
    [InlineData("step", "Angle Between Line and Plane ", null, null)]
    [InlineData("step", "Angle Between Line And Plane", null, null)]
    [InlineData("setter-label", null, "Angle Tolerance (0.0 for none)", "Angle Tolerance (0.0 for none) ")]
    [InlineData("setter-label", null, "Angle Tolerance (0.0 for none)", "Angle tolerance (0.0 for none)")]
    [InlineData("getter-label", null, "Angle", " Angle")]
    public void AlteredStepOrLabelIsAnUnreviewedFinding(string kind, string? step, string? label, string? alteredLabel)
    {
        // Mutation proof: one altered character in an otherwise reviewed operation must surface as an
        // unreviewed finding, so labels and steps are compared exactly, without trimming or case folding.
        const string operationId = "analysis_operations.angle_between_line_and_plane";
        var operation = ImplementedOperations().Single(candidate => candidate.Descriptor.OperationId == operationId);
        var observation = Result.Value.Observed[operationId];
        var evidenceFor = LoadEvidence();
        var deviations = LoadDeviations();
        var baseline = new HashSet<Finding>();
        Compare(baseline, operation, operation.Descriptor.MpStep, observation, evidenceFor);
        Assert.True(baseline.All(finding => deviations.Any(deviation => deviation.Matches(finding))),
            "The unaltered probe operation must match its evidence or a reviewed deviation.");

        static IReadOnlySet<ObservedBinding> Alter(IReadOnlySet<ObservedBinding> bindings, string? label, string? altered) =>
            bindings.Select(binding => binding.Label == label ? binding with { Label = altered! } : binding).ToHashSet();
        var altered = observation with
        {
            Inputs = Alter(observation.Inputs, label, alteredLabel),
            Outputs = Alter(observation.Outputs, label, alteredLabel)
        };
        Assert.True(label is null || !altered.Inputs.SetEquals(observation.Inputs) || !altered.Outputs.SetEquals(observation.Outputs));
        var findings = new HashSet<Finding>();
        Compare(findings, operation, step ?? operation.Descriptor.MpStep, altered, evidenceFor);

        Assert.Contains(findings, finding => finding.Key.Kind == kind &&
            (label is null || finding.Key.Argument == alteredLabel) &&
            !deviations.Any(deviation => deviation.Matches(finding)));
    }

    [Fact]
    public void ObservationReadsLabelsBuiltBySharedHelpers()
    {
        var observed = Result.Value.Observed;

        // Calibration-appliance-node operations build their node argument through a shared helper.
        Assert.Contains(observed["robot_calibration_appliance_node_operations.clear_calibration_appliance_node_trap_manager_requests"].Inputs,
            binding => binding == new ObservedBinding("Calibration Appliance Node", "SetCollectionObjectNameArg2"));
        // Enum-dependent text literals are observed for each reviewed choice.
        Assert.Contains(observed["construction_operations.construct_circles_lines_from_surfaces"].Inputs,
            binding => binding == new ObservedBinding("Circle Line Mode", "SetStringArg"));
        Assert.Contains(observed["construction_operations.construct_circles_lines_from_surfaces"].Outputs,
            binding => binding == new ObservedBinding("Geometry Objects", "GetCollectionObjectNameRefListArg"));
    }

    [Fact]
    public void EveryLiteralSourceLabelIsObserved()
    {
        // Guards the observation strategy: a literal label that no generated request variation
        // reaches would otherwise escape the cross-check silently.
        var observed = Result.Value.Observed;
        var anyOperationLabels = observed.Values
            .SelectMany(operation => operation.Inputs.Concat(operation.Outputs))
            .Select(binding => binding.Label)
            .ToHashSet(StringComparer.Ordinal);
        var unobserved = new List<string>();
        var scanned = 0;
        var operationsRoot = Path.Combine(TargetRoot(), "src", "Briosa.Server", "Operations");
        foreach (var path in Directory.EnumerateFiles(operationsRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(path);
            var operationId = OperationIdPattern().Match(source);
            string owner;
            HashSet<string> labels;
            if (operationId.Success)
            {
                owner = operationId.Groups[1].Value;
                Assert.True(observed.TryGetValue(owner, out var operation), path);
                labels = operation.Inputs.Concat(operation.Outputs)
                    .Select(binding => binding.Label)
                    .ToHashSet(StringComparer.Ordinal);
            }
            else
            {
                // A shared helper or a descriptor keyed by a constant: some observed operation must
                // carry each of its literal labels.
                owner = Path.GetRelativePath(operationsRoot, path);
                labels = anyOperationLabels;
            }

            foreach (var label in LiteralLabelPattern().Matches(source).Select(match => match.Groups[1].Value
                .Replace("\\\"", "\"", StringComparison.Ordinal)
                .Replace("\\\\", "\\", StringComparison.Ordinal)))
            {
                scanned++;
                if (!labels.Contains(label))
                {
                    unobserved.Add($"{owner}: {label}");
                }
            }
        }

        Assert.True(scanned >= SpatialAnalyzerApi.Operations.Count, $"Only {scanned} literal labels were found.");
        Assert.True(unobserved.Count == 0,
            "No generated request variation reached these literal labels:" +
            Environment.NewLine + string.Join(Environment.NewLine, unobserved));
    }

    [GeneratedRegex("""Descriptor \{ get; \} =[^"]*"([a-z0-9_]+\.[a-z0-9_]+)"\s*,""")]
    private static partial Regex OperationIdPattern();

    [GeneratedRegex("""new(?:\s+WorkerMp(?:Input|Output)Argument)?\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*WorkerMpValueKind\.""")]
    private static partial Regex LiteralLabelPattern();

    private static string Describe(Finding finding) => JsonSerializer.Serialize(new
    {
        command = finding.Key.Command,
        argument = finding.Key.Argument,
        binding = finding.Key.Binding,
        kind = finding.Key.Kind,
        operation = finding.OperationId,
        detail = finding.Detail
    });

    private static string Describe(Deviation deviation) => JsonSerializer.Serialize(new
    {
        group = deviation.Group,
        command = deviation.Command,
        argument = deviation.Argument,
        binding = deviation.Binding,
        kind = deviation.Kind,
        evidence_method = deviation.EvidenceMethod,
        finding_count = deviation.FindingCount
    });

    private static CrossCheck Run()
    {
        var evidenceFor = LoadEvidence();
        var observed = new Dictionary<string, ObservedOperation>(StringComparer.Ordinal);
        var unexercised = new List<string>();
        var stepDisagreements = new List<string>();
        var findings = new HashSet<Finding>();
        foreach (var operation in ImplementedOperations())
        {
            var observation = Observe(operation, out var rejection);
            if (observation is null)
            {
                unexercised.Add($"{operation.Descriptor.OperationId}: {rejection}");
                continue;
            }

            observed.Add(operation.Descriptor.OperationId, observation);
            var step = operation.Descriptor.MpStep;
            stepDisagreements.AddRange(observation.Steps
                .Where(commandStep => !string.Equals(commandStep, step, StringComparison.Ordinal))
                .Select(commandStep => $"{operation.Descriptor.OperationId}: '{commandStep}'"));
            Compare(findings, operation, step, observation, evidenceFor);
        }

        return new(observed, unexercised, stepDisagreements, findings
            .OrderBy(finding => finding.Key.Command, StringComparer.Ordinal)
            .ThenBy(finding => finding.Key.Argument, StringComparer.Ordinal)
            .ThenBy(finding => finding.Key.Binding, StringComparer.Ordinal)
            .ThenBy(finding => finding.Key.Kind, StringComparer.Ordinal)
            .ToArray());
    }

    private static void Compare(
        HashSet<Finding> findings,
        ImplementedOperation operation,
        string step,
        ObservedOperation observation,
        Func<string, string, OperationEvidence> evidenceFor)
    {
        var evidence = evidenceFor(operation.Descriptor.OperationId, step);
        if (evidence.StepFinding is not null)
        {
            findings.Add(new(new(step, null, null, "step"), operation.Descriptor.OperationId, null,
                evidence.StepFinding));
        }

        foreach (var input in observation.Inputs)
        {
            AddBindingFinding(findings, operation, step, input, evidence.Arguments, input: true);
        }

        foreach (var output in observation.Outputs)
        {
            AddBindingFinding(findings, operation, step, output, evidence.Arguments, input: false);
        }

        foreach (var label in operation.OutputContracts
            .Select(contract => contract.ArgumentName)
            .Where(label => observation.Outputs.All(output => !string.Equals(output.Label, label, StringComparison.Ordinal))))
        {
            AddBindingFinding(findings, operation, step, new(label, null), evidence.Arguments, input: false,
                compareMethod: false);
        }
    }

    private static void AddBindingFinding(
        HashSet<Finding> findings,
        ImplementedOperation operation,
        string step,
        ObservedBinding binding,
        IReadOnlyList<EvidenceArgument> arguments,
        bool input,
        bool compareMethod = true)
    {
        var (side, otherSide) = input ? ("setter", "getter") : ("getter", "setter");
        Func<EvidenceArgument, EvidenceBinding> sideOf = input ? argument => argument.Setter : argument => argument.Getter;
        Func<EvidenceArgument, EvidenceBinding> otherSideOf = input ? argument => argument.Getter : argument => argument.Setter;
        var matches = arguments
            .Select(sideOf)
            .Where(candidate => string.Equals(candidate.Label, binding.Label, StringComparison.Ordinal))
            .ToArray();
        string kind;
        string? evidenceMethod = null;
        string detail;
        if (matches.Length == 0)
        {
            var otherDirection = arguments.Any(argument => string.Equals(
                otherSideOf(argument).Label, binding.Label, StringComparison.Ordinal));
            kind = otherDirection ? $"{side}-direction" : $"{side}-label";
            detail = otherDirection
                ? $"The label has only {otherSide} evidence."
                : $"No evidence {side} label equals the label.";
        }
        else if (!compareMethod || matches.Any(match =>
            match.Status == "available" &&
            string.Equals(match.Method, binding.SdkBinding, StringComparison.Ordinal)))
        {
            return;
        }
        else if (matches.Any(match => match.Status == "unavailable"))
        {
            kind = $"{side}-not-supported";
            detail = $"The SDK sample is NOT_SUPPORTED; implemented {binding.SdkBinding ?? "<none>"}.";
        }
        else
        {
            kind = $"{side}-method";
            evidenceMethod = string.Join(", ", matches
                .Select(match => match.Method ?? match.Status)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal));
            detail = $"Implemented {binding.SdkBinding ?? "<none>"}; evidence {evidenceMethod}.";
        }

        findings.Add(new(new(step, binding.Label, binding.SdkBinding, kind), operation.Descriptor.OperationId,
            evidenceMethod, detail));
    }

    private static ObservedOperation? Observe(ImplementedOperation operation, out string? rejection)
    {
        var steps = new HashSet<string>(StringComparer.Ordinal);
        var inputs = new HashSet<ObservedBinding>();
        var outputs = new HashSet<ObservedBinding>();
        var requestDescriptor = ((IMessage)Activator.CreateInstance(operation.RequestType)!).Descriptor;
        rejection = null;
        foreach (var variant in Variants(requestDescriptor))
        {
            var request = CreateMessage(requestDescriptor, variant, 0);
            WorkerMpCommand command;
            try
            {
                command = (WorkerMpCommand)operation.CreateCommand.Invoke(null, [request])!;
            }
            catch (TargetInvocationException exception) when (
                exception.InnerException is ArgumentException or InvalidOperationException or NotSupportedException)
            {
                // This variation is not a valid request for the operation.
                rejection ??= exception.InnerException.Message;
                continue;
            }

            steps.Add(command.StepName);
            inputs.UnionWith(command.InputArguments.Select(argument => new ObservedBinding(argument.Name, argument.SdkBinding)));
            outputs.UnionWith(command.OutputArguments.Select(argument => new ObservedBinding(argument.Name, argument.SdkBinding)));
        }

        return steps.Count == 0 ? null : new(steps, inputs, outputs);
    }

    private static IEnumerable<Variant> Variants(MessageDescriptor request)
    {
        var enumFields = new List<FieldDescriptor>();
        var oneofs = new List<OneofDescriptor>();
        CollectVariationPoints(request, enumFields, oneofs, []);

        var maximumChoices = enumFields.Select(field => ChoiceCount(field.EnumType)).DefaultIfEmpty(0).Max();
        // Fixed-size values such as transforms (16) and covariance matrices (6 x 6) need longer lists.
        foreach (var count in RepeatedCounts)
        {
            foreach (var logical in new[] { true, false })
            {
                foreach (var setOptional in new[] { true, false })
                {
                    yield return new(count, logical, setOptional, 0, null, 0, null, null);
                }
            }

            foreach (var field in enumFields)
            {
                for (var index = 1; index < ChoiceCount(field.EnumType); index++)
                {
                    yield return new(count, true, true, 0, field, index, null, null);
                    yield return new(count, false, true, 0, field, index, null, null);
                }
            }

            for (var index = 1; index < maximumChoices; index++)
            {
                yield return new(count, true, true, index, null, 0, null, null);
            }

            foreach (var oneof in oneofs)
            {
                foreach (var field in oneof.Fields.Skip(1))
                {
                    yield return new(count, true, true, 0, null, 0, oneof, field);
                    yield return new(count, false, true, 0, null, 0, oneof, field);
                }
            }
        }
    }

    private static void CollectVariationPoints(
        MessageDescriptor message,
        List<FieldDescriptor> enumFields,
        List<OneofDescriptor> oneofs,
        HashSet<string> visited)
    {
        if (!visited.Add(message.FullName))
        {
            return;
        }

        oneofs.AddRange(message.Oneofs.Where(oneof => !oneof.IsSynthetic));
        foreach (var field in message.Fields.InFieldNumberOrder())
        {
            if (field.FieldType == FieldType.Enum)
            {
                enumFields.Add(field);
            }
            else if (field.FieldType == FieldType.Message)
            {
                CollectVariationPoints(field.MessageType, enumFields, oneofs, visited);
            }
        }
    }

    private static IMessage CreateMessage(MessageDescriptor descriptor, Variant variant, int depth)
    {
        var message = (IMessage)Activator.CreateInstance(descriptor.ClrType)!;
        foreach (var field in descriptor.Fields.InFieldNumberOrder())
        {
            var oneof = field.RealContainingOneof;
            if (oneof is not null)
            {
                var selected = oneof == variant.Oneof ? variant.OneofCase : oneof.Fields[0];
                if (field != selected)
                {
                    continue;
                }
            }
            else if (!variant.SetOptional && field.HasPresence && field.FieldType != FieldType.Message)
            {
                continue;
            }

            var value = CreateValue(field, variant, depth);
            if (value is null || field.IsMap)
            {
                continue;
            }

            if (field.IsRepeated)
            {
                var list = (IList)field.Accessor.GetValue(message);
                list.Add(value);
                for (var index = 1; index < variant.RepeatedCount; index++)
                {
                    list.Add(CreateValue(field, variant, depth));
                }
            }
            else
            {
                field.Accessor.SetValue(message, value);
            }
        }

        return message;
    }

    private static object? CreateValue(FieldDescriptor field, Variant variant, int depth) => field.FieldType switch
    {
        FieldType.Bool => variant.Logical,
        FieldType.String => RepresentativeText.GetValueOrDefault(field.FullName, "Briosa"),
        FieldType.Bytes => ByteString.CopyFromUtf8("Briosa"),
        FieldType.Double => 1d,
        FieldType.Float => 1f,
        FieldType.Int32 or FieldType.SInt32 or FieldType.SFixed32 => 1,
        FieldType.UInt32 or FieldType.Fixed32 => 1u,
        FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64 => 1L,
        FieldType.UInt64 or FieldType.Fixed64 => 1UL,
        FieldType.Enum => EnumValue(field, variant),
        FieldType.Message when depth < MaximumMessageDepth => CreateMessage(field.MessageType, variant, depth + 1),
        _ => null
    };

    private static object EnumValue(FieldDescriptor field, Variant variant)
    {
        var choices = field.EnumType.Values.Where(value => value.Number != 0).ToArray();
        var index = field == variant.EnumField ? variant.EnumFieldIndex : variant.EnumIndex;
        var number = choices.Length == 0 ? 0 : choices[Math.Min(index, choices.Length - 1)].Number;
        return Enum.ToObject(field.EnumType.ClrType, number);
    }

    private static int ChoiceCount(EnumDescriptor descriptor) => descriptor.Values.Count(value => value.Number != 0);

    private static ImplementedOperation[] ImplementedOperations()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var operations = new List<ImplementedOperation>();
        foreach (var type in typeof(SpatialAnalyzerApi).Assembly.GetTypes())
        {
            var descriptor = type.GetProperty("Descriptor", flags);
            var createCommand = type.GetMethods(flags)
                .Where(method => method.Name == "CreateCommand" && method.ReturnType == typeof(WorkerMpCommand))
                .ToArray();
            if (descriptor?.PropertyType != typeof(OperationDescriptor) || createCommand.Length != 1)
            {
                continue;
            }

            var requestType = Assert.Single(createCommand[0].GetParameters()).ParameterType;
            Assert.True(typeof(IMessage).IsAssignableFrom(requestType), type.FullName);
            var outputContracts = type.GetProperty("OutputContracts", flags)?.GetValue(null) as IEnumerable<OperationOutputContract>;
            operations.Add(new(
                (OperationDescriptor)descriptor.GetValue(null)!,
                createCommand[0],
                requestType,
                outputContracts?.ToArray() ?? []));
        }

        return [.. operations];
    }

    private static Deviation[] LoadDeviations()
    {
        var document = JsonNode.Parse(File.ReadAllText(
            Path.Combine(TargetRoot(), "tests", "Briosa.Server.Tests", "EvidenceDeviations.json")))!;
        Assert.Equal(SpatialAnalyzerApi.TargetVersion, document["spatial_analyzer_target"]!.GetValue<string>());
        Assert.All(document.AsObject().Select(property => property.Key), property => Assert.Contains(property,
            new[] { "description", "spatial_analyzer_target", EntryGroup, FamilyGroup }));
        return
        [
            .. document[EntryGroup]!.AsArray().Select(entry => ReadEntry(entry!)),
            .. (document[FamilyGroup]?.AsArray() ?? []).Select(entry => ReadFamily(entry!))
        ];
    }

    private static Deviation ReadEntry(JsonNode entry)
    {
        var properties = entry.AsObject().Select(property => property.Key).Order(StringComparer.Ordinal);
        Assert.Equal(["argument", "binding", "command", "kind", "provenance", "rationale"], properties);
        return new(
            EntryGroup,
            entry["kind"]!.GetValue<string>(),
            entry["command"]!.GetValue<string>(),
            entry["argument"]?.GetValue<string>(),
            entry["binding"]?.GetValue<string>(),
            null,
            null,
            entry["rationale"]!.GetValue<string>(),
            entry["provenance"]!.GetValue<string>());
    }

    private static Deviation ReadFamily(JsonNode entry)
    {
        var properties = entry.AsObject().Select(property => property.Key).Order(StringComparer.Ordinal);
        Assert.Equal(["binding", "evidence_method", "finding_count", "kind", "provenance", "rationale"], properties);
        return new(
            FamilyGroup,
            entry["kind"]!.GetValue<string>(),
            null,
            null,
            entry["binding"]!.GetValue<string>(),
            entry["evidence_method"]!.GetValue<string>(),
            entry["finding_count"]!.GetValue<int>(),
            entry["rationale"]!.GetValue<string>(),
            entry["provenance"]!.GetValue<string>());
    }

    private static string TargetRoot()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Briosa.slnx")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        return root.FullName;
    }

    private sealed record ImplementedOperation(
        OperationDescriptor Descriptor,
        MethodInfo CreateCommand,
        Type RequestType,
        OperationOutputContract[] OutputContracts);

    private sealed record Variant(
        int RepeatedCount,
        bool Logical,
        bool SetOptional,
        int EnumIndex,
        FieldDescriptor? EnumField,
        int EnumFieldIndex,
        OneofDescriptor? Oneof,
        FieldDescriptor? OneofCase);

    private sealed record ObservedBinding(string Label, string? SdkBinding);

    private sealed record ObservedOperation(
        IReadOnlySet<string> Steps,
        IReadOnlySet<ObservedBinding> Inputs,
        IReadOnlySet<ObservedBinding> Outputs);

    /// <summary>One side of an evidence argument. <c>Label</c> is null when the
    /// evidence records no binding on this side.</summary>
    private sealed record EvidenceBinding(string? Label, string Status, string? Method);

    private sealed record EvidenceArgument(EvidenceBinding Setter, EvidenceBinding Getter);

    /// <summary>Evidence for one implemented operation. <c>StepFinding</c> is null when
    /// the evidence step text equals the descriptor step.</summary>
    private sealed record OperationEvidence(string? StepFinding, IReadOnlyList<EvidenceArgument> Arguments);

    private sealed record FindingKey(string Command, string? Argument, string? Binding, string Kind);

    private sealed record Finding(FindingKey Key, string OperationId, string? EvidenceMethod, string Detail);

    private sealed record Deviation(
        string Group,
        string Kind,
        string? Command,
        string? Argument,
        string? Binding,
        string? EvidenceMethod,
        int? FindingCount,
        string Rationale,
        string Provenance)
    {
        public bool Matches(Finding finding) => FindingCount is null
            ? finding.Key == new FindingKey(Command!, Argument, Binding, Kind)
            : finding.Key.Kind == Kind &&
                string.Equals(finding.Key.Binding, Binding, StringComparison.Ordinal) &&
                string.Equals(finding.EvidenceMethod, EvidenceMethod, StringComparison.Ordinal);
    }

    private sealed record CrossCheck(
        IReadOnlyDictionary<string, ObservedOperation> Observed,
        IReadOnlyList<string> Unexercised,
        IReadOnlyList<string> StepTextDisagreements,
        IReadOnlyList<Finding> Findings)
    {
        public IEnumerable<string> ExercisedOperationIds => Observed.Keys;
    }
}
