using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Briosa.Worker.Control;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

#pragma warning disable CA2007 // Test continuations do not rely on a synchronization context.
#pragma warning disable CA1814 // The exact SDK transform contract requires a rectangular array.

public sealed class BindingFamilyAdapterCompletenessTests
{
    private const string Target = "2026.1.0529.7";

    [Fact]
    public void RegistryEvidenceAndContractRowsAreMechanicallyComplete()
    {
        var evidence = LoadEvidence();
        var rows = evidence.BindingRows;

        Assert.Equal(97, evidence.UsableMethods.Count);
        Assert.Equal(103, rows.Count);
        Assert.Equal(79, evidence.ImplementedFamilies.Count);
        Assert.Equal(995, evidence.CommandAssignments.Count);

        Assert.True(
            evidence.UsableMethods.SetEquals(
                typeof(ISpatialAnalyzerSdkCalls)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(IsArgumentMethod)
                .Select(method => method.Name)
                .ToHashSet(StringComparer.Ordinal)));

        foreach (var component in new[] { "protocol", "worker", "adapter", "fake", "generator" })
        {
            Assert.True(evidence.UsableMethods.SetEquals(evidence.ImplementedCoverage[component]));
        }

        var mappedKinds = evidence.ImplementedFamilies
            .Select(FamilyKind)
            .ToHashSet();
        Assert.True(Enum.GetValues<WorkerMpValueKind>().ToHashSet().SetEquals(mappedKinds));
        Assert.True(
            Enum.GetValues<SdkValueKind>()
                .Select(value => value.ToString())
                .ToHashSet(StringComparer.Ordinal)
                .SetEquals(mappedKinds.Select(value => value.ToString())));

        var sharedRows = rows
            .GroupBy(row => row.Method, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Select(row => (row.Method, row.FamilyId)))
            .ToHashSet();
        var assignedPairs = evidence.CommandAssignments
            .Select(assignment => (assignment.Method, assignment.FamilyId))
            .ToHashSet();
        Assert.True(sharedRows.SetEquals(assignedPairs));
        Assert.All(evidence.CommandAssignments, assignment =>
            Assert.Contains((assignment.Method, assignment.FamilyId), sharedRows));
    }

    [Fact]
    public void EveryImplementedPrivateShapeRoundTripsAndRejectsMissingInput()
    {
        var evidence = LoadEvidence();
        var sdkSamples = new List<SdkInputArgument>();

        foreach (var family in evidence.ImplementedFamilies.OrderBy(value => value, StringComparer.Ordinal))
        {
            var setter = Assert.Single(
                evidence.BindingRows,
                row => row.Direction == "setter" && row.FamilyId == family).Method;
            var input = CreateInput(family, $"round-trip:{family}", setter);
            var command = RoundTripCommand(new WorkerMpCommand(
                family,
                family,
                [input],
                []));
            var sdkInput = Assert.Single(WorkerControlHost.ToSdkCommand(command).InputArguments);

            Assert.Equal(FamilyKind(family).ToString(), sdkInput.Kind.ToString());
            Assert.Equal(setter, sdkInput.SdkBinding);
            sdkSamples.Add(sdkInput);

            var missing = new WorkerMpInputArgument(
                $"missing:{family}",
                FamilyKind(family),
                SdkBinding: setter);
            AssertTransportRejected(new WorkerMpCommand(
                $"missing-{family}",
                $"missing-{family}",
                [missing],
                []));
        }

        var sampleObjects = sdkSamples.SelectMany(Traverse).ToArray();
        foreach (var structuredType in evidence.StructuredTypes)
        {
            var instances = sampleObjects
                .Where(value => value.GetType().Name == structuredType.WorkerType)
                .Distinct(ReferenceEqualityComparer.Instance)
                .ToArray();
            Assert.NotEmpty(instances);
            Assert.All(instances, instance => Assert.Equal(
                structuredType.WorkerFields.OrderBy(value => value, StringComparer.Ordinal),
                instance.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Select(property => property.Name)
                    .OrderBy(value => value, StringComparer.Ordinal)));
        }
    }

