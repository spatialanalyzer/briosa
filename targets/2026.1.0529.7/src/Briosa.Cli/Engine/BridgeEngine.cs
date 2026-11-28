using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Grpc.Core;

namespace Briosa.Cli.Engine;

/// <summary>
/// Where result files go, and the clock that names them. Spill files go in the
/// directory <see cref="SpillComponents"/> names below <see cref="SpillBase"/>; every
/// component below the base is managed and verified by the CLI.
/// </summary>
internal sealed record BridgeEnvironment(
    IResultFileSystem Files,
    string SpillBase,
    IReadOnlyList<string> SpillComponents,
    TimeProvider Time)
{
    public static BridgeEnvironment Default { get; } = new(ResultFileSystem.Instance, ResultFileSystem.DefaultSpillBase,
        ResultFileSystem.DefaultSpillComponents, TimeProvider.System);
}

/// <summary>A search over discovery metadata.</summary>
internal sealed record OperationQuery(
    string? Text,
    string? Category,
    OperationEffect? Effect,
    IReadOnlyList<OperationRiskFlag> RiskFlags,
    BridgeTier? Tier,
    bool IncludeDenied,
    bool ValidatedOnly,
    int Offset,
    int Limit)
{
    public const int DefaultLimit = 25;
    public const int MaximumLimit = 100;
}

/// <summary>One read-only call. Exactly one of the two input sources is set.</summary>
internal sealed record CallRequest(
    string OperationId,
    string? InputText,
    string? InputFile,
    string? OutputPath,
    int MaximumInlineBytes,
    TimeSpan? Deadline);

