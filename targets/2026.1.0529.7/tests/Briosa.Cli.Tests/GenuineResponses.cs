using System.Collections;
using System.Reflection;
using Briosa.Cli.Engine;
using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Configuration;

namespace Briosa.Cli.Tests;

/// <summary>
/// Genuine server responses for every registered operation, produced by the server's
/// own code: each operation's real output contracts, the real
/// <c>GrpcOperationOutcomeMapper</c>, and the operation's real <c>CreateResult</c>.
/// The server keeps all of it internal, so it is reached by reflection.
/// </summary>
internal static class GenuineResponses
{
    private static readonly Assembly Server = Assembly.Load(new AssemblyName("Briosa.Server"));
    private static readonly Type Mapper = Server.GetType("Briosa.Server.Services.GrpcOperationOutcomeMapper", true)!;
    private static readonly Type Outcome = Server.GetType("Briosa.Server.Workers.WorkerExecutionOutcome", true)!;
    private static readonly Type Status = Server.GetType("Briosa.Server.Workers.WorkerExecutionStatus", true)!;
    private static readonly Type Disposition = Server.GetType("Briosa.Server.Workers.WorkerExecutionDisposition", true)!;
    private static readonly Type Contract = Server.GetType("Briosa.Server.Services.OperationOutputContract", true)!;
    private static readonly Type Successful = Server.GetType("Briosa.Server.Services.SuccessfulOperationExecution", true)!;
    private static readonly Type Exceptions = Server.GetType("Briosa.Server.Services.MpPartialSuccessExceptions", true)!;
    private static readonly Lazy<IReadOnlyList<GenuineOperation>> LazyOperations = new(Load);
    private static readonly Lazy<IReadOnlyList<object>> LazyDenials = new(LoadDenials);
    private static readonly Lazy<IReadOnlyList<object>> LazyPolicyCallerDenials = new(LoadPolicyCallerDenials);

    public static IReadOnlyList<GenuineOperation> Operations => LazyOperations.Value;

    public static GenuineOperation Operation(string operationId) =>
        Operations.Single(operation => operation.OperationId == operationId);

    /// <summary>The operations whose MP code 4 the server reports as an MP failure.</summary>
    public static IReadOnlySet<string> PartialSuccessFailures { get; } =
        ((IEnumerable<string>)Exceptions.GetProperty("OperationIds")!.GetValue(null)!).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Policy denials as the server's denial factory makes them, each with its
    /// payload: the server's own rules over every profile, every risk flag, and with
    /// and without a request option; the caller rules with the values the policy
    /// takes from the restriction this CLI sends (its profile, and a flag it denies),
    /// with and without a request option.
    /// </summary>
    public static IReadOnlyList<object> PolicyDenials => LazyDenials.Value;

    /// <summary>
    /// The caller denials the real policy makes with the restriction this CLI sends,
    /// for every operation and every reviewed request option.
    /// </summary>
    public static IReadOnlyList<object> PolicyCallerDenials => LazyPolicyCallerDenials.Value;

    /// <summary>The public detail of a denial.</summary>
    public static PolicyDenialDetails Details(object denial) =>
        (PolicyDenialDetails)denial.GetType().GetMethod("ToDetails")!.Invoke(denial, null)!;

    /// <summary>A completed MP sequence with the given code and every output retrieved.</summary>
    public static GenuineReply Complete(GenuineOperation operation, int code) =>
        Map(operation, new WorkerMpResultAvailable(code, 1,
            WorkerMpResultCodes.PermitsOutputRetrieval(code) ? Retrieved(operation) : [], null));

    /// <summary>MP code 2 with the first output's getter returning no usable value.</summary>
    public static GenuineReply FirstOutputFailed(GenuineOperation operation) =>
        Map(operation, new WorkerMpResultAvailable(2, 1,
            [Unavailable(operation.Contracts[0]), .. Retrieved(operation).Skip(1)], null));

    /// <summary>A completed worker outcome, mapped and turned into a result as the executor does.</summary>
    public static GenuineReply Map(GenuineOperation operation, WorkerMpExecutionResult execution,
        ReplaySafety? replaySafety = null) =>
        MapOutcome(operation, CompletedOutcome(execution), replaySafety ?? operation.ReplaySafety, deadline: false);