    [Fact]
    public async Task EveryBindingFamilyRowRunsSuccessAndNegativePathsOnTheOwnedSta()
    {
        var evidence = LoadEvidence();
        CompletenessSdkCalls? fake = null;
        await using var executor = new SerializedSdkExecutor(() =>
        {
            fake = CompletenessSdkCalls.Create();
            return new SpatialAnalyzerSdkAdapter(fake.Calls);
        });

        foreach (var row in evidence.BindingRows)
        {
            if (row.Direction == "setter")
            {
                fake!.Clear();
                var success = await ExecuteAsync(
                    executor,
                    new WorkerMpCommand(
                        $"success-{row.Method}-{row.FamilyId}",
                        $"success:{row.Method}:{row.FamilyId}",
                        [CreateInput(row.FamilyId, $"success:{row.FamilyId}", row.Method)],
                        []));
                Assert.True(success.MpResult.Succeeded);
                AssertCalls(fake, "SetStep", row.Method, "ExecuteStep", "GetMPStepResult");
                AssertVariantBoundary(fake, row.Method);

                fake.Clear();
                var rejected = await ExecuteAsync(
                    executor,
                    new WorkerMpCommand(
                        $"reject-{row.Method}-{row.FamilyId}",
                        $"reject:{row.Method}:{row.FamilyId}",
                        [CreateInput(row.FamilyId, $"reject:{row.FamilyId}", row.Method)],
                        []));
                Assert.False(rejected.ExecuteStepReturned);
                Assert.Equal("sdk-argument-rejected", rejected.DiagnosticCode);
                AssertCalls(fake, "SetStep", row.Method);
                continue;
            }

            fake!.Clear();
            var output = new WorkerMpOutputArgument(
                $"success:{row.FamilyId}",
                FamilyKind(row.FamilyId),
                row.Method);
            var successOutput = await ExecuteAsync(
                executor,
                new WorkerMpCommand(
                    $"success-{row.Method}-{row.FamilyId}",
                    $"success:{row.Method}:{row.FamilyId}",
                    [],
                    [output]));
            Assert.True(Assert.Single(successOutput.OutputValues).Retrieved);
            AssertCalls(fake, "SetStep", "ExecuteStep", "GetMPStepResult", row.Method);
            AssertVariantBoundary(fake, row.Method);
            RoundTripExecution(successOutput);

            fake.Clear();
            var getterFailure = await ExecuteAsync(
                executor,
                new WorkerMpCommand(
                    $"getter-failure-{row.Method}-{row.FamilyId}",
                    $"getter-failure:{row.Method}:{row.FamilyId}",
                    [],
                    [output with { Name = $"getter-failure:{row.FamilyId}" }]));
            Assert.False(Assert.Single(getterFailure.OutputValues).Retrieved);
            Assert.Equal("sdk-output-retrieval-failed", getterFailure.DiagnosticCode);
            AssertCalls(fake, "SetStep", "ExecuteStep", "GetMPStepResult", row.Method);

            fake.Clear();
            var mpFailure = await ExecuteAsync(
                executor,
                new WorkerMpCommand(
                    $"mp-failure-{row.Method}-{row.FamilyId}",
                    $"mp-failure:{row.Method}:{row.FamilyId}",
                    [],
                    [output with { Name = $"mp-failure:{row.FamilyId}" }]));
            Assert.False(mpFailure.MpResult.Succeeded);
            Assert.Empty(mpFailure.OutputValues);
            AssertCalls(fake, "SetStep", "ExecuteStep", "GetMPStepResult");
        }
    }

    [Fact]
    public async Task EveryEvidenceEnumMemberEmitsItsExactSdkLiteral()
    {
        var evidence = LoadEvidence();
        CompletenessSdkCalls? fake = null;
        await using var executor = new SerializedSdkExecutor(() =>
        {
            fake = CompletenessSdkCalls.Create();
            return new SpatialAnalyzerSdkAdapter(fake.Calls);
        });
        var exercised = 0;

        foreach (var enumType in evidence.EnumTypes)
        {
            var family = EnumCarrierFamily(enumType.WorkerType);
            var setter = Assert.Single(
                evidence.BindingRows,
                row => row.Direction == "setter" && row.FamilyId == family).Method;
            foreach (var member in enumType.Members)
            {
                fake!.Clear();
                var input = CreateInput(
                    family,
                    $"enum:{enumType.WorkerType}:{member.WorkerSymbol}",
                    setter,
                    enumType.WorkerType,
                    member.PublicNumber);
                var result = await ExecuteAsync(
                    executor,
                    new WorkerMpCommand(
                        $"enum-{enumType.WorkerType}-{member.WorkerSymbol}",
                        $"enum:{enumType.WorkerType}:{member.WorkerSymbol}",
                        [input],
                        []));

                Assert.True(result.MpResult.Succeeded);
                var call = Assert.Single(fake.CallsFor(setter));
                var literalIndex = setter == "SetCollectionObjectNameArg2" ? 3 : 1;
                Assert.Equal(member.SdkLiteral, Assert.IsType<string>(call.Arguments[literalIndex]));
                exercised++;
            }
        }

        Assert.Equal(470, exercised);
    }

