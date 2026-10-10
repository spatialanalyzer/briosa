using Briosa;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Microsoft.Extensions.Configuration;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.AspNetCore.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ProtocolIdentityMatchState = Briosa.RuntimeIdentityMatchState;
using ProtocolIdentitySource = Briosa.RuntimeIdentityEvidenceSource;
using ServerIdentityEvidence = Briosa.Server.Workers.RuntimeIdentityEvidence;
using ServerIdentityMatchState = Briosa.Server.Workers.RuntimeIdentityMatchState;
using ServerIdentitySource = Briosa.Server.Workers.RuntimeIdentityEvidenceSource;

namespace Briosa.Server.Tests;

public sealed class DiscoveryServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AllReadinessViewsRequireAnAvailableAdmissionConsumer(bool admissionOpen)
    {
        var snapshot = Snapshot(WorkerLifecycleState.Ready, WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.ExecutionReady) with { AdmissionOpen = admissionOpen, StateRevision = 17 };
        var status = new FakeWorkerStatusProvider(snapshot);
        var discovery = new ServerDiscoveryService(status, new FakeBuildIdentityProvider(), CreatePolicy());
        var lifecycle = new SpatialAnalyzerSdkLifecycleStateProjection(status);
        Assert.Equal(admissionOpen, WorkerReadinessHealthCheck.IsReady(snapshot));
        Assert.Equal(admissionOpen, discovery.CreateServerInfo().ReadyForMp);
        Assert.Equal(admissionOpen, lifecycle.Current.ReadyForMp);
        Assert.Equal(17UL, lifecycle.Current.StateRevision);
        Assert.Equal(admissionOpen ? SpatialAnalyzerSdkState.Ready : SpatialAnalyzerSdkState.Running,
            lifecycle.Current.SdkState);
    }

    [Fact]
    public async Task LivenessIsIndependentWhileReadinessRequiresVerifiedExecution()
    {
        var statusProvider = new FakeWorkerStatusProvider(Snapshot(
            WorkerLifecycleState.Ready,
            WorkerConnectionState.Faulted));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IWorkerStatusProvider>(statusProvider);
        services.AddBriosaHealthAndDiscovery();
        await using var provider = services.BuildServiceProvider();
        var health = provider.GetRequiredService<HealthCheckService>();
        var mappings = provider
            .GetRequiredService<IOptions<GrpcHealthChecksOptions>>()
            .Value.Services.Select(mapping => mapping.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var liveness = await health.CheckHealthAsync(registration =>
            registration.Name == WorkerReadinessHealthCheck.LivenessServiceName);
        var notReady = await health.CheckHealthAsync(registration =>
            registration.Name == WorkerReadinessHealthCheck.ReadinessServiceName);
        statusProvider.Current = Snapshot(
            WorkerLifecycleState.Ready,
            WorkerConnectionState.Connected);
        var connectedButUnverified = await health.CheckHealthAsync(registration =>
            registration.Name == WorkerReadinessHealthCheck.ReadinessServiceName);
        statusProvider.Current = Snapshot(
            WorkerLifecycleState.Ready,
            WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.ExecutionReady);
        var ready = await health.CheckHealthAsync(registration =>
            registration.Name == WorkerReadinessHealthCheck.ReadinessServiceName);

        Assert.Equal(
            [string.Empty, "briosa.liveness", "briosa.readiness"],
            mappings);
        Assert.Equal(HealthStatus.Healthy, liveness.Status);
        Assert.Equal(HealthStatus.Unhealthy, notReady.Status);
        Assert.Equal(HealthStatus.Unhealthy, connectedButUnverified.Status);
        Assert.Equal(HealthStatus.Healthy, ready.Status);
        Assert.Equal("briosa.liveness", WorkerReadinessHealthCheck.LivenessServiceName);
        Assert.Equal("briosa.readiness", WorkerReadinessHealthCheck.ReadinessServiceName);
    }

    [Fact]
    public void ServerInfoReportsSafeStateWithoutInventingConnectedVersion()
    {
        var snapshot = Snapshot(
            WorkerLifecycleState.Ready,
            WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.ExecutionReady) with
        {
            RuntimeIdentity = UnavailableIdentity()
        };
        var service = new ServerDiscoveryService(
            new FakeWorkerStatusProvider(snapshot),
            new FakeBuildIdentityProvider(),
            CreatePolicy());

        var response = service.CreateServerInfo();

        Assert.Equal("0.1.0-test", response.Version.BriosaVersion);
        Assert.Equal(2U, response.Compatibility.Major);
        Assert.Equal(0U, response.Compatibility.Revision);
        Assert.Equal("briosa", response.Version.ProtocolPackage);
        Assert.Equal("2026.1.0529.7", response.Version.SpatialAnalyzerTarget);
        Assert.Equal(WorkerRuntimeState.Ready, response.WorkerState);
        Assert.Equal(
            SpatialAnalyzerConnectionState.Connected,
            response.SpatialAnalyzerConnectionState);
        Assert.Equal(
            SpatialAnalyzerExecutionReadinessState.ExecutionReady,
            response.SpatialAnalyzerExecutionReadinessState);
        Assert.False(response.ReadyForMp);
        Assert.Equal(TargetIsolationMode.SingleTenant, response.TargetIsolationMode);
        Assert.False(response.HasConnectedSpatialAnalyzerVersion);
        Assert.Equal(
            ConnectedSpatialAnalyzerVersionState.Unavailable,
            response.ConnectedSpatialAnalyzerVersionState);
        Assert.False(response.ActivatedSdkIdentity.HasVersion);
        Assert.Equal(
            ProtocolIdentitySource.Unavailable,
            response.ActivatedSdkIdentity.Source);
        Assert.Equal(
            ProtocolIdentityMatchState.Unavailable,
            response.ActivatedSdkIdentity.MatchState);
        Assert.False(response.ConnectedSpatialAnalyzerIdentity.HasVersion);
    }

    [Fact]
    public void ServerInfoSeparatesConfiguredTargetAttachmentAndExecutionVerification()
    {
        var response = new ServerDiscoveryService(
            new FakeWorkerStatusProvider(Snapshot(
                WorkerLifecycleState.Ready,
                WorkerConnectionState.Connected,
                WorkerExecutionReadinessState.Unverified)),
            new FakeBuildIdentityProvider(),
            CreatePolicy())
            .CreateServerInfo();

        Assert.Equal("2026.1.0529.7", response.Version.SpatialAnalyzerTarget);
        Assert.Equal(
            SpatialAnalyzerConnectionState.Connected,
            response.SpatialAnalyzerConnectionState);
        Assert.Equal(
            SpatialAnalyzerExecutionReadinessState.Unverified,
            response.SpatialAnalyzerExecutionReadinessState);
        Assert.False(response.ReadyForMp);
    }

    [Fact]
    public void DiscoveryPreservesIndependentRuntimeAndAttestedIdentityEvidence()
    {
        var snapshot = Snapshot(
            WorkerLifecycleState.Ready,
            WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.ExecutionReady) with
        {
            RuntimeIdentity = new ExactTargetIdentitySnapshot(
                new ServerIdentityEvidence(
                    "2026.1.0529.7",
                    ServerIdentitySource.RuntimeVerification,
                    ServerIdentityMatchState.ExactMatch),
                new ServerIdentityEvidence(
                    "2026.1.0529.7",
                    ServerIdentitySource.OperatorAttestation,
                    ServerIdentityMatchState.ExactMatch))
        };

        var response = new ServerDiscoveryService(
            new FakeWorkerStatusProvider(snapshot),
            new FakeBuildIdentityProvider(),
            CreatePolicy())
            .CreateServerInfo();

        Assert.True(response.ReadyForMp);
        Assert.Equal(
            ProtocolIdentitySource.RuntimeVerification,
            response.ActivatedSdkIdentity.Source);
        Assert.Equal(
            ProtocolIdentitySource.OperatorAttestation,
            response.ConnectedSpatialAnalyzerIdentity.Source);
        Assert.Equal(
            ConnectedSpatialAnalyzerVersionState.OperatorAttestedMatch,
            response.ConnectedSpatialAnalyzerVersionState);
        Assert.Equal(
            "2026.1.0529.7",
            response.ConnectedSpatialAnalyzerVersion);
    }

    [Fact]
    public void VerifiedMismatchIsDiscoverableAndCannotReportReady()
    {
        var snapshot = Snapshot(
            WorkerLifecycleState.Ready,
            WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.ExecutionReady) with
        {
            RuntimeIdentity = new ExactTargetIdentitySnapshot(
                new ServerIdentityEvidence(
                    "2025.0",
                    ServerIdentitySource.RuntimeVerification,
                    ServerIdentityMatchState.Mismatch),
                new ServerIdentityEvidence(
                    "2026.1.0529.7",
                    ServerIdentitySource.RuntimeVerification,
                    ServerIdentityMatchState.ExactMatch))
        };

        var response = new ServerDiscoveryService(
            new FakeWorkerStatusProvider(snapshot),
            new FakeBuildIdentityProvider(),
            CreatePolicy())
            .CreateServerInfo();

        Assert.False(response.ReadyForMp);
        Assert.Equal("2025.0", response.ActivatedSdkIdentity.Version);
        Assert.Equal(
            ProtocolIdentityMatchState.Mismatch,
            response.ActivatedSdkIdentity.MatchState);
        Assert.Equal(
            ProtocolIdentityMatchState.ExactMatch,
            response.ConnectedSpatialAnalyzerIdentity.MatchState);
    }

    [Fact]
    public void CapabilitiesComeFromImplementedOperationRegistry()
    {
        var response = new ServerDiscoveryService(
            new FakeWorkerStatusProvider(Snapshot(WorkerLifecycleState.Stopped, null)),
            new FakeBuildIdentityProvider(),
            CreatePolicy())
            .CreateCapabilities();

        Assert.Equal("2026.1.0529.7", response.SpatialAnalyzerTarget);
        Assert.Equal("briosa", response.ProtocolPackage);
        var policy = CreatePolicy();
        Assert.Equal("standard", response.AdmissionProfile);
        Assert.Equal(policy.Fingerprint, response.PolicyFingerprint);
        Assert.Equal(policy.AllowedOperations.Count, response.Operations.Count);
        Assert.True(response.Operations.Count < SpatialAnalyzerApi.Operations.Count);
        Assert.Equal(
            policy.AllowedOperations.Select(operation => operation.OperationId),
            response.Operations.Select(operation => operation.OperationId));
        Assert.DoesNotContain(response.Operations, operation =>
            operation.ExecutionScope == global::Briosa.OperationExecutionScope.ExclusiveWorkflow);
        foreach (var descriptor in policy.AllowedOperations)
        {
            var operation = Assert.Single(
                response.Operations,
                candidate => candidate.OperationId == descriptor.OperationId);
            Assert.Equal(descriptor.GrpcService, operation.GrpcService);
            Assert.Equal(descriptor.Rpc, operation.Rpc);
            Assert.Equal(
                descriptor.FullyQualifiedMethod,
                operation.FullyQualifiedMethod);
            Assert.Equal(
                descriptor.Effect,
                operation.Effect);
            Assert.Equal(descriptor.ExecutionScope, operation.ExecutionScope);
            Assert.Equal(descriptor.ReplaySafety, operation.ReplaySafety);
            Assert.Equal(OperationAdmission.Admitted, operation.Admission);
            AssertReviewedRow(operation);
        }
    }

    [Theory]
    [InlineData("file_operations.get_working_directory", OperationAdmission.Admitted,
        OperationDurationClass.Quick, OperationValidationStatus.NoRecordedGap,
        new[] { OperationRiskFlag.FilesystemMetadata })]
    [InlineData("analysis_operations.get_object_reporting_frame", OperationAdmission.Admitted,
        OperationDurationClass.Quick, OperationValidationStatus.FixturePending,
        new OperationRiskFlag[0])]
    [InlineData("file_operations.save_as", OperationAdmission.Admitted,
        OperationDurationClass.LongRunning, OperationValidationStatus.NoRecordedGap,
        new[] { OperationRiskFlag.FilesystemWrite })]
    [InlineData("file_operations.import_e57_file", OperationAdmission.Admitted,
        OperationDurationClass.LongRunning, OperationValidationStatus.FixturePending,
        new[] { OperationRiskFlag.FilesystemRead, OperationRiskFlag.FilesystemWrite })]
    [InlineData("utility_operations.delete_objects", OperationAdmission.Admitted,
        OperationDurationClass.Quick, OperationValidationStatus.NoRecordedGap,
        new[] { OperationRiskFlag.Destructive })]
    [InlineData("file_operations.delete_general_file", OperationAdmission.DeniedProfile,
        OperationDurationClass.Quick, OperationValidationStatus.NoRecordedGap,
        new[] { OperationRiskFlag.FilesystemDelete, OperationRiskFlag.Destructive })]
    [InlineData("mp_subroutines.run_subroutine", OperationAdmission.DeniedProfile,
        OperationDurationClass.LongRunning, OperationValidationStatus.FixturePending,
        new[] { OperationRiskFlag.FilesystemRead, OperationRiskFlag.CodeExecution })]
    [InlineData("instrument_operations.point_at_target", OperationAdmission.DeniedProfile,
        OperationDurationClass.LongRunning, OperationValidationStatus.NoRecordedGap,
        new[] { OperationRiskFlag.FilesystemRead, OperationRiskFlag.PhysicalMotion, OperationRiskFlag.DeviceSession })]
    [InlineData("instrument_operations.set_instrument_measurement_mode_profile", OperationAdmission.DeniedProfile,
        OperationDurationClass.Quick, OperationValidationStatus.NoRecordedGap,
        new[] { OperationRiskFlag.DeviceConfig })]
    [InlineData("process_flow_operations.ask_for_double", OperationAdmission.DeniedProfile,
        OperationDurationClass.Interactive, OperationValidationStatus.NoRecordedGap,
        new[] { OperationRiskFlag.InteractiveUi })]
    [InlineData("utility_operations.get_opc_da_tag_value_string", OperationAdmission.DeniedProfile,
        OperationDurationClass.Quick, OperationValidationStatus.FixturePending,
        new[] { OperationRiskFlag.ExternalIo })]
    [InlineData("instrument_operations.start_instrument_interface", OperationAdmission.DeniedExclusive,
        OperationDurationClass.LongRunning, OperationValidationStatus.NoRecordedGap,
        new[] { OperationRiskFlag.DeviceSession, OperationRiskFlag.ExternalIo })]
    public void CapabilitiesReportTheReviewedRowAndStandardAdmission(
        string operationId,
        OperationAdmission admission,
        OperationDurationClass duration,
        OperationValidationStatus validationStatus,
        OperationRiskFlag[] riskFlags)
    {
        var response = CreateService(CreatePolicy()).CreateCapabilities(includeDenied: true);

        var operation = Assert.Single(response.Operations, candidate => candidate.OperationId == operationId);
        Assert.Equal(admission, operation.Admission);
        Assert.Equal(duration, operation.DurationClass);
        Assert.Equal(validationStatus, operation.ValidationStatus);
        Assert.Equal(riskFlags, operation.RiskFlags);
        Assert.Equal(
            admission == OperationAdmission.DeniedExclusive
                ? global::Briosa.OperationExecutionScope.ExclusiveWorkflow
                : SpatialAnalyzerApi.Operations.Single(descriptor => descriptor.OperationId == operationId).ExecutionScope,
            operation.ExecutionScope);
    }

    [Fact]
    public void IncludeDeniedListsEveryRegisteredOperationWithItsReviewedRowAndDenialReason()
    {
        var policy = CreatePolicy();
        var service = CreateService(policy);

        var admitted = service.CreateCapabilities();
        var all = service.CreateCapabilities(includeDenied: true);

        Assert.Equal(admitted.AdmissionProfile, all.AdmissionProfile);
        Assert.Equal(admitted.PolicyFingerprint, all.PolicyFingerprint);
        Assert.Equal(admitted.SpatialAnalyzerTarget, all.SpatialAnalyzerTarget);
        Assert.Equal(
            SpatialAnalyzerApi.Operations.Select(operation => operation.OperationId).Order(StringComparer.Ordinal),
            all.Operations.Select(operation => operation.OperationId));
        Assert.Equal(admitted.Operations, all.Operations.Where(operation => operation.Admission == OperationAdmission.Admitted));
        Assert.All(all.Operations, operation =>
        {
            AssertReviewedRow(operation);
            Assert.Equal(ServerDiscoveryService.ToAdmission(policy.Evaluate(operation.OperationId)), operation.Admission);
            Assert.Equal(
                OperationClassification.Find(operation.OperationId).Row!.Isolation == OperationIsolationClass.ExclusiveWorkflow,
                operation.Admission == OperationAdmission.DeniedExclusive);
            Assert.Equal(
                operation.Admission == OperationAdmission.DeniedExclusive,
                operation.ExecutionScope == global::Briosa.OperationExecutionScope.ExclusiveWorkflow);
        });
        Assert.Equal(
            [OperationAdmission.Admitted, OperationAdmission.DeniedProfile, OperationAdmission.DeniedExclusive],
            all.Operations.Select(operation => operation.Admission).Distinct().Order());
    }

    [Fact]
    public void ExcludingDeniedHidesEveryDeniedAndExclusiveOperation()
    {
        var request = new ListCapabilitiesRequest();
        Assert.False(request.IncludeDenied);

        var policy = CreatePolicy(settings: new()
        {
            ["Overrides:file_operations:get_working_directory"] = "deny",
            ["Overrides:instrument_operations:start_instrument_interface"] = "allow",
            ["Flags:filesystem_write"] = "deny"
        });
        var response = CreateService(policy).CreateCapabilities();

        Assert.Equal(
            policy.AllowedOperations.Select(operation => operation.OperationId),
            response.Operations.Select(operation => operation.OperationId));
        Assert.All(response.Operations, operation =>
        {
            Assert.Equal(OperationAdmission.Admitted, operation.Admission);
            Assert.NotEqual(global::Briosa.OperationExecutionScope.ExclusiveWorkflow, operation.ExecutionScope);
            Assert.DoesNotContain(OperationRiskFlag.FilesystemWrite, operation.RiskFlags);
        });
        Assert.DoesNotContain(response.Operations, operation =>
            operation.OperationId is "file_operations.get_working_directory" or
                "instrument_operations.start_instrument_interface");
    }

    [Fact]
    public void IncludeDeniedReportsOverrideFlagAndExclusiveDenials()
    {
        var policy = CreatePolicy(settings: new()
        {
            ["Overrides:file_operations:get_working_directory"] = "deny",
            ["Overrides:instrument_operations:start_instrument_interface"] = "allow",
            ["Overrides:mp_subroutines:run_subroutine"] = "allow",
            ["Flags:filesystem_write"] = "deny"
        });

        var response = CreateService(policy).CreateCapabilities(includeDenied: true);
        var operations = response.Operations.ToDictionary(operation => operation.OperationId, StringComparer.Ordinal);

        Assert.Equal(OperationAdmission.DeniedOverride, operations["file_operations.get_working_directory"].Admission);
        Assert.Equal(OperationAdmission.DeniedExclusive,
            operations["instrument_operations.start_instrument_interface"].Admission);
        Assert.Equal(OperationAdmission.Admitted, operations["mp_subroutines.run_subroutine"].Admission);
        Assert.Equal(OperationAdmission.DeniedFlag, operations["file_operations.save_as"].Admission);
        Assert.Equal(OperationAdmission.DeniedFlag,
            operations["analysis_operations.best_fit_transformation_group_to_group"].Admission);
        Assert.All(
            response.Operations.Where(operation =>
                operation.RiskFlags.Contains(OperationRiskFlag.FilesystemWrite) &&
                operation.Admission != OperationAdmission.DeniedExclusive),
            operation => Assert.Equal(OperationAdmission.DeniedFlag, operation.Admission));
    }

    [Fact]
    public async Task ListCapabilitiesRpcHonorsIncludeDenied()
    {
        var service = CreateService(CreatePolicy());
        var context = new InMemoryServerCallContext(
            "/briosa.DiscoveryService/ListCapabilities", deadline: null, CancellationToken.None);

        var admitted = await service.ListCapabilities(new ListCapabilitiesRequest(), context).ConfigureAwait(true);
        var all = await service.ListCapabilities(new ListCapabilitiesRequest { IncludeDenied = true }, context)
            .ConfigureAwait(true);

        Assert.Equal(service.CreateCapabilities(), admitted);
        Assert.Equal(service.CreateCapabilities(includeDenied: true), all);
        Assert.Equal(SpatialAnalyzerApi.Operations.Count, all.Operations.Count);
        Assert.True(admitted.Operations.Count < all.Operations.Count);
    }

    [Fact]
    public void UnreviewedOperationsAreReportedWithoutClassificationMetadata()
    {
        const string operationId = "file_operations.get_working_directory";
        var policy = OperationPolicyTests.CreatePolicy(
            profile: "full",
            settings: new() { ["Overrides:file_operations:get_working_directory"] = "allow" },
            classify: candidate => candidate == operationId
                ? OperationClassificationLookup.Unreviewed
                : OperationClassification.Find(candidate));

        var service = CreateService(policy);
        var operation = Assert.Single(
            service.CreateCapabilities(includeDenied: true).Operations,
            candidate => candidate.OperationId == operationId);

        Assert.Equal(OperationAdmission.DeniedUnreviewed, operation.Admission);
        Assert.Empty(operation.RiskFlags);
        Assert.Equal(OperationDurationClass.Unspecified, operation.DurationClass);
        Assert.Equal(OperationValidationStatus.Unspecified, operation.ValidationStatus);
        Assert.DoesNotContain(service.CreateCapabilities().Operations, candidate => candidate.OperationId == operationId);
    }

    [Theory]
    [InlineData("read-only", null)]
    [InlineData("standard", null)]
    [InlineData("standard", "allow")]
    [InlineData("device", null)]
    [InlineData("full", null)]
    [InlineData("full", "allow")]
    [InlineData("full", "deny")]
    public void EveryProfileProducesADescribableAdmissionForEveryOperation(string profile, string? interactive)
    {
        var policy = OperationPolicyTests.CreatePolicy(
            profile: profile,
            settings: interactive is null ? null : new() { ["Flags:interactive_ui"] = interactive });

        var service = CreateService(policy);
        var all = service.CreateCapabilities(includeDenied: true);

        Assert.Equal(profile, all.AdmissionProfile);
        Assert.Equal(SpatialAnalyzerApi.Operations.Count, all.Operations.Count);
        Assert.DoesNotContain(all.Operations, operation => operation.Admission == OperationAdmission.Unspecified);
        Assert.Equal(
            policy.AllowedOperations.Select(operation => operation.OperationId),
            all.Operations.Where(operation => operation.Admission == OperationAdmission.Admitted)
                .Select(operation => operation.OperationId));
        Assert.Equal(
            interactive == "allow",
            all.Operations.Any(operation =>
                operation.Admission == OperationAdmission.Admitted &&
                operation.RiskFlags.Contains(OperationRiskFlag.InteractiveUi)));
        Assert.Equal(
            interactive == "deny",
            all.Operations.Any(operation => operation.Admission == OperationAdmission.DeniedFlag));
    }

    [Theory]
    [InlineData(nameof(OperationPolicyDecisionKind.Allowed), "profile.standard", OperationAdmission.Admitted)]
    [InlineData(nameof(OperationPolicyDecisionKind.Allowed), "override", OperationAdmission.Admitted)]
    [InlineData(nameof(OperationPolicyDecisionKind.Denied), "profile.read-only", OperationAdmission.DeniedProfile)]
    [InlineData(nameof(OperationPolicyDecisionKind.Denied), "flag.code_execution", OperationAdmission.DeniedFlag)]
    [InlineData(nameof(OperationPolicyDecisionKind.Denied), "override", OperationAdmission.DeniedOverride)]
    [InlineData(nameof(OperationPolicyDecisionKind.Denied), "isolation", OperationAdmission.DeniedExclusive)]
    [InlineData(nameof(OperationPolicyDecisionKind.Denied), "classification", OperationAdmission.DeniedUnreviewed)]
    public void AdmissionFollowsThePrecedenceStepThatDecided(
        string kind,
        string rule,
        OperationAdmission expected)
    {
        var decision = new OperationPolicyDecision(
            Enum.Parse<OperationPolicyDecisionKind>(kind), "diagnostic", Operation: null, rule);

        Assert.Equal(expected, ServerDiscoveryService.ToAdmission(decision));
    }

    [Theory]
    [InlineData(nameof(OperationPolicyDecisionKind.Unsupported), "registry")]
    [InlineData(nameof(OperationPolicyDecisionKind.Denied), "registry")]
    [InlineData(nameof(OperationPolicyDecisionKind.Denied), "option.show_results_dialog/flag.interactive_ui")]
    public void RequestOnlyDecisionsHaveNoDiscoveryAdmission(string kind, string rule)
    {
        var decision = new OperationPolicyDecision(
            Enum.Parse<OperationPolicyDecisionKind>(kind), "diagnostic", Operation: null, rule);

        Assert.Throws<InvalidOperationException>(() => ServerDiscoveryService.ToAdmission(decision));
    }

    [Fact]
    public void PolicyFingerprintIsStableValueFreeAndTracksThePolicy()
    {
        var first = CreateService(CreatePolicy()).CreateCapabilities();
        var second = CreateService(CreatePolicy()).CreateCapabilities(includeDenied: true);

        Assert.Matches("^sha256:[0-9A-F]{64}$", first.PolicyFingerprint);
        Assert.Equal(first.PolicyFingerprint, second.PolicyFingerprint);
        Assert.Equal(CreatePolicy().Fingerprint, first.PolicyFingerprint);
        var variants = new[]
        {
            CreateService(OperationPolicyTests.CreatePolicy(profile: "full")).CreateCapabilities().PolicyFingerprint,
            CreateService(CreatePolicy(denyWorkingDirectory: true)).CreateCapabilities().PolicyFingerprint,
            CreateService(OperationPolicyTests.CreatePolicy(settings: new() { ["Flags:filesystem_write"] = "deny" }))
                .CreateCapabilities().PolicyFingerprint
        };
        Assert.DoesNotContain(first.PolicyFingerprint, variants);
        Assert.Equal(variants.Length, variants.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain("file_operations", variants[1], StringComparison.Ordinal);
        Assert.DoesNotContain("filesystem_write", variants[2], StringComparison.Ordinal);
    }

    [Fact]
    public void PolicyFingerprintAlgorithmIsPinned()
    {
        // One reviewed row and no options: a change to the canonical form, or to
        // the names of the duration and validation values it hashes, changes
        // every deployed fingerprint and must be deliberate.
        const string operationId = "file_operations.get_working_directory";
        var operation = SpatialAnalyzerApi.Operations.Single(candidate => candidate.OperationId == operationId);
        var row = new OperationClassificationRow(
            operationId,
            OperationRisks.FilesystemMetadata,
            OperationDurationClass.Quick,
            OperationValidationStatus.NoRecordedGap,
            OperationIsolationClass.Admissible);
        var policy = OperationPolicyTests.CreatePolicy(
            operations: [operation],
            classify: _ => new OperationClassificationLookup(OperationClassificationStatus.Reviewed, row),
            requestClassifier: new OperationRequestClassifier([]));

        var response = CreateService(policy).CreateCapabilities();

        Assert.Equal(
            "sha256:4889C014C50C63229C83A418CC5BE795ED6C2AA7C5FB6D87D43B977010E66E6A",
            policy.ClassificationFingerprint);
        Assert.Equal(
            "sha256:1CA661CA0F0A92D1F53E9C803FD605E50922F8A9721A1DFD60196976E2658277",
            response.PolicyFingerprint);
        Assert.Equal([OperationRiskFlag.FilesystemMetadata], Assert.Single(response.Operations).RiskFlags);
    }

    [Fact]
    public void RiskFlagWireValuesMatchTheReviewedVocabulary()
    {
        Assert.Equal(
            OperationRiskVocabulary.Members.Count + 1,
            Enum.GetValues<OperationRiskFlag>().Distinct().Count());

        var descriptor = global::Briosa.DiscoveryReflection.Descriptor.EnumTypes
            .Single(type => type.Name == nameof(OperationRiskFlag));
        foreach (var (risk, index) in OperationRiskVocabulary.Members.Select((risk, index) => (risk, index)))
        {
            var wire = OperationRiskVocabulary.GetWireFlag(risk);
            Assert.Equal(index + 1, (int)wire);
            Assert.Equal(
                $"OPERATION_RISK_FLAG_{OperationRiskVocabulary.GetName(risk).ToUpperInvariant()}",
                descriptor.FindValueByNumber((int)wire).Name);
        }

        Assert.Equal(
            OperationRiskVocabulary.Members.Select(OperationRiskVocabulary.GetWireFlag),
            OperationRiskVocabulary.GetWireFlags(OperationRiskVocabulary.All));
        Assert.Empty(OperationRiskVocabulary.GetWireFlags(OperationRisks.None));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OperationRiskVocabulary.GetWireFlag(OperationRisks.FilesystemRead | OperationRisks.FilesystemWrite));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OperationRiskVocabulary.GetWireFlags((OperationRisks)(1 << 20)));
    }

    [Fact]
    public void CapabilitiesExcludeOperationsDeniedByRuntimePolicy()
    {
        var response = new ServerDiscoveryService(
            new FakeWorkerStatusProvider(Snapshot(WorkerLifecycleState.Stopped, null)),
            new FakeBuildIdentityProvider(),
            CreatePolicy(denyWorkingDirectory: true))
            .CreateCapabilities();

        Assert.DoesNotContain(response.Operations, operation =>
            operation.OperationId == "file_operations.get_working_directory");
        Assert.Equal(CreatePolicy().AllowedOperations.Count - 1, response.Operations.Count);
    }

    [Fact]
    public void MutatingCapabilitiesCannotChangeOtherResponses()
    {
        var service = new ServerDiscoveryService(
            new FakeWorkerStatusProvider(Snapshot(WorkerLifecycleState.Stopped, null)),
            new FakeBuildIdentityProvider(),
            CreatePolicy());
        var expected = service.CreateCapabilities();
        var expectedIncludingDenied = service.CreateCapabilities(includeDenied: true);
        var modified = service.CreateCapabilities();
        modified.Operations[0].OperationId = "changed";
        modified.Operations[0].RiskFlags.Add(OperationRiskFlag.CodeExecution);
        modified.Operations.RemoveAt(modified.Operations.Count - 1);
        modified.SpatialAnalyzerTarget = "changed";
        modified.AdmissionProfile = "full";
        var modifiedIncludingDenied = service.CreateCapabilities(includeDenied: true);
        modifiedIncludingDenied.Operations[0].Admission = OperationAdmission.Admitted;
        modifiedIncludingDenied.PolicyFingerprint = "changed";

        Assert.Equal(expected, service.CreateCapabilities());
        Assert.Equal(expectedIncludingDenied, service.CreateCapabilities(includeDenied: true));
    }
    [Fact]
    public void BuildIdentityMatchesAssemblyProvenanceAndReviewedCoordinates()
    {
        var provider = new BuildIdentityProvider();

        var coordinates = provider.CreateVersionCoordinates();

        Assert.True(coordinates.HasBriosaVersion);
        Assert.Equal(System.Reflection.CustomAttributeExtensions
            .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(typeof(Program).Assembly)!
            .InformationalVersion, coordinates.BriosaVersion);
        Assert.Equal(ServerBuildIdentity.SourceRevision, coordinates.SourceRevision);
        Assert.Equal("briosa", coordinates.ProtocolPackage);
        Assert.Equal("2026.1.0529.7", coordinates.SpatialAnalyzerTarget);
        Assert.Equal(
            BuildIdentityProvider.InteropFingerprint,
            CommittedInteropProvenance.Load().ExpectedInteropFingerprint);
        Assert.Equal(
            BuildIdentityProvider.InteropFingerprint,
            coordinates.InteropFingerprint);
    }

    [Fact]
    public void CommittedInteropProvenanceHashesTheCommittedCanonicalApi()
    {
        var provenance = CommittedInteropProvenance.Load();

        var canonicalApi = File.ReadAllBytes(Path.Combine(
            provenance.InteropDirectory,
            provenance.CanonicalApiFileName));

        Assert.Equal(
            provenance.CanonicalApiSha256,
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(canonicalApi)));
    }

    [Fact]
    public void MutatingDiscoveryCoordinatesDoesNotChangeSubsequentResponses()
    {
        var provider = new BuildIdentityProvider();
        var expected = provider.CreateVersionCoordinates();
        var modified = provider.CreateVersionCoordinates();
        modified.BriosaVersion = "changed";
        modified.SpatialAnalyzerTarget = "changed";
        modified.SourceRevision = "changed";

        Assert.Equal(expected, provider.CreateVersionCoordinates());
    }
    private static OperationPolicy CreatePolicy(bool denyWorkingDirectory = false) =>
        OperationPolicyTests.CreatePolicy(
            profile: "standard",
            settings: denyWorkingDirectory
                ? new() { ["Overrides:file_operations:get_working_directory"] = "deny" }
                : null);

    private static OperationPolicy CreatePolicy(Dictionary<string, string?> settings) =>
        OperationPolicyTests.CreatePolicy(profile: "standard", settings: settings);

    private static ServerDiscoveryService CreateService(OperationPolicy policy) =>
        new(
            new FakeWorkerStatusProvider(Snapshot(WorkerLifecycleState.Stopped, null)),
            new FakeBuildIdentityProvider(),
            policy);

    // Discovery reports the reviewed row, never a request's effective classification.
    private static void AssertReviewedRow(OperationCapability operation)
    {
        var row = OperationClassification.Find(operation.OperationId).Row!;
        Assert.Equal(OperationRiskVocabulary.GetWireFlags(row.Risks), operation.RiskFlags);
        Assert.Equal(row.Duration, operation.DurationClass);
        Assert.Equal(row.ValidationStatus, operation.ValidationStatus);
        Assert.NotEqual(OperationDurationClass.Unspecified, operation.DurationClass);
        Assert.NotEqual(OperationValidationStatus.Unspecified, operation.ValidationStatus);
        Assert.DoesNotContain(OperationRiskFlag.Unspecified, operation.RiskFlags);
    }
    private static WorkerLifecycleSnapshot Snapshot(
        WorkerLifecycleState workerState,
        WorkerConnectionState? connectionState,
        WorkerExecutionReadinessState executionReadinessState =
            WorkerExecutionReadinessState.Unverified) =>
        new(
            workerState,
            Generation: 2,
            ProcessId: 9876,
            RecoveryCount: 1,
            WorkerTerminationKind.None,
            "sensitive-internal-diagnostic",
            connectionState is null
                ? null
                : new WorkerConnectionSnapshot(
                    connectionState.Value,
                    executionReadinessState,
                    StatusCode: 42,
                    Attempt: 1,
                    MaximumAttempts: 3,
                    "sensitive-connection-diagnostic",
                    DateTimeOffset.UtcNow),
            DateTimeOffset.UtcNow,
            MatchingIdentity(), AdmissionOpen: true);

    private static ExactTargetIdentitySnapshot MatchingIdentity() =>
        new(
            new ServerIdentityEvidence(
                "2026.1.0529.7",
                ServerIdentitySource.OperatorAttestation,
                ServerIdentityMatchState.ExactMatch),
            new ServerIdentityEvidence(
                "2026.1.0529.7",
                ServerIdentitySource.OperatorAttestation,
                ServerIdentityMatchState.ExactMatch));

    private static ExactTargetIdentitySnapshot UnavailableIdentity() =>
        new(
            new ServerIdentityEvidence(
                Version: null,
                ServerIdentitySource.Unavailable,
                ServerIdentityMatchState.Unavailable),
            new ServerIdentityEvidence(
                Version: null,
                ServerIdentitySource.Unavailable,
                ServerIdentityMatchState.Unavailable));

    private sealed class FakeWorkerStatusProvider(WorkerLifecycleSnapshot current) :
        IWorkerStatusProvider
    {
        public WorkerLifecycleSnapshot Current { get; set; } = current;
    }

    private sealed class FakeBuildIdentityProvider : IServerBuildIdentityProvider
    {
        public VersionCoordinates CreateVersionCoordinates() =>
            new()
            {
                BriosaVersion = "0.1.0-test",
                ProtocolPackage = "briosa",
                SpatialAnalyzerTarget = "2026.1.0529.7",
                InteropFingerprint = "sha256:test"
            };
    }
}