    /// <summary>
    /// A worker outcome that did not complete, mapped as the server maps it, or null when
    /// the outcome's own constructor rejects the combination.
    /// </summary>
    public static GenuineReply? MapTransport(GenuineOperation operation, string status, string disposition,
        WorkerConnectionSnapshot? connection, string diagnosticCode, bool deadline = false, object? denial = null,
        ReplaySafety? replaySafety = null)
    {
        object outcome;
        try
        {
            outcome = Activator.CreateInstance(Outcome, Enum.Parse(Status, status), Enum.Parse(Disposition, disposition),
                null, connection, diagnosticCode, 3, Guid.Empty, denial)!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is ArgumentException)
        {
            return null;
        }

        return MapOutcome(operation, outcome, replaySafety ?? operation.ReplaySafety, deadline);
    }

    /// <summary>The executor's own validation failure: a malformed restriction header, or a rejected request field.</summary>
    public static GenuineReply Validation(GenuineOperation operation, string diagnosticCode, ReplaySafety replaySafety,
        RequestValidationDetails? details) =>
        new(null, Invoke("CreateValidationFailure", operation.OperationId, diagnosticCode, 0, replaySafety, details));

    /// <summary>The executor's failure for a request that turned out invalid after its command left the executor.</summary>
    public static GenuineReply RequestViolationAfterMapping(GenuineOperation operation, ReplaySafety replaySafety) =>
        new(null, Invoke("CreateInternalFailure", operation.OperationId, replaySafety, 0,
            Enum.Parse(Disposition, "StartedOutcomeUnknown"), "request-violation-after-mapping"));

    /// <summary>The executor's failure for a successful execution whose values the result mapping rejected.</summary>
    public static GenuineReply ResultMappingFailure(GenuineOperation operation, ReplaySafety replaySafety,
        MpExecutionDetails details) =>
        new(null, Invoke("CreateResultMappingFailure", operation.OperationId, replaySafety, 3, details));

    private static GenuineReply MapOutcome(GenuineOperation operation, object outcome, ReplaySafety replaySafety,
        bool deadline)
    {
        try
        {
            var completed = Mapper.GetMethod("RequireSuccess", BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, [outcome, operation.OperationId, replaySafety, operation.ContractList, deadline])!;
            var details = (MpExecutionDetails)Successful.GetProperty("Details")!.GetValue(completed)!;
            try
            {
                return new GenuineReply((IMessage)operation.CreateResult.Invoke(null, [completed])!, null, details);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is ArgumentException or
                InvalidOperationException or OverflowException)
            {
                // The executor turns a result the mapping rejects into result-mapping-failed.
                return ResultMappingFailure(operation, replaySafety, details);
            }
        }
        catch (TargetInvocationException exception) when (exception.InnerException is RpcException rpc)
        {
            return new GenuineReply(null, rpc);
        }
    }