    [Fact]
    public async Task SharedCollectionDomainsFailClosedForUnknownReturnedTypeLiterals()
    {
        var evidence = LoadEvidence();
        var rows = evidence.BindingRows.Where(row =>
            row.Direction == "getter" &&
            row.Method is "GetCollectionObjectNameArg" or
                "GetCollectionObjectNameRefListArg").ToArray();
        Assert.Equal(4, rows.Length);

        CompletenessSdkCalls? fake = null;
        await using var executor = new SerializedSdkExecutor(() =>
        {
            fake = CompletenessSdkCalls.Create();
            return new SpatialAnalyzerSdkAdapter(fake.Calls);
        });

        foreach (var row in rows)
        {
            fake!.Clear();
            var result = await ExecuteAsync(
                executor,
                new WorkerMpCommand(
                    $"unknown-{row.Method}-{row.FamilyId}",
                    $"unknown:{row.Method}:{row.FamilyId}",
                    [],
                    [new(
                        $"unknown:{row.FamilyId}",
                        FamilyKind(row.FamilyId),
                        row.Method)]));

            Assert.False(Assert.Single(result.OutputValues).Retrieved);
            Assert.Equal("sdk-output-retrieval-failed", result.DiagnosticCode);
            AssertCalls(fake, "SetStep", "ExecuteStep", "GetMPStepResult", row.Method);
        }
    }

    private static bool IsArgumentMethod(MethodInfo method) =>
        (method.Name.StartsWith("Get", StringComparison.Ordinal) ||
         method.Name.StartsWith("Set", StringComparison.Ordinal)) &&
        method.Name.Contains("Arg", StringComparison.Ordinal);

    private static async Task<SdkExecutionResult> ExecuteAsync(
        SerializedSdkExecutor executor,
        WorkerMpCommand command)
    {
        var roundTrip = RoundTripCommand(command);
        return await executor.ExecuteAsync(WorkerControlHost.ToSdkCommand(roundTrip));
    }

    private static WorkerMpCommand RoundTripCommand(WorkerMpCommand command)
    {
        using var stream = new MemoryStream();
        using (var sender = new WorkerControlChannel(stream, leaveOpen: true))
        {
            sender.Send(WorkerControlMessage.Execute(Guid.NewGuid(), command));
        }

        stream.Position = 0;
        using var receiver = new WorkerControlChannel(stream, leaveOpen: true);
        return receiver.Receive().Command!;
    }

    private static void RoundTripExecution(SdkExecutionResult result)
    {
        var execution = WorkerControlHost.ToControlResult(result);
        var snapshot = new WorkerConnectionSnapshot(
            WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.ExecutionReady,
            StatusCode: 0,
            Attempt: 1,
            MaximumAttempts: 1,
            DiagnosticCode: "execution-ready",
            DateTimeOffset.UnixEpoch);
        var message = WorkerControlMessage.ExecutionResult(
            Guid.NewGuid(),
            new WorkerExecutionResponse(
                WorkerExecutionResponseStatus.Completed,
                execution,
                snapshot,
                DiagnosticCode: null));
        using var stream = new MemoryStream();
        using (var sender = new WorkerControlChannel(stream, leaveOpen: true))
        {
            sender.Send(message);
        }

        stream.Position = 0;
        using var receiver = new WorkerControlChannel(stream, leaveOpen: true);
        var roundTrip = receiver.Receive().ExecutionResponse!.Execution!;
        Assert.Equal(execution.OutputValues.Count, roundTrip.OutputValues.Count);
        Assert.Equal(
            execution.OutputValues.Select(output => (output.Name, output.Kind, output.Retrieved)),
            roundTrip.OutputValues.Select(output => (output.Name, output.Kind, output.Retrieved)));
    }