/// <summary>
/// The shared engine behind the JSON CLI and, in a later slice, the stdio MCP
/// server. It attaches to an existing loopback server, verifies it, and observes:
/// it never starts, stops, connects, or recovers the server, the SDK, or
/// SpatialAnalyzer, and the only RPCs it sends are two discovery reads and the
/// one MP call a <c>call</c> asks for. Every method returns one envelope and never
/// throws for an outcome.
/// </summary>
internal sealed partial class BridgeEngine
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private const string MetrologyGuidance =
        "Coordinates, distances and other results are expressed in SpatialAnalyzer's live working frame, active " +
        "collection, and units, which can change between calls without any change on the server. Inspect them " +
        "with explicit read-only calls, for example utility_operations.get_working_frame_properties, " +
        "construction_operations.get_active_collection_name and utility_operations.get_active_units. The CLI " +
        "never makes hidden preliminary MP calls.";

    private readonly LoopbackEndpoint _endpoint;
    private readonly ResultDelivery _delivery;

    /// <summary>One engine serves one attached loopback endpoint.</summary>
    public BridgeEngine(LoopbackEndpoint endpoint, BridgeEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _delivery = new ResultDelivery(environment.Files, environment.SpillBase, environment.SpillComponents,
            environment.Time);
    }

    public static bool IsOperationId(string? text) => text is { Length: <= 128 } && OperationIdPattern().IsMatch(text);

    public async Task<BridgeEnvelope> StatusAsync(CancellationToken cancellationToken)
    {
        var envelope = new BridgeEnvelope("status", BridgeOutcome.Succeeded);
        using var connection = new BriosaConnection(_endpoint);
        var assessment = await AttachAsync(envelope, connection, cancellationToken).ConfigureAwait(false);
        if (assessment is null)
        {
            return envelope;
        }

        var info = assessment.Info;
        var data = new JsonObject
        {
            ["server_info"] = ProtocolJson.ToNode(info),
            ["admission_profile"] = null,
            ["policy_fingerprint"] = null,
            ["call_available"] = assessment.SupportsExecution
        };
        envelope.Data = data;
        if (info.ConfiguredSpatialAnalyzerInteractionMode != ConfiguredSpatialAnalyzerInteractionMode.Silent)
        {
            envelope.Warn("interaction-mode-not-silent",
                "The server is not configured to set SpatialAnalyzer's Silent interaction mode (see " +
                "data.server_info.configured_spatial_analyzer_interaction_mode). An operator dialog can block an MP " +
                "call until someone dismisses it.");
        }

        if (!info.ReadyForMp)
        {
            envelope.Warn("not-ready-for-mp",
                "The server reports that it is not ready for MP requests. This CLI never connects or starts " +
                "anything; use the Control Center or the application that owns the server.");
        }

        envelope.Guidance.Add(MetrologyGuidance);
        var capabilities = await ListAsync(envelope, connection, includeDenied: false, cancellationToken)
            .ConfigureAwait(false);
        if (capabilities is not null)
        {
            data["admission_profile"] = capabilities.AdmissionProfile;
            data["policy_fingerprint"] = capabilities.PolicyFingerprint;
        }

        return envelope;
    }

    public async Task<BridgeEnvelope> CategoriesAsync(CancellationToken cancellationToken)
    {
        var envelope = new BridgeEnvelope("ops categories", BridgeOutcome.Succeeded);
        using var connection = new BriosaConnection(_endpoint);
        if (await AttachAsync(envelope, connection, cancellationToken).ConfigureAwait(false) is null ||
            await ListAsync(envelope, connection, includeDenied: true, cancellationToken).ConfigureAwait(false) is not
                { } capabilities)
        {
            return envelope;
        }

        var categories = new JsonArray();
        foreach (var group in capabilities.Operations.GroupBy(CategoryOf, StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var tiers = new JsonObject();
            foreach (var tier in BridgeTiers.All)
            {
                tiers[BridgeTiers.Name(tier)] = group.Count(operation => BridgeTiers.Classify(operation) == tier);
            }

            categories.Add(new JsonObject
            {
                ["category"] = group.Key,
                ["grpc_services"] = new JsonArray(group.Select(operation => operation.GrpcService)
                    .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).Select(name => (JsonNode)name).ToArray()),
                ["operations"] = group.Count(),
                ["admitted"] = group.Count(operation => operation.Admission == OperationAdmission.Admitted),
                ["denied"] = group.Count(operation => operation.Admission != OperationAdmission.Admitted),
                ["tiers"] = tiers
            });
        }

        envelope.Data = new JsonObject
        {
            ["admission_profile"] = capabilities.AdmissionProfile,
            ["policy_fingerprint"] = capabilities.PolicyFingerprint,
            ["categories"] = categories
        };
        return envelope;
    }

    public async Task<BridgeEnvelope> SearchAsync(OperationQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var envelope = new BridgeEnvelope("ops search", BridgeOutcome.Succeeded);
        using var connection = new BriosaConnection(_endpoint);
        if (await AttachAsync(envelope, connection, cancellationToken).ConfigureAwait(false) is not { } assessment ||
            await ListAsync(envelope, connection, query.IncludeDenied, cancellationToken).ConfigureAwait(false) is not
                { } capabilities)
        {
            return envelope;
        }

        var matches = capabilities.Operations.Where(operation => Matches(operation, query)).ToArray();
        var page = matches.Skip(query.Offset).Take(query.Limit).ToArray();
        var next = query.Offset + page.Length;
        envelope.Data = new JsonObject
        {
            ["admission_profile"] = capabilities.AdmissionProfile,
            ["policy_fingerprint"] = capabilities.PolicyFingerprint,
            ["include_denied"] = query.IncludeDenied,
            ["total_matches"] = matches.Length,
            ["offset"] = query.Offset,
            ["limit"] = query.Limit,
            ["next_offset"] = next < matches.Length ? next : null,
            ["operations"] = new JsonArray(page.Select(operation => (JsonNode)OperationSummary(operation)).ToArray())
        };
        if (!assessment.ReportsMpStep)
        {
            envelope.Warn("mp-step-not-reported",
                "This server does not report mp_step (Server 0.10.2 and later do), so a search cannot match MP " +
                "command names.");
        }

        return envelope;
    }

    public async Task<BridgeEnvelope> DescribeAsync(string operationId, CancellationToken cancellationToken)
    {
        var envelope = new BridgeEnvelope("ops describe", BridgeOutcome.Succeeded);
        if (!IsOperationId(operationId))
        {
            return RejectLocally(envelope, "operation-id-invalid");
        }

        using var connection = new BriosaConnection(_endpoint);
        if (await AttachAsync(envelope, connection, cancellationToken).ConfigureAwait(false) is not { } assessment ||
            await ListAsync(envelope, connection, includeDenied: true, cancellationToken).ConfigureAwait(false) is not
                { } capabilities)
        {
            return envelope;
        }

        var capability = capabilities.Operations.SingleOrDefault(operation =>
            string.Equals(operation.OperationId, operationId, StringComparison.Ordinal));
        if (capability is null)
        {
            return RejectLocally(envelope, "operation-unknown");
        }

        var method = FindMethod(capability);
        if (method is null)
        {
            return RejectLocally(envelope, "operation-schema-unavailable");
        }

        var tier = BridgeTiers.Classify(capability);
        var operation = OperationSummary(capability);
        operation["tier_description"] = BridgeTiers.Description(tier);
        operation["served_by_call"] = BridgeTiers.ServedByCall(tier) && assessment.SupportsExecution;
        var data = new JsonObject
        {
            ["operation"] = operation,
            ["request_options"] = RequestOptions(capability, method, envelope),
            ["request_message"] = method.InputType.FullName,
            ["result_message"] = method.OutputType.FullName,
            ["requiredness"] = RequestSchema.RequirednessStatement,
            ["input_rules"] = RequestSchema.InputRules(),
            ["request_schema"] = RequestSchema.Build(method.InputType)
        };
        envelope.Data = data;
        if (capability.ValidationStatus != OperationValidationStatus.NoRecordedGap)
        {
            envelope.Warn("validation-gap-recorded",
                "This operation has a recorded validation gap (see data.operation.validation_status). It is " +
                "informational and does not affect admission.");
        }

        return envelope;
    }

    public async Task<BridgeEnvelope> CallAsync(CallRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var envelope = new BridgeEnvelope("call", BridgeOutcome.Succeeded) { Execution = MpOutcome.NotSent() };
        if (!IsOperationId(request.OperationId))
        {
            return RejectLocally(envelope, "operation-id-invalid");
        }

        var input = ReadInput(request, out var inputCode);
        if (input is null)
        {
            return RejectLocally(envelope, inputCode!);
        }

        // The output file is created before any RPC, so a path that cannot be written
        // is refused while nothing has been sent at all.
        ResultFile? output = null;
        if (request.OutputPath is not null)
        {
            output = _delivery.Reserve(request.OutputPath, out var outputCode);
            if (output is null)
            {
                return RejectLocally(envelope, outputCode!);
            }
        }

        try
        {
            return await AttachAndSendAsync(envelope, request, input, output, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // A reservation that never received a result is removed through its own
            // handle; a written result file has already been closed and stays.
            ResultDelivery.Release(output);
        }
    }

    private async Task<BridgeEnvelope> AttachAndSendAsync(BridgeEnvelope envelope, CallRequest request, byte[] input,
        ResultFile? output, CancellationToken cancellationToken)
    {
        using var connection = new BriosaConnection(_endpoint);
        if (await AttachAsync(envelope, connection, cancellationToken).ConfigureAwait(false) is not { } assessment)
        {
            return envelope;
        }

        if (!assessment.SupportsExecution)
        {
            // An older server ignores the caller restriction, so the call would run
            // under the server's configuration alone. It is never sent unrestricted.
            envelope.Outcome = BridgeOutcome.ServerIncompatible;
            envelope.Failure = Failure("cli", "server-revision-too-old-for-call");
            return envelope;
        }

        if (await ListAsync(envelope, connection, includeDenied: true, cancellationToken).ConfigureAwait(false) is not
            { } capabilities)
        {
            return envelope;
        }

        // The listing was checked for duplicate IDs, so at most one row matches.
        var capability = capabilities.Operations.SingleOrDefault(operation =>
            string.Equals(operation.OperationId, request.OperationId, StringComparison.Ordinal));
        if (capability is null)
        {
            return RejectLocally(envelope, "operation-unknown");
        }

        var tier = BridgeTiers.Classify(capability);
        // Every call data member, always in this order; delivery only fills values.
        var data = new JsonObject
        {
            ["operation"] = OperationSummary(capability),
            ["result_delivery"] = "none",
            ["result_message"] = null,
            ["result_json_bytes"] = null,
            ["inline_cap_bytes"] = request.MaximumInlineBytes,
            ["result"] = null,
            ["result_file"] = null,
            ["result_summary"] = null,
            ["result_delivery_error"] = null
        };
        envelope.Data = data;
        if (!BridgeTiers.ServedByCall(tier))
        {
            return RejectLocally(envelope, tier == BridgeTier.Excluded ? "operation-tier-excluded" : "operation-tier-not-served");
        }

        var method = FindMethod(capability);
        if (method is null)
        {
            return RejectLocally(envelope, "operation-schema-unavailable");
        }

        var parsed = StrictJsonInput.Parse(input, method.InputType);
        if (parsed.Rejection is { } rejection)
        {
            RejectLocally(envelope, rejection.Code);
            envelope.Failure!["input"] = new JsonObject
            {
                ["field_path"] = rejection.FieldPath,
                ["byte_offset"] = rejection.ByteOffset,
                ["line"] = rejection.Line,
                ["column"] = rejection.Column,
                ["suggested_field_name"] = rejection.SuggestedFieldName
            };
            return envelope;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return RejectLocally(envelope, "client-cancelled-before-send");
        }

        // The server reports MP code 4 as an MP failure only for its reviewed exceptions,
        // and none of them is in the read-only tier (pinned by
        // ReviewRoundTwoTests.NoOperationWhoseCodeFourIsAFailureIsServedByCall). A tier
        // that serves mutating operations must keep this true for them.
        var call = new MpCallContext(request.OperationId, capability.FullyQualifiedMethod,
            BridgeContract.ReadOnlyCallerRestriction, method.OutputType, capability.ReplaySafety,
            CodeFourMayBeFailure: tier != BridgeTier.ReadOnly);
        return await SendAsync(envelope, connection, call, method, parsed.Message!, request, output, data,
            cancellationToken).ConfigureAwait(false);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Once the request is sent, every failure must still produce an envelope that says so.")]
    private async Task<BridgeEnvelope> SendAsync(BridgeEnvelope envelope, BriosaConnection connection,
        MpCallContext call, Google.Protobuf.Reflection.MethodDescriptor method, Google.Protobuf.IMessage message,
        CallRequest request, ResultFile? output, JsonObject data, CancellationToken cancellationToken)
    {
        MpCallObservation observation;
        try
        {
            DateTime? deadline = request.Deadline is { } after ? DateTime.UtcNow.Add(after) : null;
            observation = await MpInvoker.InvokeAsync(connection.Invoker, method, message,
                call.CallerRestriction, deadline, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            envelope.Outcome = MpOutcome.Classify(
                new MpCallObservation(null, StatusCode.Unknown, null, cancellationToken.IsCancellationRequested, false),
                call, envelope);
            envelope.Failure!["diagnostic_code"] = "client-fault-after-send";
            return envelope;
        }

        observation = observation with
        {
            ResendBlocked = connection.ResendBlocked,
            ReconnectRefused = connection.ReconnectRefused
        };
        var outcome = MpOutcome.Classify(observation, call, envelope);
        if (observation.Response is null || observation.ResendBlocked)
        {
            envelope.Outcome = outcome;
            return envelope;
        }

        // Completion is established only by the call's own consistent evidence.
        var completionEstablished = outcome is BridgeOutcome.Succeeded or BridgeOutcome.PartialSuccess or
            BridgeOutcome.OutputsIncomplete;
        bool delivered;
        try
        {
            delivered = _delivery.Deliver(observation.Response, call.OperationId, output, request.MaximumInlineBytes,
                completionEstablished, envelope, data);
        }
        catch (Exception)
        {
            output?.Dispose();
            data["result_delivery"] = "lost";
            data["result_delivery_error"] = "result-format-failed";
            delivered = false;
        }

        if (!delivered && completionEstablished)
        {
            // Exit code 12 asserts that the operation completed, so it is used only
            // when the evidence established that. Otherwise the unestablished outcome
            // stands, and data.result_delivery_error records the failed delivery.
            envelope.Failure ??= Failure("cli", data["result_delivery_error"]?.GetValue<string>() ?? "output-write-failed");
            envelope.Guidance.Add("The operation completed. Its result could not be delivered as requested; do not " +
                "run it again to fetch the result unless you have confirmed that doing so is safe.");
            outcome = BridgeOutcome.OutputWriteFailed;
        }

        envelope.Outcome = outcome;
        return envelope;
    }

    private static byte[]? ReadInput(CallRequest request, out string? code)
    {
        code = null;
        if (request.InputText is { } text)
        {
            try
            {
                // Strict encoding: a lone surrogate is refused, never replaced.
                if (StrictUtf8.GetByteCount(text) > StrictJsonInput.MaximumInputBytes)
                {
                    code = InputRejectionCodes.TooLarge;
                    return null;
                }

                return StrictUtf8.GetBytes(text);
            }
            catch (EncoderFallbackException)
            {
                code = InputRejectionCodes.InvalidString;
                return null;
            }
        }

        try
        {
            using var stream = new FileStream(request.InputFile!, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > StrictJsonInput.MaximumInputBytes)
            {
                code = InputRejectionCodes.TooLarge;
                return null;
            }

            var buffer = new byte[StrictJsonInput.MaximumInputBytes + 1];
            var total = 0;
            int read;
            while (total < buffer.Length && (read = stream.Read(buffer, total, buffer.Length - total)) > 0)
            {
                total += read;
            }

            if (total > StrictJsonInput.MaximumInputBytes)
            {
                code = InputRejectionCodes.TooLarge;
                return null;
            }

            return buffer[..total];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            ArgumentException or NotSupportedException)
        {
            code = "input-file-unreadable";
            return null;
        }
    }

    private static async Task<ServerAssessment?> AttachAsync(BridgeEnvelope envelope, BriosaConnection connection,
        CancellationToken cancellationToken)
    {
        envelope.Endpoint = connection.Endpoint.Text;
        GetServerInfoResponse info;
        try
        {
            info = await connection.GetServerInfoAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (RpcException exception)
        {
            DiscoveryFailed(envelope, connection, "/briosa.DiscoveryService/GetServerInfo", exception.StatusCode,
                connection.ReceiveLimitExceeded, cancellationToken);
            return null;
        }

        envelope.Transport = new JsonObject
        {
            ["method"] = "/briosa.DiscoveryService/GetServerInfo",
            ["status"] = "OK"
        };
        var assessment = ServerAssessment.Assess(info);
        envelope.Server = assessment.ToJson();
        if (!assessment.Verified)
        {
            envelope.Outcome = BridgeOutcome.ServerIncompatible;
            envelope.Failure = Failure("cli", assessment.Problems[0]);
            return null;
        }

        assessment.AddFeatureWarnings(envelope);
        return assessment;
    }

    private static async Task<ListCapabilitiesResponse?> ListAsync(BridgeEnvelope envelope, BriosaConnection connection,
        bool includeDenied, CancellationToken cancellationToken)
    {
        const string Method = "/briosa.DiscoveryService/ListCapabilities";
        ListCapabilitiesResponse capabilities;
        try
        {
            capabilities = await connection.ListCapabilitiesAsync(includeDenied, cancellationToken).ConfigureAwait(false);
        }
        catch (RpcException exception)
        {
            DiscoveryFailed(envelope, connection, Method, exception.StatusCode, connection.ReceiveLimitExceeded,
                cancellationToken);
            return null;
        }

        envelope.Transport = new JsonObject { ["method"] = Method, ["status"] = "OK" };
        if (!string.Equals(capabilities.SpatialAnalyzerTarget, BridgeContract.SpatialAnalyzerTarget, StringComparison.Ordinal) ||
            !string.Equals(capabilities.ProtocolPackage, BridgeContract.ProtocolPackage, StringComparison.Ordinal))
        {
            envelope.Outcome = BridgeOutcome.ServerIncompatible;
            envelope.Failure = Failure("cli", "capabilities-target-mismatch");
            return null;
        }

        if (!IsConsistent(capabilities))
        {
            // Tiers, counts, and the method a call dispatches all come from this
            // listing, so an inconsistent one is refused whole, never resolved.
            envelope.Outcome = BridgeOutcome.ServerIncompatible;
            envelope.Failure = Failure("cli", "capabilities-inconsistent");
            return null;
        }

        return capabilities;
    }

    /// <summary>
    /// Every row has a well-formed operation ID that no other row has, and names one
    /// method, as its service and RPC, that no other row names. The ID must also be
    /// the one <see cref="NamesMethod"/> binds to that method, so a listing can never
    /// send a call for one operation to another operation's method.
    /// </summary>
    internal static bool IsConsistent(ListCapabilitiesResponse capabilities)
    {
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        var methods = new HashSet<string>(StringComparer.Ordinal);
        return capabilities.Operations.All(operation =>
            IsOperationId(operation.OperationId) &&
            identifiers.Add(operation.OperationId) &&
            string.Equals(operation.FullyQualifiedMethod, "/" + operation.GrpcService + "/" + operation.Rpc,
                StringComparison.Ordinal) &&
            NamesMethod(operation.OperationId, operation.GrpcService, operation.Rpc) &&
            methods.Add(operation.FullyQualifiedMethod));
    }

    /// <summary>
    /// Whether an operation ID names this method. On this target every operation ID
    /// is its service name and RPC name in snake case: with the underscores removed,
    /// <c>category.operation</c> equals <c>Service.Rpc</c>, ignoring ASCII case, where
    /// the service is named without the protocol package. For example
    /// <c>file_operations.get_working_directory</c> names
    /// <c>briosa.FileOperations/GetWorkingDirectory</c>.
    /// </summary>
    internal static bool NamesMethod(string operationId, string grpcService, string rpc)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(grpcService);
        var prefix = BridgeContract.ProtocolPackage + ".";
        return grpcService.StartsWith(prefix, StringComparison.Ordinal) &&
            string.Equals(operationId.Replace("_", "", StringComparison.Ordinal),
                grpcService[prefix.Length..] + "." + rpc, StringComparison.OrdinalIgnoreCase);
    }

    private static void DiscoveryFailed(BridgeEnvelope envelope, BriosaConnection connection, string method,
        StatusCode status, bool receiveLimitExceeded, CancellationToken cancellationToken)
    {
        envelope.Transport = new JsonObject { ["method"] = method, ["status"] = ProtocolJson.StatusName(status) };
        if (cancellationToken.IsCancellationRequested)
        {
            envelope.Outcome = BridgeOutcome.RejectedLocally;
            envelope.Failure = Failure("cli", "client-cancelled-before-send");
            return;
        }

        if (status == StatusCode.ResourceExhausted && receiveLimitExceeded)
        {
            // The server answered with a message whose declared length is larger
            // than this CLI accepts. A RESOURCE_EXHAUSTED status without that
            // evidence is reported below as the status it is.
            envelope.Outcome = BridgeOutcome.ServerIncompatible;
            envelope.Failure = Failure("cli", "discovery-response-too-large");
            envelope.Guidance.Add("The server answered, but its discovery reply is larger than this CLI accepts. " +
                "No operation was sent.");
            return;
        }

        if (status is not (StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.Internal or
            StatusCode.Unknown or StatusCode.Cancelled))
        {
            // The server answered the discovery RPC with a status this CLI cannot use.
            envelope.Outcome = BridgeOutcome.ServerIncompatible;
            envelope.Failure = Failure("transport", ProtocolJson.TransportCode(status));
            return;
        }

        envelope.Outcome = BridgeOutcome.ServerUnreachable;
        envelope.Failure = Failure("transport",
            connection.ReconnectRefused ? MpOutcome.ServerConnectionReplaced : ProtocolJson.TransportCode(status));
        envelope.Guidance.Add("This CLI attaches to a server that is already running and never starts one. Start " +
            "the server with the Control Center or the application that owns it, then try again.");
    }

    private static BridgeEnvelope RejectLocally(BridgeEnvelope envelope, string code)
    {
        envelope.Outcome = BridgeOutcome.RejectedLocally;
        envelope.Failure = Failure("cli", code);
        return envelope;
    }

    internal static JsonObject Failure(string source, string code) => new()
    {
        ["source"] = source,
        ["kind"] = null,
        ["diagnostic_code"] = code,
        ["policy_denial"] = null,
        ["request_validation"] = null,
        ["input"] = null
    };

    private static Google.Protobuf.Reflection.MethodDescriptor? FindMethod(OperationCapability capability)
    {
        var method = ProtocolCatalog.Instance.FindMethod(capability.FullyQualifiedMethod);
        return method is not null &&
            string.Equals(method.Service.FullName, capability.GrpcService, StringComparison.Ordinal) &&
            string.Equals(method.Name, capability.Rpc, StringComparison.Ordinal) &&
            !method.IsClientStreaming && !method.IsServerStreaming &&
            ProtocolCatalog.FindExecutionField(method.OutputType) is not null
                ? method
                : null;
    }

    internal static string CategoryOf(OperationCapability capability)
    {
        var separator = capability.OperationId.IndexOf('.', StringComparison.Ordinal);
        return separator > 0 ? capability.OperationId[..separator] : capability.OperationId;
    }

    internal static JsonObject OperationSummary(OperationCapability capability) => new()
    {
        ["operation_id"] = capability.OperationId,
        ["mp_step"] = capability.MpStep.Length == 0 ? null : capability.MpStep,
        ["category"] = CategoryOf(capability),
        ["grpc_service"] = capability.GrpcService,
        ["rpc"] = capability.Rpc,
        ["fully_qualified_method"] = capability.FullyQualifiedMethod,
        ["effect"] = ProtocolJson.EnumNode(capability.Effect),
        ["risk_flags"] = ProtocolJson.RiskFlags(capability.RiskFlags),
        ["execution_scope"] = ProtocolJson.EnumNode(capability.ExecutionScope),
        ["replay_safety"] = ProtocolJson.EnumNode(capability.ReplaySafety),
        ["duration_class"] = ProtocolJson.EnumNode(capability.DurationClass),
        ["validation_status"] = ProtocolJson.EnumNode(capability.ValidationStatus),
        ["admission"] = ProtocolJson.EnumNode(capability.Admission),
        ["policy_denial"] = ProtocolJson.Optional(capability.PolicyDenial),
        ["tier"] = BridgeTiers.Name(BridgeTiers.Classify(capability))
    };

    private static JsonObject RequestOptions(OperationCapability capability,
        Google.Protobuf.Reflection.MethodDescriptor method, BridgeEnvelope envelope)
    {
        if (capability.RequestOptions is null)
        {
            envelope.Warn("request-options-not-described",
                "This server does not describe request options (Server 0.10.1 and later do). A request field can " +
                "still turn on operator UI or leave work running, and the server still decides each request.");
            return new JsonObject { ["described"] = false, ["options"] = null };
        }

        var options = new JsonArray();
        var activatesWhenOmitted = false;
        foreach (var option in capability.RequestOptions.Options)
        {
            var node = (JsonObject)ProtocolJson.ToNode(option);
            var omissionActivates = option.Omission == OperationRequestOptionOmission.Activates;
            node["activates_when_omitted"] = omissionActivates;
            node["field_in_request_schema"] = method.InputType.FindFieldByName(option.FieldName) is not null;
            activatesWhenOmitted |= omissionActivates;
            options.Add(node);
        }

        if (activatesWhenOmitted)
        {
            envelope.Warn("option-activates-when-omitted",
                "Omitting at least one request field of this operation turns on its effect, for example operator UI. " +
                "See data.request_options.options[].activates_when_omitted.");
        }

        return new JsonObject { ["described"] = true, ["options"] = options };
    }

    private static bool Matches(OperationCapability operation, OperationQuery query)
    {
        if (query.Text is { } text &&
            !(operation.OperationId.Contains(text, StringComparison.OrdinalIgnoreCase) ||
              operation.MpStep.Contains(text, StringComparison.OrdinalIgnoreCase) ||
              operation.Rpc.Contains(text, StringComparison.OrdinalIgnoreCase) ||
              operation.GrpcService.Contains(text, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return (query.Category is null || string.Equals(CategoryOf(operation), query.Category, StringComparison.Ordinal)) &&
            (query.Effect is null || operation.Effect == query.Effect) &&
            query.RiskFlags.All(flag => operation.RiskFlags.Contains(flag)) &&
            (query.Tier is null || BridgeTiers.Classify(operation) == query.Tier) &&
            (!query.ValidatedOnly || operation.ValidationStatus == OperationValidationStatus.NoRecordedGap);
    }

    [GeneratedRegex(@"\A[a-z][a-z0-9_]*\.[a-z][a-z0-9_]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex OperationIdPattern();
}
