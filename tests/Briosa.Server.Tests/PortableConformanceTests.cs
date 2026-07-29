using System.Collections;
using System.Reflection;
using System.Text.Json;
using Briosa.Core.V1Alpha1;
using Briosa.Server.Generated.Sa.V2026_1_0529_7.V1Alpha1;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Briosa.Server.Tests;

public sealed class PortableConformanceTests
{
    private static readonly HashSet<string> RequiredOperationScenarioKinds =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "capability.metadata",
            "error.cancellation_not_started",
            "error.cancellation_started_unknown",
            "error.deadline_not_started",
            "error.deadline_started_unknown",
            "error.disconnected",
            "error.execute_rejected",
            "error.malformed_worker_response",
            "error.mp_failed",
            "error.mp_result_retrieval_failed",
            "error.worker_crash",
            "error.worker_hang",
            "metadata.execution_disposition",
            "metadata.execution_scope",
            "metadata.replay_guidance",
            "policy.allowed",
            "policy.denied",
            "readiness.unverified",
            "request.valid",
            "result.success"
        };

    [Fact]
    public void ManifestAndGeneratedBindingsHaveTheExactSupportedOperationSet()
    {
        var manifest = LoadManifest();
        var bindings = TargetCatalogConformanceMetadata.Operations;

        Assert.Equal(
            manifest.Keys.Order(StringComparer.Ordinal),
            bindings.Select(binding => binding.Operation.OperationId)
                .Order(StringComparer.Ordinal));
        Assert.Equal(
            TargetCatalogMetadata.Operations.Select(operation => operation.OperationId)
                .Order(StringComparer.Ordinal),
            bindings.Select(binding => binding.Operation.OperationId)
                .Order(StringComparer.Ordinal));
        Assert.All(manifest.Values, operation =>
        {
            Assert.Equal(
                operation.Scenarios.Count,
                operation.Scenarios.Select(scenario => scenario.ScenarioId)
                    .Distinct(StringComparer.Ordinal).Count());
            Assert.Subset(
                operation.Scenarios.Select(scenario => scenario.Kind)
                    .ToHashSet(StringComparer.Ordinal),
                RequiredOperationScenarioKinds);
        });
    }

    [Fact]
    public void EverySupportedRequestMapsToTheExactImmutableWorkerCommand()
    {
        var manifest = LoadManifest();
        foreach (var binding in TargetCatalogConformanceMetadata.Operations)
        {
            var operation = manifest[binding.Operation.OperationId];
            var request = CreatePopulatedMessage(binding.RequestType);

            var command = binding.CreateCommand(request);

            Assert.Equal(operation.OperationId, command.OperationId);
            Assert.Equal(operation.MpStep, command.StepName);
            Assert.Equal(operation.Inputs.Count, command.InputArguments.Count);
            Assert.Equal(operation.Outputs.Count, command.OutputArguments.Count);
            foreach (var expected in operation.Inputs)
            {
                var actual = Assert.Single(command.InputArguments, argument =>
                    argument.Name == expected.MpName);
                Assert.Equal(expected.Binding, actual.SdkBinding);
                Assert.Equal(expected.WorkerValueKind, actual.Kind.ToString());
            }

            foreach (var expected in operation.Outputs)
            {
                var actual = Assert.Single(command.OutputArguments, argument =>
                    argument.Name == expected.MpName);
                Assert.Equal(expected.Binding, actual.SdkBinding);
                Assert.Equal(expected.WorkerValueKind, actual.Kind.ToString());
            }

            var inputSnapshot = command.InputArguments.ToArray();
            var outputSnapshot = command.OutputArguments.ToArray();
            PopulateMessage(request, depth: 0);
            Assert.Equal(inputSnapshot, command.InputArguments);
            Assert.Equal(outputSnapshot, command.OutputArguments);
        }
    }

    [Fact]
    public void EveryGeneratedRequestNegativeAndOmissionCaseExecutes()
    {
        var manifest = LoadManifest();
        foreach (var binding in TargetCatalogConformanceMetadata.Operations)
        {
            var operation = manifest[binding.Operation.OperationId];
            foreach (var input in operation.Inputs)
            {
                var field = ((IMessage)Activator.CreateInstance(binding.RequestType)!)
                    .Descriptor.FindFieldByName(input.ArgumentId);
                Assert.NotNull(field);
                if (HasScenario(operation, "request.present_default_like", input.ArgumentId))
                {
                    var request = CreatePopulatedMessage(binding.RequestType);
                    SetPresentDefaultLike(request, field);
                    var command = binding.CreateCommand(request);
                    Assert.Contains(command.InputArguments, argument =>
                        argument.Name == input.MpName);
                }

                if (HasScenario(operation, "request.required_missing", input.ArgumentId))
                {
                    var request = CreatePopulatedMessage(binding.RequestType);
                    field.Accessor.Clear(request);
                    Assert.Throws<ArgumentException>(() => binding.CreateCommand(request));
                }

                if (HasScenario(operation, "request.optional_omitted", input.ArgumentId))
                {
                    var request = CreatePopulatedMessage(binding.RequestType);
                    field.Accessor.Clear(request);
                    var command = binding.CreateCommand(request);
                    if (input.OmissionBehavior == "omit_sdk_setter")
                    {
                        Assert.DoesNotContain(command.InputArguments, argument =>
                            argument.Name == input.MpName);
                    }
                    else
                    {
                        Assert.Contains(command.InputArguments, argument =>
                            argument.Name == input.MpName);
                    }
                }

                if (HasScenario(operation, "request.malformed_shape", input.ArgumentId))
                {
                    var request = CreatePopulatedMessage(binding.RequestType);
                    field.Accessor.SetValue(
                        request,
                        Activator.CreateInstance(field.MessageType.ClrType)!);
                    Assert.Throws<ArgumentException>(() => binding.CreateCommand(request));
                }

                if (HasScenario(operation, "request.unknown_enum", input.ArgumentId))
                {
                    var request = CreatePopulatedMessage(binding.RequestType);
                    SetFirstEnumToUnknown(request, field);
                    Assert.Throws<ArgumentException>(() => binding.CreateCommand(request));
                }
            }
        }
    }

    [Fact]
    public void RenamePointOmissionSendsTheReviewedFalseDefault()
    {
        var binding = Assert.Single(
            TargetCatalogConformanceMetadata.Operations,
            candidate => candidate.Operation.OperationId ==
                "collection_operations.rename_point");
        var request = CreatePopulatedMessage(binding.RequestType);
        request.Descriptor.FindFieldByName("overwrite_if_exists").Accessor.Clear(request);

        var command = binding.CreateCommand(request);

        var overwrite = Assert.Single(
            command.InputArguments,
            argument => argument.Name == "Overwrite if exists?");
        Assert.Equal(WorkerMpValueKind.Logical, overwrite.Kind);
        Assert.False(overwrite.BooleanValue);
    }

    [Fact]
    public async Task EverySupportedResultMapsThroughTheGeneratedExecutionSeam()
    {
        var manifest = LoadManifest();
        foreach (var binding in TargetCatalogConformanceMetadata.Operations)
        {
            var operation = manifest[binding.Operation.OperationId];
            var worker = new FixedOutcomeExecutor(Completed(binding.OutputContracts));
            var request = CreatePopulatedMessage(binding.RequestType);

            var result = await binding.ExecuteAsync(
                Executor(worker),
                request,
                CancellationToken.None,
                null);

            var message = Assert.IsAssignableFrom<IMessage>(result);
            Assert.Equal(binding.ResultType, result.GetType());
            foreach (var output in operation.Outputs)
            {
                var field = message.Descriptor.FindFieldByName(output.ArgumentId);
                Assert.NotNull(field);
                Assert.True(field.IsRepeated
                    ? Count(field.Accessor.GetValue(message)) > 0
                    : field.Accessor.HasValue(message));
            }

            var execution = message.Descriptor.FindFieldByNumber(1000);
            Assert.NotNull(execution);
            Assert.True(execution.Accessor.HasValue(message));
            Assert.Equal(binding.Operation.OperationId, worker.Command?.OperationId);
        }
    }

    [Fact]
    public async Task EverySupportedOutputExecutesGetterFailureAndMissingTypedValueCases()
    {
        var manifest = LoadManifest();
        foreach (var binding in TargetCatalogConformanceMetadata.Operations)
        {
            var operation = manifest[binding.Operation.OperationId];
            foreach (var output in operation.Outputs)
            {
                var contract = Assert.Single(binding.OutputContracts, value =>
                    value.FieldName == output.ArgumentId);
                var getterFailure = Completed(binding.OutputContracts, contract, OutputFault.GetterFailed);
                var getterException = await ExecuteFailure(binding, getterFailure);
                Assert.Equal(StatusCode.DataLoss, getterException.StatusCode);
                Assert.Equal(
                    OperationFailureKind.OutputRetrievalFailure,
                    Error(getterException).Kind);

                var missingValue = Completed(binding.OutputContracts, contract, OutputFault.MissingValue);
                var missingException = await ExecuteFailure(binding, missingValue);
                Assert.Equal(StatusCode.DataLoss, missingException.StatusCode);
                Assert.Equal(
                    OperationFailureKind.OutputRetrievalFailure,
                    Error(missingException).Kind);

                if (HasScenario(operation, "result.unknown_returned_enum", output.ArgumentId))
                {
                    var unknown = Completed(binding.OutputContracts, contract, OutputFault.UnknownEnum);
                    var unknownException = await ExecuteFailure(binding, unknown);
                    Assert.Equal(StatusCode.DataLoss, unknownException.StatusCode);
                    Assert.Equal(OperationFailureKind.Internal, Error(unknownException).Kind);
                }
            }
        }
    }

    [Fact]
    public async Task EverySupportedOperationExecutesTheSharedFailureAndReplayMatrix()
    {
        foreach (var binding in TargetCatalogConformanceMetadata.Operations)
        {
            var cases = FailureCases(binding.OutputContracts);
            foreach (var item in cases)
            {
                var exception = await ExecuteFailure(binding, item.Outcome, item.Deadline)
                    .ConfigureAwait(true);
                var error = Error(exception);
                Assert.Equal(item.StatusCode, exception.StatusCode);
                Assert.Equal(item.FailureKind, error.Kind);
                Assert.Equal(item.Disposition, error.ExecutionDisposition);
                Assert.Equal(binding.Operation.ReplaySafety, error.ReplaySafety);
                Assert.NotEqual(ReplayGuidance.Unspecified, error.ReplayGuidance);
                Assert.Equal(binding.Operation.OperationId, error.OperationId);
            }
        }
    }

    [Fact]
    public void EverySupportedOperationExecutesAllowDenyAndCapabilityMetadataCases()
    {
        var allowedConfiguration = new Dictionary<string, string?>(StringComparer.Ordinal);
        for (var index = 0; index < TargetCatalogMetadata.Operations.Count; index++)
        {
            allowedConfiguration.Add(
                $"{OperationPolicy.AllowKey}:{index}",
                TargetCatalogMetadata.Operations[index].OperationId);
        }

        var allowPolicy = OperationPolicy.Create(
            new ConfigurationBuilder().AddInMemoryCollection(allowedConfiguration).Build(),
            TargetCatalogMetadata.Operations);
        var denyPolicy = OperationPolicy.Create(
            new ConfigurationBuilder().Build(),
            TargetCatalogMetadata.Operations);
        foreach (var binding in TargetCatalogConformanceMetadata.Operations)
        {
            var command = binding.CreateCommand(CreatePopulatedMessage(binding.RequestType));
            Assert.Equal(OperationPolicyDecisionKind.Allowed, allowPolicy.Evaluate(command).Kind);
            Assert.Equal(OperationPolicyDecisionKind.Denied, denyPolicy.Evaluate(command).Kind);
            var capability = Assert.Single(allowPolicy.AllowedOperations, operation =>
                operation.OperationId == binding.Operation.OperationId);
            Assert.Equal(binding.Operation.FullyQualifiedMethod, capability.FullyQualifiedMethod);
            Assert.Equal(binding.Operation.ExecutionScope, capability.ExecutionScope);
            Assert.Equal(binding.Operation.ReplaySafety, capability.ReplaySafety);
        }
    }

    private static CatalogOperationExecutor Executor(IWorkerCommandExecutor worker) =>
        new(
            worker,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance),
            TimeProvider.System);

    private static async Task<RpcException> ExecuteFailure(
        CatalogOperationConformanceBinding binding,
        WorkerExecutionOutcome outcome,
        DateTime? deadline = null)
    {
        var exception = await Assert.ThrowsAsync<RpcException>(() => binding.ExecuteAsync(
            Executor(new FixedOutcomeExecutor(outcome)),
            CreatePopulatedMessage(binding.RequestType),
            CancellationToken.None,
            deadline)).ConfigureAwait(true);
        return exception;
    }

    private static WorkerExecutionOutcome Completed(
        IReadOnlyList<OperationOutputContract> contracts,
        OperationOutputContract? faulted = null,
        OutputFault fault = OutputFault.None)
    {
        var outputs = contracts.Select(contract =>
        {
            if (contract == faulted && fault == OutputFault.GetterFailed)
            {
                return new WorkerMpOutputValue(contract.ArgumentName, contract.Kind, Retrieved: false);
            }

            if (contract == faulted && fault == OutputFault.MissingValue)
            {
                return new WorkerMpOutputValue(contract.ArgumentName, contract.Kind, Retrieved: true);
            }

            var value = ValidOutput(contract);
            return contract == faulted && fault == OutputFault.UnknownEnum
                ? WithUnknownEnum(value)
                : value;
        }).ToArray();
        return Outcome(
            WorkerExecutionStatus.Completed,
            WorkerExecutionDisposition.Completed,
            new WorkerMpExecutionResult(
                ExecuteStepReturned: true,
                MpResultRetrieved: true,
                MpSucceeded: true,
                MpResultCode: 2,
                DurationMilliseconds: 1,
                outputs,
                fault == OutputFault.GetterFailed ? "sdk-output-retrieval-failed" : "completed"),
            Connection(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady),
            "completed");
    }

    private static IReadOnlyList<FailureCase> FailureCases(
        IReadOnlyList<OperationOutputContract> contracts) =>
        [
            new(
                "disconnected",
                Outcome(
                    WorkerExecutionStatus.Unavailable,
                    WorkerExecutionDisposition.NotStarted,
                    execution: null,
                    Connection(WorkerConnectionState.Disconnected),
                    "spatialanalyzer-not-running"),
                null,
                StatusCode.Unavailable,
                OperationFailureKind.SpatialAnalyzerUnavailable,
                ExecutionDisposition.NotStarted),
            new(
                "readiness-unverified",
                Outcome(
                    WorkerExecutionStatus.Unavailable,
                    WorkerExecutionDisposition.NotStarted,
                    execution: null,
                    Connection(
                        WorkerConnectionState.Connected,
                        WorkerExecutionReadinessState.Unverified),
                    "execution-readiness-unverified"),
                null,
                StatusCode.Unavailable,
                OperationFailureKind.SpatialAnalyzerUnavailable,
                ExecutionDisposition.NotStarted),
            new(
                "execute-rejected",
                Outcome(
                    WorkerExecutionStatus.Completed,
                    WorkerExecutionDisposition.StartedOutcomeUnknown,
                    Execution(executeStepReturned: false, diagnosticCode: "execute-step-rejected"),
                    Connection(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady),
                    "execute-step-rejected"),
                null,
                StatusCode.FailedPrecondition,
                OperationFailureKind.ExecuteStepRejected,
                ExecutionDisposition.StartedOutcomeUnknown),
            new(
                "mp-result-retrieval-failed",
                Outcome(
                    WorkerExecutionStatus.Completed,
                    WorkerExecutionDisposition.StartedOutcomeUnknown,
                    Execution(mpResultRetrieved: false, diagnosticCode: "sdk-mp-result-retrieval-failed"),
                    Connection(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady),
                    "sdk-mp-result-retrieval-failed"),
                null,
                StatusCode.Internal,
                OperationFailureKind.MpResultRetrievalFailure,
                ExecutionDisposition.StartedOutcomeUnknown),
            new(
                "mp-failed",
                Outcome(
                    WorkerExecutionStatus.Completed,
                    WorkerExecutionDisposition.Completed,
                    Execution(mpSucceeded: false, diagnosticCode: "mp-command-failed"),
                    Connection(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady),
                    "mp-command-failed"),
                null,
                StatusCode.FailedPrecondition,
                OperationFailureKind.MpFailure,
                ExecutionDisposition.Completed),
            CancellationCase(
                "deadline-not-started",
                WorkerExecutionDisposition.NotStarted,
                deadline: DateTime.UtcNow.AddMinutes(-1),
                OperationFailureKind.CallerDeadlineExceeded,
                StatusCode.DeadlineExceeded,
                ExecutionDisposition.NotStarted),
            CancellationCase(
                "deadline-started-unknown",
                WorkerExecutionDisposition.StartedOutcomeUnknown,
                deadline: DateTime.UtcNow.AddMinutes(-1),
                OperationFailureKind.CallerDeadlineExceeded,
                StatusCode.DeadlineExceeded,
                ExecutionDisposition.StartedOutcomeUnknown),
            CancellationCase(
                "cancellation-not-started",
                WorkerExecutionDisposition.NotStarted,
                deadline: null,
                OperationFailureKind.CallerCancelled,
                StatusCode.Cancelled,
                ExecutionDisposition.NotStarted),
            CancellationCase(
                "cancellation-started-unknown",
                WorkerExecutionDisposition.StartedOutcomeUnknown,
                deadline: null,
                OperationFailureKind.CallerCancelled,
                StatusCode.Cancelled,
                ExecutionDisposition.StartedOutcomeUnknown),
            new(
                "worker-hang",
                Outcome(
                    WorkerExecutionStatus.WatchdogTimeout,
                    WorkerExecutionDisposition.StartedOutcomeUnknown,
                    execution: null,
                    Connection(WorkerConnectionState.Faulted),
                    "worker-watchdog-timeout"),
                null,
                StatusCode.Unavailable,
                OperationFailureKind.WorkerWatchdogTimeout,
                ExecutionDisposition.StartedOutcomeUnknown),
            new(
                "worker-crash",
                Outcome(
                    WorkerExecutionStatus.WorkerFailure,
                    WorkerExecutionDisposition.StartedOutcomeUnknown,
                    execution: null,
                    Connection(WorkerConnectionState.Faulted),
                    "worker-process-exited"),
                null,
                StatusCode.Unavailable,
                OperationFailureKind.WorkerFailure,
                ExecutionDisposition.StartedOutcomeUnknown),
            new(
                "malformed-worker-response",
                Outcome(
                    WorkerExecutionStatus.Completed,
                    WorkerExecutionDisposition.Completed,
                    execution: null,
                    Connection(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady),
                    "worker-result-missing"),
                null,
                StatusCode.Internal,
                OperationFailureKind.Internal,
                ExecutionDisposition.Completed),
            new(
                "policy-denied",
                Outcome(
                    WorkerExecutionStatus.PolicyDenied,
                    WorkerExecutionDisposition.NotStarted,
                    execution: null,
                    Connection(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady),
                    "operation-policy-denied"),
                null,
                StatusCode.PermissionDenied,
                OperationFailureKind.PolicyDenied,
                ExecutionDisposition.NotStarted)
        ];

    private static FailureCase CancellationCase(
        string name,
        WorkerExecutionDisposition workerDisposition,
        DateTime? deadline,
        OperationFailureKind failureKind,
        StatusCode statusCode,
        ExecutionDisposition disposition) =>
        new(
            name,
            Outcome(
                WorkerExecutionStatus.ClientCancelled,
                workerDisposition,
                execution: null,
                Connection(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady),
                "client-wait-cancelled"),
            deadline,
            statusCode,
            failureKind,
            disposition);

    private static WorkerMpExecutionResult Execution(
        bool executeStepReturned = true,
        bool mpResultRetrieved = true,
        bool mpSucceeded = true,
        string diagnosticCode = "completed") =>
        new(
            executeStepReturned,
            mpResultRetrieved,
            mpSucceeded,
            mpResultRetrieved ? 2 : null,
            DurationMilliseconds: 1,
            OutputValues: [],
            diagnosticCode);

    private static WorkerExecutionOutcome Outcome(
        WorkerExecutionStatus status,
        WorkerExecutionDisposition disposition,
        WorkerMpExecutionResult? execution,
        WorkerConnectionSnapshot? connection,
        string diagnosticCode) =>
        new(status, disposition, execution, connection, diagnosticCode, Generation: 7);

    private static WorkerConnectionSnapshot Connection(
        WorkerConnectionState state,
        WorkerExecutionReadinessState readiness = WorkerExecutionReadinessState.Unverified) =>
        new(
            state,
            readiness,
            StatusCode: null,
            Attempt: 1,
            MaximumAttempts: 1,
            "portable-conformance",
            DateTimeOffset.UnixEpoch);

    private static WorkerMpOutputValue ValidOutput(OperationOutputContract contract)
    {
        WorkerMpOutputValue Base(
            bool? BooleanValue = null,
            int? IntegerValue = null,
            double? DoubleValue = null,
            string? StringValue = null,
            WorkerPointNameValue? PointNameValue = null,
            WorkerVectorValue? VectorValue = null,
            WorkerToleranceVectorOptionsValue? ToleranceVectorOptionsValue = null,
            WorkerCollectionInstrumentIdValue? CollectionInstrumentIdValue = null,
            WorkerCollectionInstrumentIdListValue? CollectionInstrumentIdListValue = null,
            WorkerCollectionMachineIdValue? CollectionMachineIdValue = null,
            WorkerCollectionItemNameValue? CollectionItemNameValue = null,
            WorkerCollectionItemNameListValue? CollectionItemNameListValue = null,
            WorkerCollectionObjectNameValue? CollectionObjectNameValue = null,
            WorkerCollectionObjectNameListValue? CollectionObjectNameListValue = null,
            WorkerCollectionGroupNameListValue? CollectionGroupNameListValue = null,
            WorkerCollectionVectorGroupNameValue? CollectionVectorGroupNameValue = null,
            WorkerCollectionVectorGroupNameListValue? CollectionVectorGroupNameListValue = null,
            WorkerPointNameListValue? PointNameListValue = null,
            WorkerStringListValue? StringListValue = null,
            WorkerVectorNameListValue? VectorNameListValue = null,
            WorkerDoubleArrayValue? DoubleArrayValue = null,
            WorkerTransformValue? TransformValue = null,
            WorkerWorldTransformValue? WorldTransformValue = null,
            WorkerFileReferenceValue? FileReferenceValue = null,
            WorkerFitConstraintScalarOptionsValue? FitConstraintScalarOptionsValue = null,
            WorkerToleranceScalarOptionsValue? ToleranceScalarOptionsValue = null) =>
            new(
                contract.ArgumentName,
                contract.Kind,
                Retrieved: true,
                BooleanValue,
                IntegerValue,
                DoubleValue,
                StringValue,
                PointNameValue,
                VectorValue,
                ToleranceVectorOptionsValue,
                CollectionInstrumentIdValue,
                CollectionInstrumentIdListValue,
                CollectionMachineIdValue,
                CollectionItemNameValue,
                CollectionItemNameListValue,
                CollectionObjectNameValue,
                CollectionObjectNameListValue,
                CollectionGroupNameListValue,
                CollectionVectorGroupNameValue,
                CollectionVectorGroupNameListValue,
                PointNameListValue,
                StringListValue,
                VectorNameListValue,
                DoubleArrayValue,
                TransformValue,
                WorldTransformValue,
                FileReferenceValue,
                FitConstraintScalarOptionsValue,
                ToleranceScalarOptionsValue);

        return contract.Kind switch
        {
            WorkerMpValueKind.Logical => Base(BooleanValue: true),
            WorkerMpValueKind.WholeNumber => Base(IntegerValue: 7),
            WorkerMpValueKind.FloatingPoint => Base(DoubleValue: 1.25),
            WorkerMpValueKind.Text or
            WorkerMpValueKind.ChartName or
            WorkerMpValueKind.CloudName or
            WorkerMpValueKind.CollectionName or
            WorkerMpValueKind.FrameName or
            WorkerMpValueKind.VectorGroupName or
            WorkerMpValueKind.ViewName => Base(StringValue: "portable-conformance"),
            WorkerMpValueKind.DoubleArray => Base(DoubleArrayValue: new([1, 2, 3])),
            WorkerMpValueKind.EditText => Base(StringListValue: new(["one", "two"])),
            WorkerMpValueKind.Transform => Base(TransformValue: new(Matrix())),
            WorkerMpValueKind.WorldTransform => Base(
                WorldTransformValue: new(new(Matrix()), 1.5)),
            WorkerMpValueKind.FileReference => Base(
                FileReferenceValue: new("portable.xit", EmbeddedFile: false)),
            WorkerMpValueKind.PointName => Base(
                PointNameValue: new("Collection", "Group", "Point")),
            WorkerMpValueKind.Vector => Base(VectorValue: new(1, 2, 3)),
            WorkerMpValueKind.ToleranceVectorOptions => Base(
                ToleranceVectorOptionsValue: ToleranceVector()),
            WorkerMpValueKind.CollectionInstrumentId => Base(
                CollectionInstrumentIdValue: new("Collection", 7)),
            WorkerMpValueKind.CollectionInstrumentIdList => Base(
                CollectionInstrumentIdListValue: new([new("Collection", 7)])),
            WorkerMpValueKind.CollectionMachineId => Base(
                CollectionMachineIdValue: new("Collection", 7)),
            WorkerMpValueKind.CollectionItemName => Base(
                CollectionItemNameValue: new(
                    "Collection",
                    "Item",
                    WorkerItemTypeValue.PointGroup)),
            WorkerMpValueKind.CollectionItemNameList => Base(
                CollectionItemNameListValue: new([
                    new("Collection", "Item", WorkerItemTypeValue.PointGroup)
                ])),
            WorkerMpValueKind.CollectionObjectName => Base(
                CollectionObjectNameValue: new(
                    "Collection",
                    "Object",
                    WorkerObjectTypeValue.PointGroup)),
            WorkerMpValueKind.CollectionObjectNameList => Base(
                CollectionObjectNameListValue: new([
                    new("Collection", "Object", WorkerObjectTypeValue.PointGroup)
                ])),
            WorkerMpValueKind.CollectionGroupNameList => Base(
                CollectionGroupNameListValue: new([new("Collection", "Group")])),
            WorkerMpValueKind.CollectionVectorGroupName => Base(
                CollectionVectorGroupNameValue: new("Collection", "Vectors")),
            WorkerMpValueKind.CollectionVectorGroupNameList => Base(
                CollectionVectorGroupNameListValue: new([new("Collection", "Vectors")])),
            WorkerMpValueKind.PointNameList => Base(
                PointNameListValue: new([new("Collection", "Group", "Point")])),
            WorkerMpValueKind.StringList => Base(StringListValue: new(["one", "two"])),
            WorkerMpValueKind.VectorNameList => Base(
                VectorNameListValue: new([new("Collection", "Group", "Vector")])),
            WorkerMpValueKind.FitConstraintScalarOptions => Base(
                FitConstraintScalarOptionsValue: new(new(true, 1), new(true, -1))),
            WorkerMpValueKind.ToleranceScalarOptions => Base(
                ToleranceScalarOptionsValue: new(new(true, 1), new(true, -1))),
            _ => throw new InvalidOperationException(
                $"Portable output sample support is missing for '{contract.Kind}'.")
        };
    }

    private static WorkerMpOutputValue WithUnknownEnum(WorkerMpOutputValue value) =>
        value.Kind switch
        {
            WorkerMpValueKind.CollectionItemName => value with
            {
                CollectionItemNameValue = value.CollectionItemNameValue! with
                {
                    ItemType = (WorkerItemTypeValue)int.MaxValue
                }
            },
            WorkerMpValueKind.CollectionItemNameList => value with
            {
                CollectionItemNameListValue = new([
                    value.CollectionItemNameListValue!.Values[0] with
                    {
                        ItemType = (WorkerItemTypeValue)int.MaxValue
                    }
                ])
            },
            WorkerMpValueKind.CollectionObjectName => value with
            {
                CollectionObjectNameValue = value.CollectionObjectNameValue! with
                {
                    ObjectType = (WorkerObjectTypeValue)int.MaxValue
                }
            },
            WorkerMpValueKind.CollectionObjectNameList => value with
            {
                CollectionObjectNameListValue = new([
                    value.CollectionObjectNameListValue!.Values[0] with
                    {
                        ObjectType = (WorkerObjectTypeValue)int.MaxValue
                    }
                ])
            },
            _ => throw new InvalidOperationException(
                $"Unknown returned enum is not applicable to '{value.Kind}'.")
        };

    private static double[] Matrix() =>
        Enumerable.Range(0, 16).Select(value => (double)value).ToArray();

    private static WorkerToleranceVectorOptionsValue ToleranceVector()
    {
        var limit = new WorkerToleranceLimit(true, 1);
        return new(limit, limit, limit, limit, limit, limit, limit, limit);
    }

    private static IMessage CreatePopulatedMessage(Type type)
    {
        var message = Assert.IsAssignableFrom<IMessage>(Activator.CreateInstance(type));
        PopulateMessage(message, depth: 0);
        return message;
    }

    private static void PopulateMessage(IMessage message, int depth)
    {
        if (depth > 8)
        {
            throw new InvalidOperationException("Portable request sample exceeded the message nesting limit.");
        }

        foreach (var field in message.Descriptor.Fields.InFieldNumberOrder())
        {
            if (field.IsMap)
            {
                throw new InvalidOperationException(
                    $"Portable request sample support is missing for map field '{field.FullName}'.");
            }

            var value = SampleValue(field, depth + 1);
            if (field.IsRepeated)
            {
                var values = field.Accessor.GetValue(message);
                var add = values.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Single(method =>
                        method.Name == "Add" &&
                        method.GetParameters() is [{ ParameterType: var parameterType }] &&
                        parameterType.IsInstanceOfType(value));
                _ = add.Invoke(values, [value]);
            }
            else
            {
                field.Accessor.SetValue(message, value);
            }
        }
    }

    private static object SampleValue(FieldDescriptor field, int depth) =>
        field.FieldType switch
        {
            FieldType.Bool => true,
            FieldType.Bytes => ByteString.CopyFromUtf8("portable-conformance"),
            FieldType.Double => 1.25d,
            FieldType.Float => 1.25f,
            FieldType.Int32 or FieldType.SInt32 or FieldType.SFixed32 => 7,
            FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64 => 7L,
            FieldType.UInt32 or FieldType.Fixed32 => 7U,
            FieldType.UInt64 or FieldType.Fixed64 => 7UL,
            FieldType.String => "portable-conformance",
            FieldType.Enum => Enum.ToObject(
                field.EnumType.ClrType,
                field.EnumType.Values.First(value => value.Number != 0).Number),
            FieldType.Message => CreateNestedMessage(field, depth),
            _ => throw new InvalidOperationException(
                $"Portable request sample support is missing for '{field.FieldType}'.")
        };

    private static IMessage CreateNestedMessage(FieldDescriptor field, int depth)
    {
        var message = Assert.IsAssignableFrom<IMessage>(
            Activator.CreateInstance(field.MessageType.ClrType));
        PopulateMessage(message, depth);
        return message;
    }

    private static void SetPresentDefaultLike(IMessage request, FieldDescriptor field)
    {
        object value = field.FieldType switch
        {
            FieldType.Bool => false,
            FieldType.Double => 0d,
            FieldType.Float => 0f,
            FieldType.Int32 or FieldType.SInt32 or FieldType.SFixed32 => 0,
            FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64 => 0L,
            FieldType.UInt32 or FieldType.Fixed32 => 0U,
            FieldType.UInt64 or FieldType.Fixed64 => 0UL,
            FieldType.String => string.Empty,
            _ => throw new InvalidOperationException(
                $"Present default-like coverage is not defined for '{field.FieldType}'.")
        };
        field.Accessor.SetValue(request, value);
        Assert.True(field.Accessor.HasValue(request));
    }

    private static void SetFirstEnumToUnknown(IMessage request, FieldDescriptor field)
    {
        if (field.FieldType == FieldType.Enum)
        {
            field.Accessor.SetValue(
                request,
                Enum.ToObject(field.EnumType.ClrType, int.MaxValue));
            return;
        }

        var nested = Assert.IsAssignableFrom<IMessage>(field.Accessor.GetValue(request));
        var enumField = nested.Descriptor.Fields.InFieldNumberOrder()
            .FirstOrDefault(candidate => candidate.FieldType == FieldType.Enum) ??
            throw new InvalidOperationException(
                $"No enum component exists in '{field.MessageType.FullName}'.");
        enumField.Accessor.SetValue(
            nested,
            Enum.ToObject(enumField.EnumType.ClrType, int.MaxValue));
    }

    private static int Count(object value) =>
        value switch
        {
            ICollection collection => collection.Count,
            _ => (int)(value.GetType().GetProperty("Count")?.GetValue(value) ?? 0)
        };

    private static bool HasScenario(
        ManifestOperation operation,
        string kind,
        string argumentId) =>
        operation.Scenarios.Any(scenario =>
            scenario.Kind == kind && scenario.ArgumentId == argumentId);

    private static OperationError Error(RpcException exception) =>
        OperationError.Parser.ParseFrom(
            Assert.Single(exception.Trailers, trailer =>
                trailer.Key == GrpcOperationOutcomeMapper.ErrorTrailerName).ValueBytes);

    private static Dictionary<string, ManifestOperation> LoadManifest()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(
            root.FullName,
            "generated",
            "conformance",
            "sa",
            "2026.1.0529.7",
            "manifest.json");
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        return document.RootElement.GetProperty("operations").EnumerateArray()
            .Select(operation => new ManifestOperation(
                operation.GetProperty("operation_id").GetString()!,
                operation.GetProperty("mp_step").GetString()!,
                ReadArguments(operation.GetProperty("inputs")),
                ReadArguments(operation.GetProperty("outputs")),
                operation.GetProperty("scenarios").EnumerateArray()
                    .Select(scenario => new ManifestScenario(
                        scenario.GetProperty("scenario_id").GetString()!,
                        scenario.GetProperty("kind").GetString()!,
                        scenario.GetProperty("argument_id").ValueKind == JsonValueKind.Null
                            ? null
                            : scenario.GetProperty("argument_id").GetString()))
                    .ToArray()))
            .ToDictionary(operation => operation.OperationId, StringComparer.Ordinal);
    }

    private static ManifestArgument[] ReadArguments(JsonElement arguments) =>
        [.. arguments.EnumerateArray().Select(argument => new ManifestArgument(
            argument.GetProperty("argument_id").GetString()!,
            argument.GetProperty("mp_name").GetString()!,
            argument.GetProperty("worker_value_kind").GetString()!,
            argument.GetProperty("binding").GetString()!,
            argument.GetProperty("omission_behavior").ValueKind == JsonValueKind.Null
                ? null
                : argument.GetProperty("omission_behavior").GetString()))];

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ??
            throw new DirectoryNotFoundException("Could not locate the Briosa repository root.");
    }

    private sealed class FixedOutcomeExecutor(WorkerExecutionOutcome outcome) : IWorkerCommandExecutor
    {
        public WorkerMpCommand? Command { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Command = command;
            return Task.FromResult(outcome);
        }
    }

    private sealed record ManifestOperation(
        string OperationId,
        string MpStep,
        IReadOnlyList<ManifestArgument> Inputs,
        IReadOnlyList<ManifestArgument> Outputs,
        IReadOnlyList<ManifestScenario> Scenarios);
    private sealed record ManifestArgument(
        string ArgumentId,
        string MpName,
        string WorkerValueKind,
        string Binding,
        string? OmissionBehavior);
    private sealed record ManifestScenario(
        string ScenarioId,
        string Kind,
        string? ArgumentId);
    private sealed record FailureCase(
        string Name,
        WorkerExecutionOutcome Outcome,
        DateTime? Deadline,
        StatusCode StatusCode,
        OperationFailureKind FailureKind,
        ExecutionDisposition Disposition);
    private enum OutputFault
    {
        None,
        GetterFailed,
        MissingValue,
        UnknownEnum
    }
}