    private static void AssertTransportRejected(WorkerMpCommand command)
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        Assert.Throws<InvalidDataException>(() =>
            channel.Send(WorkerControlMessage.Execute(Guid.NewGuid(), command)));
        Assert.Equal(0, stream.Length);
    }

    private static void AssertCalls(CompletenessSdkCalls fake, params string[] methods)
    {
        Assert.Equal(methods, fake.Invocations.Select(call => call.Method));
        Assert.All(
            fake.Invocations,
            call => Assert.Equal(ApartmentState.STA, call.ApartmentState));
    }

    private static void AssertVariantBoundary(CompletenessSdkCalls fake, string methodName)
    {
        var method = typeof(ISpatialAnalyzerSdkCalls).GetMethod(methodName)!;
        var objectByRefIndexes = method.GetParameters()
            .Select((parameter, index) => (parameter, index))
            .Where(item =>
                item.parameter.ParameterType.IsByRef &&
                item.parameter.ParameterType.GetElementType() == typeof(object))
            .Select(item => item.index)
            .ToArray();
        if (objectByRefIndexes.Length == 0)
        {
            return;
        }

        var call = Assert.Single(fake.CallsFor(methodName));
        Assert.All(
            objectByRefIndexes,
            index => Assert.IsType<VariantWrapper>(call.Arguments[index]));
    }

    private static WorkerMpValueKind FamilyKind(string familyId)
    {
        var name = familyId switch
        {
            "string" => nameof(WorkerMpValueKind.Text),
            "vector3" => nameof(WorkerMpValueKind.Vector),
            _ => string.Concat(familyId
                .Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]))
        };
        return Enum.Parse<WorkerMpValueKind>(name, ignoreCase: false);
    }

    private static string EnumCarrierFamily(string workerType) => workerType switch
    {
        "SdkCloudThinningModeValue" => "cloud_thinning_options",
        "SdkItemTypeValue" => "collection_item_name",
        "SdkReportOutputTypeValue" => "report_output_options",
        "SdkReportViewTypeValue" => "report_view_options",
        _ => LoadEvidence().FamiliesByWorkerType[workerType]
    };

    private static WorkerMpInputArgument CreateInput(
        string familyId,
        string name,
        string binding,
        string? enumWorkerType = null,
        int publicEnumNumber = 1)
    {
        var kind = FamilyKind(familyId);
        var transform = Enumerable.Range(0, 16).Select(value => (double)value).ToArray();
        var tolerance = new WorkerToleranceLimit(true, 1.25);
        var scalarTolerance = new WorkerScalarToleranceLimit(true, 1.25);
        var specializedNumber = publicEnumNumber - 1;

        return kind switch
        {
            WorkerMpValueKind.Logical => new(name, kind, BooleanValue: true, SdkBinding: binding),
            WorkerMpValueKind.WholeNumber => new(name, kind, IntegerValue: 7, SdkBinding: binding),
            WorkerMpValueKind.FloatingPoint => new(name, kind, DoubleValue: 1.25, SdkBinding: binding),
            WorkerMpValueKind.Text or
            WorkerMpValueKind.ChartName or
            WorkerMpValueKind.CloudName or
            WorkerMpValueKind.CollectionName or
            WorkerMpValueKind.FrameName or
            WorkerMpValueKind.VectorGroupName or
            WorkerMpValueKind.ViewName =>
                new(name, kind, StringValue: "value", SdkBinding: binding),
            WorkerMpValueKind.DoubleArray =>
                new(name, kind, DoubleArrayValue: new([1d, 2d, 3d]), SdkBinding: binding),
            WorkerMpValueKind.EditText =>
                new(name, kind, StringListValue: new(["A", "B"]), SdkBinding: binding),
            WorkerMpValueKind.Transform =>
                new(name, kind, TransformValue: new(transform), SdkBinding: binding),
            WorkerMpValueKind.WorldTransform =>
                new(name, kind, WorldTransformValue: new(new(transform), 2.5), SdkBinding: binding),
            WorkerMpValueKind.RgbColor =>
                new(name, kind, RgbColorValue: new(1, 2, 3), SdkBinding: binding),
            WorkerMpValueKind.FileReference =>
                new(name, kind, FileReferenceValue: new("file.xit", false), SdkBinding: binding),
            WorkerMpValueKind.AngularUnit => new(
                name,
                kind,
                AngularUnitValue: (WorkerAngularUnitValue)publicEnumNumber,
                SdkBinding: binding),
            WorkerMpValueKind.DistanceUnit => new(
                name,
                kind,
                DistanceUnitValue: (WorkerDistanceUnitValue)publicEnumNumber,
                SdkBinding: binding),
            WorkerMpValueKind.TemperatureUnit => new(
                name,
                kind,
                TemperatureUnitValue: (WorkerTemperatureUnitValue)publicEnumNumber,
                SdkBinding: binding),
            WorkerMpValueKind.Font =>
                new(name, kind, FontValue: new("Segoe UI", 12, new(1, 2, 3)), SdkBinding: binding),
            WorkerMpValueKind.PointName => new(
                name,
                kind,
                PointNameValue: new("Collection", "Group", "Point"),
                SdkBinding: binding),
            WorkerMpValueKind.Vector =>
                new(name, kind, VectorValue: new(1, 2, 3), SdkBinding: binding),
            WorkerMpValueKind.ToleranceVectorOptions => new(
                name,
                kind,
                ToleranceVectorOptionsValue: new(
                    tolerance,
                    tolerance,
                    tolerance,
                    tolerance,
                    tolerance,
                    tolerance,
                    tolerance,
                    tolerance),
                SdkBinding: binding),
            WorkerMpValueKind.CollectionInstrumentId => new(
                name,
                kind,
                CollectionInstrumentIdValue: new("Collection", 17),
                SdkBinding: binding),
            WorkerMpValueKind.CollectionInstrumentIdList => new(
                name,
                kind,
                CollectionInstrumentIdListValue: new([new("Collection", 17)]),
                SdkBinding: binding),
            WorkerMpValueKind.CollectionMachineId => new(
                name,
                kind,
                CollectionMachineIdValue: new("Collection", 17),
                SdkBinding: binding),
            WorkerMpValueKind.CollectionItemName => new(
                name,
                kind,
                CollectionItemNameValue: new(
                    "Collection",
                    "Item",
                    enumWorkerType == "SdkItemTypeValue"
                        ? (WorkerItemTypeValue)publicEnumNumber
                        : WorkerItemTypeValue.Any),
                SdkBinding: binding),
            WorkerMpValueKind.CollectionItemNameList => new(
                name,
                kind,
                CollectionItemNameListValue: new([
                    new("Collection", "Item", WorkerItemTypeValue.Any)
                ]),
                SdkBinding: binding),
            WorkerMpValueKind.CollectionObjectName => new(
                name,
                kind,
                CollectionObjectNameValue: new(
                    "Collection",
                    "Object",
                    WorkerObjectTypeValue.Any),
                SdkBinding: binding),
            WorkerMpValueKind.CollectionObjectNameList => new(
                name,
                kind,
                CollectionObjectNameListValue: new([
                    new("Collection", "Object", WorkerObjectTypeValue.Any)
                ]),
                SdkBinding: binding),
            WorkerMpValueKind.CollectionGroupNameList => new(
                name,
                kind,
                CollectionGroupNameListValue: new([new("Collection", "Group")]),
                SdkBinding: binding),
            WorkerMpValueKind.CollectionVectorGroupName => new(
                name,
                kind,
                CollectionVectorGroupNameValue: new("Collection", "Vectors"),
                SdkBinding: binding),
            WorkerMpValueKind.CollectionVectorGroupNameList => new(
                name,
                kind,
                CollectionVectorGroupNameListValue: new([new("Collection", "Vectors")]),
                SdkBinding: binding),
            WorkerMpValueKind.PointNameList => new(
                name,
                kind,
                PointNameListValue: new([new("Collection", "Group", "Point")]),
                SdkBinding: binding),
            WorkerMpValueKind.StringList => new(
                name,
                kind,
                StringListValue: new(["A", "B"]),
                SdkBinding: binding),
            WorkerMpValueKind.VectorNameList => new(
                name,
                kind,
                VectorNameListValue: new([new("Collection", "Vectors", "Vector")]),
                SdkBinding: binding),
            WorkerMpValueKind.AutoFilterProximitySettings => new(
                name,
                kind,
                AutoFilterProximitySettingsValue: new(1, 2, 3, 4, 5, 6, 0, 1, 2, true, false),
                SdkBinding: binding),
            WorkerMpValueKind.CloudThinningOptions => new(
                name,
                kind,
                CloudThinningOptionsValue: new(
                    enumWorkerType == "SdkCloudThinningModeValue" ? specializedNumber : 0,
                    2,
                    3,
                    4),
                SdkBinding: binding),
            WorkerMpValueKind.ColorizationOptions => new(
                name,
                kind,
                ColorizationOptionsValue: new(
                    0, 0, 0, 0, true, false, true, 2, 3, false, 4, true, false, true,
                    false, 5, -5, 1, -1),
                SdkBinding: binding),
            WorkerMpValueKind.FitConstraintScalarOptions => new(
                name,
                kind,
                FitConstraintScalarOptionsValue: new(scalarTolerance, scalarTolerance),
                SdkBinding: binding),
            WorkerMpValueKind.FitDegreeOfFreedomOptions => new(
                name,
                kind,
                FitDegreeOfFreedomOptionsValue: new(true, false, true, false, true, false, true),
                SdkBinding: binding),
            WorkerMpValueKind.ReportOutputOptions => new(
                name,
                kind,
                ReportOutputOptionsValue: enumWorkerType == "SdkReportOutputTypeValue"
                    ? new(specializedNumber, "report.pdf", null)
                    : new(0, null, new("Collection", "Report")),
                SdkBinding: binding),
            WorkerMpValueKind.ReportViewOptions => new(
                name,
                kind,
                ReportViewOptionsValue: new(
                    enumWorkerType == "SdkReportViewTypeValue" ? specializedNumber : 0,
                    "Collection",
                    "Callout"),
                SdkBinding: binding),
            WorkerMpValueKind.ToleranceScalarOptions => new(
                name,
                kind,
                ToleranceScalarOptionsValue: new(scalarTolerance, scalarTolerance),
                SdkBinding: binding),
            _ => new(
                name,
                kind,
                SpecializedEnumValue: new(specializedNumber),
                SdkBinding: binding)
        };
    }

    private static IEnumerable<object> Traverse(object? value)
    {
        if (value is null ||
            value is string ||
            value.GetType().IsPrimitive ||
            value.GetType().IsEnum ||
            value is decimal)
        {
            yield break;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                foreach (var nested in Traverse(item))
                {
                    yield return nested;
                }
            }

            yield break;
        }

        yield return value;
        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            foreach (var nested in Traverse(property.GetValue(value)))
            {
                yield return nested;
            }
        }
    }

    private static ContractEvidence LoadEvidence()
    {
        var root = FindRepositoryRoot().FullName;
        using var registry = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "bindings", "sa", Target, "registry.json")));
        using var review = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "bindings", "sa", Target, "review.json")));
        using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "values", "sa", Target, "catalog.json")));

        var usable = registry.RootElement.GetProperty("bindings")
            .EnumerateArray()
            .Where(binding => binding.GetProperty("registry_status").GetString() == "usable")
            .ToArray();
        var rows = usable
            .SelectMany(binding => binding.GetProperty("semantic_value_families")
                .EnumerateArray()
                .Select(family => new BindingRow(
                    binding.GetProperty("method").GetString()!,
                    binding.GetProperty("direction").GetString()!,
                    family.GetString()!)))
            .OrderBy(row => row.Method, StringComparer.Ordinal)
            .ThenBy(row => row.FamilyId, StringComparer.Ordinal)
            .ToArray();
        var implementedFamilies = catalog.RootElement.GetProperty("families")
            .EnumerateArray()
            .Where(family => family.GetProperty("implementation_status").GetString() == "implemented")
            .Select(family => family.GetProperty("family_id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        var familiesByWorkerType = catalog.RootElement.GetProperty("families")
            .EnumerateArray()
            .Where(family => family.GetProperty("implementation_status").GetString() == "implemented")
            .Where(family => family.GetProperty("shape").GetString() == "enum")
            .ToDictionary(
                family => family.GetProperty("worker_type_target").GetString()!,
                family => family.GetProperty("family_id").GetString()!,
                StringComparer.Ordinal);
        var enumTypes = catalog.RootElement.GetProperty("enum_types")
            .EnumerateArray()
            .Select(enumType => new EvidenceEnumType(
                enumType.GetProperty("worker_type").GetString()!,
                enumType.GetProperty("members")
                    .EnumerateArray()
                    .Select(member => new EvidenceEnumMember(
                        member.GetProperty("worker_symbol").GetString()!,
                        member.GetProperty("public_number").GetInt32(),
                        member.GetProperty("sdk_literal").GetString()!))
                    .ToArray()))
            .ToArray();
        var structuredTypes = catalog.RootElement.GetProperty("structured_types")
            .EnumerateArray()
            .Select(type => new EvidenceStructuredType(
                type.GetProperty("worker_type").GetString()!,
                type.GetProperty("worker_fields")
                    .EnumerateArray()
                    .Select(field => field.GetString()!)
                    .ToArray()))
            .ToArray();
        var commandAssignments = catalog.RootElement.GetProperty("command_assignments")
            .EnumerateArray()
            .Select(assignment => new CommandAssignment(
                assignment.GetProperty("method").GetString()!,
                assignment.GetProperty("family_id").GetString()!))
            .ToArray();
        var coverage = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);
        var coverageElement = review.RootElement.GetProperty("implemented_coverage");
        foreach (var component in new[] { "protocol", "worker", "adapter", "fake", "generator" })
        {
            coverage[component] = coverageElement.GetProperty(component)
                .EnumerateArray()
                .Select(method => method.GetString()!)
                .ToHashSet(StringComparer.Ordinal);
        }

        return new ContractEvidence(
            usable.Select(binding => binding.GetProperty("method").GetString()!)
                .ToHashSet(StringComparer.Ordinal),
            rows,
            implementedFamilies,
            familiesByWorkerType,
            enumTypes,
            structuredTypes,
            commandAssignments,
            coverage);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException("Could not find the repository root.");
    }

    private sealed record BindingRow(string Method, string Direction, string FamilyId);
    private sealed record EvidenceEnumMember(string WorkerSymbol, int PublicNumber, string SdkLiteral);
    private sealed record EvidenceEnumType(string WorkerType, IReadOnlyList<EvidenceEnumMember> Members);
    private sealed record EvidenceStructuredType(string WorkerType, IReadOnlyList<string> WorkerFields);
    private sealed record CommandAssignment(string Method, string FamilyId);
    private sealed record ContractEvidence(
        IReadOnlySet<string> UsableMethods,
        IReadOnlyList<BindingRow> BindingRows,
        IReadOnlySet<string> ImplementedFamilies,
        IReadOnlyDictionary<string, string> FamiliesByWorkerType,
        IReadOnlyList<EvidenceEnumType> EnumTypes,
        IReadOnlyList<EvidenceStructuredType> StructuredTypes,
        IReadOnlyList<CommandAssignment> CommandAssignments,
        IReadOnlyDictionary<string, IReadOnlySet<string>> ImplementedCoverage);

    private sealed record SdkCall(
        string Method,
        string Name,
        ApartmentState ApartmentState,
        IReadOnlyList<object?> Arguments);