    private static RpcException Invoke(string method, params object?[] arguments) =>
        (RpcException)Mapper.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == method).Invoke(null, arguments)!;

    public static IReadOnlyList<WorkerMpOutputValue> Retrieved(GenuineOperation operation) =>
        operation.Contracts.Select(contract =>
            (WorkerMpOutputValue)new WorkerRetrievedOutput(contract.ArgumentName, contract.Kind, ValueFor(contract.Kind))).ToArray();

    public static WorkerMpOutputValue Unavailable(GenuineContract contract,
        WorkerUnavailableOutputReason reason = WorkerUnavailableOutputReason.None) =>
        new WorkerUnavailableOutput(contract.ArgumentName, contract.Kind, "sdk-output-retrieval-failed", reason);

    /// <summary>What the CLI makes of a reply to this operation, whose replay safety discovery reported.</summary>
    public static BridgeEnvelope Classify(GenuineOperation operation, GenuineReply reply, ReplaySafety? replaySafety = null)
    {
        var observation = reply.Result is { } result
            ? new MpCallObservation(result, StatusCode.OK, new Metadata(), false, false)
            : new MpCallObservation(null, reply.Error!.StatusCode, reply.Error.Trailers, false, false);
        return Classify(operation, observation, replaySafety);
    }

    /// <summary>What the CLI makes of a typed error with this status.</summary>
    public static BridgeEnvelope Classify(GenuineOperation operation, StatusCode status, OperationError error,
        ReplaySafety? replaySafety = null) =>
        Classify(operation, new MpCallObservation(null, status,
            new Metadata { { Wire.OperationErrorTrailer, error.ToByteArray() } }, false, false), replaySafety);

    private static BridgeEnvelope Classify(GenuineOperation operation, MpCallObservation observation,
        ReplaySafety? replaySafety)
    {
        var envelope = new BridgeEnvelope("call", BridgeOutcome.Succeeded);
        envelope.Outcome = MpOutcome.Classify(observation,
            operation.Context with { ReplaySafety = replaySafety ?? operation.ReplaySafety }, envelope);
        return envelope;
    }

    private static List<object> LoadDenials()
    {
        var denialType = Server.GetType("Briosa.Server.Security.OperationPolicyDenial", true)!;
        var vocabulary = Server.GetType("Briosa.Server.Security.OperationRiskVocabulary", true)!;
        var profiles = Server.GetType("Briosa.Server.Security.OperationAdmissionProfile", true)!;
        object Make(string factory, params object?[] arguments) =>
            denialType.GetMethod(factory, BindingFlags.Public | BindingFlags.Static)!.Invoke(null, arguments)!;
        var flags = ((System.Collections.IEnumerable)vocabulary.GetProperty("Members")!.GetValue(null)!).Cast<object>().ToArray();
        var profileNames = ((System.Collections.IEnumerable)profiles.GetProperty("All")!.GetValue(null)!).Cast<object>()
            .Select(profile => (string)profiles.GetProperty("Name")!.GetValue(profile)!).ToArray();
        var interactive = flags.Single(flag => (string)vocabulary.GetMethod("GetName", [flag.GetType()])!
            .Invoke(null, [flag])! == "interactive_ui");
        var denials = new List<object> { Make("Unreviewed"), Make("Override") };
        foreach (var option in new[] { null, "show_results_dialog" })
        {
            denials.Add(Make("Exclusive", option));
            denials.AddRange(profileNames.Select(name => Make("Profile", name, option)));
            denials.AddRange(flags.Select(flag => Make("Flag", flag, option)));
            denials.Add(Make("CallerProfile", "read-only", option));
            denials.Add(Make("CallerFlag", interactive, option));
        }

        return denials;
    }

    private static List<object> LoadPolicyCallerDenials()
    {
        // The caller rules, from the real policy with the restriction this CLI sends.
        var api = Server.GetType("Briosa.Server.Operations.SpatialAnalyzerApi", true)!;
        var operations = api.GetProperty("Operations", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);
        var policyType = Server.GetType("Briosa.Server.Security.OperationPolicy", true)!;
        var restrictionType = Server.GetType("Briosa.Server.Security.OperationCallerRestriction", true)!;
        var restriction = restrictionType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [Wire.ReadOnlyRestriction])!;
        // The server admits everything it can, so the caller's restriction decides.
        var vocabulary = Server.GetType("Briosa.Server.Security.OperationRiskVocabulary", true)!;
        var settings = new Dictionary<string, string?> { ["Briosa:Security:Operations:Profile"] = "full" };
        foreach (var flag in ((System.Collections.IEnumerable)vocabulary.GetProperty("Members")!.GetValue(null)!).Cast<object>())
        {
            settings["Briosa:Security:Operations:Flags:" + vocabulary.GetMethod("GetName", [flag.GetType()])!.Invoke(null, [flag])] = "allow";
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var policy = policyType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [configuration, operations, null, null])!;
        var evaluate = policyType.GetMethod("Evaluate", [typeof(string), restrictionType])!;
        var evaluateOptions = policyType.GetMethod("EvaluateRequestOptions")!;
        var callers = new Dictionary<string, object>(StringComparer.Ordinal);
        void Keep(object decision)
        {
            if (decision.GetType().GetProperty("Denial")!.GetValue(decision) is { } denial &&
                Details(denial) is { Rule: PolicyDenialRule.CallerFlag or PolicyDenialRule.CallerProfile } details)
            {
                callers.TryAdd(Convert.ToBase64String(details.ToByteArray()), denial);
            }
        }

        foreach (var capability in ServerProjection.AllCapabilities.Operations)
        {
            Keep(evaluate.Invoke(policy, [capability.OperationId, restriction])!);
            foreach (var entry in (System.Collections.IEnumerable)evaluateOptions.Invoke(policy, [capability.OperationId, restriction])!)
            {
                Keep(entry.GetType().GetField("Item2")!.GetValue(entry)!);
            }
        }

        return [.. callers.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => entry.Value)];
    }

    private static object CompletedOutcome(WorkerMpExecutionResult execution)
    {
        foreach (var name in new[] { "NotStarted", "StartedOutcomeUnknown", "Completed" })
        {
            try
            {
                return Activator.CreateInstance(Outcome, Enum.Parse(Status, "Completed"), Enum.Parse(Disposition, name),
                    execution, null, "completed", 3, Guid.Empty, null)!;
            }
            catch (TargetInvocationException exception) when (exception.InnerException is ArgumentException)
            {
            }
        }

        throw new InvalidOperationException("No disposition fits the execution.");
    }

    private static List<GenuineOperation> Load()
    {
        var api = Server.GetType("Briosa.Server.Operations.SpatialAnalyzerApi", true)!;
        var registry = ((IEnumerable)api.GetProperty("Operations")!.GetValue(null)!).Cast<object>().ToArray();
        var owners = new Dictionary<object, Type>(ReferenceEqualityComparer.Instance);
        var creators = new Dictionary<Type, List<MethodInfo>>();
        foreach (var type in Server.GetTypes().Where(type => !type.ContainsGenericParameters))
        {
            if (type.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static) is { } descriptor &&
                descriptor.PropertyType.Name == "OperationDescriptor")
            {
                owners[descriptor.GetValue(null)!] = type;
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.GetParameters() is [{ } parameter] && parameter.ParameterType == Successful &&
                    typeof(IMessage).IsAssignableFrom(method.ReturnType))
                {
                    if (!creators.TryGetValue(method.ReturnType, out var list))
                    {
                        creators[method.ReturnType] = list = [];
                    }

                    list.Add(method);
                }
            }
        }

        var capabilities = ServerProjection.AllCapabilities.Operations.ToDictionary(capability => capability.OperationId);
        var operations = new List<GenuineOperation>();
        foreach (var descriptor in registry)
        {
            var id = (string)descriptor.GetType().GetProperty("OperationId")!.GetValue(descriptor)!;
            var path = (string)descriptor.GetType().GetProperty("FullyQualifiedMethod")!.GetValue(descriptor)!;
            var safety = (ReplaySafety)descriptor.GetType().GetProperty("ReplaySafety")!.GetValue(descriptor)!;
            var owner = owners[descriptor];
            var method = ProtocolCatalog.Instance.FindMethod(path)!;
            // Operations without a declared contract list pass none; every one of them has no outputs.
            var contracts = owner.GetProperty("OutputContracts", BindingFlags.Public | BindingFlags.Static) is { } property
                ? ((IEnumerable)property.GetValue(null)!).Cast<object>().ToArray()
                : [];
            var list = Array.CreateInstance(Contract, contracts.Length);
            for (var index = 0; index < contracts.Length; index++)
            {
                list.SetValue(contracts[index], index);
            }

            var candidates = creators[method.OutputType.ClrType];
            var create = candidates.Count == 1 ? candidates[0] : candidates.Single(candidate => candidate.DeclaringType == owner);
            operations.Add(new GenuineOperation(id, path, safety, list,
                contracts.Select(contract => new GenuineContract(
                    (string)Contract.GetProperty("FieldName")!.GetValue(contract)!,
                    (string)Contract.GetProperty("ArgumentName")!.GetValue(contract)!,
                    (WorkerMpValueKind)Contract.GetProperty("Kind")!.GetValue(contract)!)).ToArray(),
                create, method.OutputType, BridgeTiers.Classify(capabilities[id])));
        }

        return operations;
    }

    private static WorkerMpValue ValueFor(WorkerMpValueKind kind)
    {
        var limit = new WorkerToleranceLimit(Enabled: true, Value: 0.5);
        var point = new WorkerPointNameValue("A", "G", "P1");
        var objectName = new WorkerCollectionObjectNameValue("A", "O1", WorkerObjectTypeValue.PointGroup);
        var itemName = new WorkerCollectionItemNameValue("A", "R1", WorkerItemTypeValue.Any);
        var transform = new WorkerTransformValue([1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]);
        return kind switch
        {
            WorkerMpValueKind.Logical => new WorkerBooleanValue(true),
            WorkerMpValueKind.WholeNumber => new WorkerIntegerValue(3),
            WorkerMpValueKind.FloatingPoint => new WorkerDoubleValue(1.25),
            WorkerMpValueKind.Text or WorkerMpValueKind.ChartName or WorkerMpValueKind.CloudName or
                WorkerMpValueKind.CollectionName or WorkerMpValueKind.FrameName or WorkerMpValueKind.VectorGroupName or
                WorkerMpValueKind.ViewName => new WorkerTextValue("A"),
            WorkerMpValueKind.DoubleArray => new WorkerDoubleArrayValue([1.5, 2.5]),
            WorkerMpValueKind.EditText or WorkerMpValueKind.StringList => new WorkerStringListValue(["a", "b"]),
            WorkerMpValueKind.Transform => transform,
            WorkerMpValueKind.WorldTransform => new WorkerWorldTransformValue(transform, 1),
            WorkerMpValueKind.FileReference => new WorkerFileReferenceValue(@"C:\data\result.txt", EmbeddedFile: false),
            WorkerMpValueKind.FitConstraintScalarOptions => new WorkerFitConstraintScalarOptionsValue(limit, limit),
            WorkerMpValueKind.ToleranceScalarOptions => new WorkerToleranceScalarOptionsValue(limit, limit),
            WorkerMpValueKind.ToleranceVectorOptions => new WorkerToleranceVectorOptionsValue(limit, limit, limit, limit,
                limit, limit, limit, limit),
            WorkerMpValueKind.PointName => point,
            WorkerMpValueKind.Vector => new WorkerVectorValue(1, 2, 3),
            WorkerMpValueKind.CollectionInstrumentId => new WorkerCollectionInstrumentIdValue("A", 1),
            WorkerMpValueKind.CollectionInstrumentIdList => new WorkerCollectionInstrumentIdListValue(
                [new WorkerCollectionInstrumentIdValue("A", 1)]),
            WorkerMpValueKind.CollectionMachineId => new WorkerCollectionMachineIdValue("A", 1),
            WorkerMpValueKind.CollectionItemName => itemName,
            WorkerMpValueKind.CollectionItemNameList => new WorkerCollectionItemNameListValue([itemName]),
            WorkerMpValueKind.CollectionObjectName => objectName,
            WorkerMpValueKind.CollectionObjectNameList => new WorkerCollectionObjectNameListValue([objectName]),
            WorkerMpValueKind.CollectionGroupNameList => new WorkerCollectionGroupNameListValue(
                [new WorkerCollectionGroupNameValue("A", "G")]),
            WorkerMpValueKind.CollectionVectorGroupName => new WorkerCollectionVectorGroupNameValue("A", "V"),
            WorkerMpValueKind.CollectionVectorGroupNameList => new WorkerCollectionVectorGroupNameListValue(
                [new WorkerCollectionVectorGroupNameValue("A", "V")]),
            WorkerMpValueKind.PointNameList => new WorkerPointNameListValue([point]),
            WorkerMpValueKind.VectorNameList => new WorkerVectorNameListValue([new WorkerVectorNameValue("A", "G", "V1")]),
            _ => throw new InvalidOperationException($"No genuine value for output kind {kind}.")
        };
    }
}

internal sealed record GenuineContract(string FieldName, string ArgumentName, WorkerMpValueKind Kind);

internal sealed record GenuineOperation(
    string OperationId,
    string FullyQualifiedMethod,
    ReplaySafety ReplaySafety,
    Array ContractList,
    IReadOnlyList<GenuineContract> Contracts,
    MethodInfo CreateResult,
    Google.Protobuf.Reflection.MessageDescriptor Result,
    BridgeTier Tier)
{
    // As the engine sets it: code 4 can be a failure outside the read-only tier.
    public MpCallContext Context => new(OperationId, FullyQualifiedMethod, Wire.ReadOnlyRestriction, Result,
        ReplaySafety, CodeFourMayBeFailure: Tier != BridgeTier.ReadOnly);
}

/// <summary>A reply: a result, or the server's exception. A result keeps the MP details the mapper built for it.</summary>
internal sealed record GenuineReply(IMessage? Result, RpcException? Error, MpExecutionDetails? Details = null);