#pragma warning disable CA1812, CA1852 // DispatchProxy constructs a runtime-derived instance.
    private class CompletenessSdkCalls : DispatchProxy
    {
        private string _stepName = string.Empty;

        public ISpatialAnalyzerSdkCalls Calls { get; private set; } = null!;

        public List<SdkCall> Invocations { get; } = [];

        public static CompletenessSdkCalls Create()
        {
            var calls = DispatchProxy.Create<ISpatialAnalyzerSdkCalls, CompletenessSdkCalls>();
            var proxy = (CompletenessSdkCalls)(object)calls;
            proxy.Calls = calls;
            return proxy;
        }

        public SdkCall[] CallsFor(string method) =>
            Invocations.Where(call => call.Method == method).ToArray();

        public void Clear() => Invocations.Clear();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var method = targetMethod ?? throw new InvalidOperationException("Missing SDK method metadata.");
            args ??= [];
            var name = args.FirstOrDefault() as string ?? _stepName;
            Invocations.Add(new SdkCall(
                method.Name,
                name,
                Thread.CurrentThread.GetApartmentState(),
                [.. args]));

            switch (method.Name)
            {
                case nameof(ISpatialAnalyzerSdkCalls.SetStep):
                    _stepName = Assert.IsType<string>(args[0]);
                    return null;
                case nameof(ISpatialAnalyzerSdkCalls.ConnectEx):
                    args[1] = 0;
                    return true;
                case nameof(ISpatialAnalyzerSdkCalls.ExecuteStep):
                    return true;
                case nameof(ISpatialAnalyzerSdkCalls.GetMPStepResult):
                    args[0] = _stepName.StartsWith("mp-failure:", StringComparison.Ordinal) ? 3 : 2;
                    return true;
                case nameof(IDisposable.Dispose):
                    return null;
            }

            if (method.Name.StartsWith("Set", StringComparison.Ordinal))
            {
                return !name.StartsWith("reject:", StringComparison.Ordinal);
            }

            var retrieved = !name.StartsWith("getter-failure:", StringComparison.Ordinal);
            PopulateGetter(method.Name, name, args);
            return retrieved;
        }

        private static void PopulateGetter(string method, string name, object?[] args)
        {
            var unknown = name.StartsWith("unknown:", StringComparison.Ordinal);
            var item = name.Contains("collection_item", StringComparison.Ordinal);
            switch (method)
            {
                case "GetBoolArg":
                    args[1] = true;
                    break;
                case "GetIntegerArg":
                    args[1] = 7;
                    break;
                case "GetDoubleArg":
                    args[1] = 1.25;
                    break;
                case "GetStringArg":
                    args[1] = "value";
                    break;
                case "GetPointNameArg":
                    args[1] = "Collection";
                    args[2] = "Group";
                    args[3] = "Point";
                    break;
                case "GetColInstIdArg":
                    args[1] = "Collection";
                    args[2] = 17;
                    break;
                case "GetColInstIdRefListArg":
                    args[1] = new object[] { "Collection::17" };
                    break;
                case "GetCollectionNameArg":
                    args[1] = "Collection";
                    break;
                case "GetCollectionObjectNameArg":
                    args[1] = "Collection";
                    args[2] = unknown
                        ? "Object,Future Type,"
                        : item ? "Picture,Picture," : "Object,Point Group,";
                    break;
                case "GetCollectionObjectNameRefListArg":
                    args[1] = new object[]
                    {
                        unknown
                            ? "Collection::Object::Future Type,"
                            : item
                                ? "Collection::Report::SA Report,"
                                : "Collection::Object::Point Group,"
                    };
                    break;
                case "GetPointNameRefListArg":
                    args[1] = new object[] { "Collection::Group::Point" };
                    break;
                case "GetStringRefListArg":
                    args[1] = new object[] { "A", "B" };
                    break;
                case "GetVectorNameRefListArg":
                    args[1] = new object[] { "Collection::Vectors::Vector" };
                    break;
                case "GetVectorArg":
                    args[1] = 1d;
                    args[2] = 2d;
                    args[3] = 3d;
                    break;
                case "GetToleranceVectorOptionsArg":
                    for (var index = 1; index < args.Length; index += 2)
                    {
                        args[index] = true;
                        args[index + 1] = (double)index;
                    }
                    break;
                case "GetDoubleArrayArg":
                    args[1] = 3;
                    args[2] = new double[] { 1, 2, 3 };
                    break;
                case "GetEditTextArg":
                    args[1] = new object[] { "A", "B" };
                    break;
                case "GetTransformArg":
                    args[1] = Matrix();
                    break;
                case "GetWorldTransformArg":
                    args[1] = Matrix();
                    args[2] = 2.5;
                    break;
                case "GetFilePathArg":
                    args[1] = "file.xit";
                    args[2] = false;
                    break;
                case "GetFitConstraintScalarOptionsArg":
                case "GetToleranceScalarOptionsArg":
                    args[1] = true;
                    args[2] = 1.25;
                    args[3] = false;
                    args[4] = -2.5;
                    break;
                default:
                    throw new InvalidOperationException($"No fake getter behavior exists for '{method}'.");
            }
        }

        private static double[,] Matrix()
        {
            var matrix = new double[4, 4];
            for (var index = 0; index < 16; index++)
            {
                matrix[index / 4, index % 4] = index;
            }

            return matrix;
        }
    }
#pragma warning restore CA1812, CA1852
}

#pragma warning restore CA1814
#pragma warning restore CA2007
