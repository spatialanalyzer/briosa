using global::Briosa;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Server0100 = Briosa.Protocol.Tests.Legacy.Server0100;

namespace Briosa.Protocol.Tests;

/// <summary>
/// #400: server info gains the lifecycle bounds (field 12) and the configured
/// interaction mode (field 13). A reader of the Server 0.10.0 schema must keep every
/// field it knows; a newer reader must tell absent, finite, and unbounded apart.
/// </summary>
public sealed class LifecycleBoundsProtocolTests
{
    [Fact]
    public void ServerInfoCarriesTheNewFieldsAtTheirReviewedNumbers()
    {
        var lifecycle = GetServerInfoResponse.Descriptor.FindFieldByNumber(12);
        var mode = GetServerInfoResponse.Descriptor.FindFieldByNumber(13);

        Assert.Equal("lifecycle_bounds", lifecycle.Name);
        Assert.Equal(LifecycleBounds.Descriptor, lifecycle.MessageType);
        Assert.Equal("configured_spatial_analyzer_interaction_mode", mode.Name);
        Assert.Equal(FieldType.Enum, mode.FieldType);
        Assert.Equal("ConfiguredSpatialAnalyzerInteractionMode", mode.EnumType.Name);
        Assert.Equal(
            [
                "start_sdk_milliseconds",
                "launch_spatial_analyzer_milliseconds",
                "connect_spatial_analyzer_milliseconds",
                "recover_sdk_milliseconds",
                "stop_sdk_milliseconds",
                "close_owned_spatial_analyzer_milliseconds",
                "startup_with_launch_milliseconds",
                "startup_without_launch_milliseconds",
                "host_graceful_shutdown"
            ],
            LifecycleBounds.Descriptor.Fields.InFieldNumberOrder().Select(field => field.Name));
        Assert.Equal(
            Enumerable.Range(1, 9),
            LifecycleBounds.Descriptor.Fields.InFieldNumberOrder().Select(field => field.FieldNumber));
        Assert.All(
            LifecycleBounds.Descriptor.Fields.InFieldNumberOrder().Take(8),
            field =>
            {
                Assert.Equal(FieldType.UInt32, field.FieldType);
                Assert.True(field.HasPresence);
            });
        var finite = GracefulShutdownBound.Descriptor.FindFieldByNumber(1);
        var unbounded = GracefulShutdownBound.Descriptor.FindFieldByNumber(2);
        Assert.Equal("finite_milliseconds", finite.Name);
        Assert.Equal(FieldType.UInt64, finite.FieldType);
        Assert.Equal("unbounded", unbounded.Name);
        Assert.Equal(UnboundedShutdownWait.Descriptor, unbounded.MessageType);
        Assert.Equal("limit", finite.RealContainingOneof.Name);
        Assert.Same(finite.RealContainingOneof, unbounded.RealContainingOneof);
        Assert.Empty(UnboundedShutdownWait.Descriptor.Fields.InDeclarationOrder());
    }

    [Fact]
    public void AServer0100ReaderKeepsEveryKnownFieldOfANewReply()
    {
        var current = NewServerInfo();
        var bytes = current.ToByteArray();

        var old = Server0100.GetServerInfoResponse.Parser.ParseFrom(bytes);

        AssertKnownFieldsEqual(current, old);
        // The older reader keeps the new fields as unknown fields and writes them back unchanged.
        Assert.Equal(bytes, old.ToByteArray());
        Assert.Equal(current, GetServerInfoResponse.Parser.ParseFrom(old.ToByteArray()));
    }

    [Fact]
    public void ANewReaderSeesNoBoundsAndAnUnknownModeFromAServer0100Reply()
    {
        var old = Server0100.GetServerInfoResponse.Parser.ParseFrom(
            WithoutNewFields(NewServerInfo()).ToByteArray());

        var current = GetServerInfoResponse.Parser.ParseFrom(old.ToByteArray());

        Assert.Null(current.LifecycleBounds);
        Assert.Equal(
            ConfiguredSpatialAnalyzerInteractionMode.Unspecified,
            current.ConfiguredSpatialAnalyzerInteractionMode);
        AssertKnownFieldsEqual(current, old);
    }

    // The 0.10.0 Control Center parsed server info JSON strictly, so it cannot read a newer
    // server's reply; a reader that ignores unknown fields keeps every field it knows.
    [Fact]
    public void StrictServer0100JsonRejectsTheNewFieldsAndATolerantReaderKeepsTheRest()
    {
        var current = NewServerInfo();
        var json = JsonFormatter.Default.Format(current);

        Assert.Throws<InvalidProtocolBufferException>(
            () => Server0100.GetServerInfoResponse.Parser.ParseJson(json));
        var tolerant = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true))
            .Parse<Server0100.GetServerInfoResponse>(json);

        AssertKnownFieldsEqual(current, tolerant);
        Assert.Equal(current, GetServerInfoResponse.Parser.ParseJson(json));
    }

    [Fact]
    public void AbsentFiniteAndUnboundedStayDistinctOnTheWire()
    {
        var empty = LifecycleBounds.Parser.ParseFrom(new LifecycleBounds().ToByteArray());
        Assert.False(empty.HasStartSdkMilliseconds);
        Assert.Equal(0U, empty.StartSdkMilliseconds);
        Assert.Null(empty.HostGracefulShutdown);

        // An explicit zero is detectable; readers treat it as unavailable.
        var zero = LifecycleBounds.Parser.ParseFrom(
            new LifecycleBounds { StopSdkMilliseconds = 0 }.ToByteArray());
        Assert.True(zero.HasStopSdkMilliseconds);
        Assert.Equal(0U, zero.StopSdkMilliseconds);

        var unset = GracefulShutdownBound.Parser.ParseFrom(new GracefulShutdownBound().ToByteArray());
        Assert.Equal(GracefulShutdownBound.LimitOneofCase.None, unset.LimitCase);
        // A bare finite getter reads zero when the case is unset, so readers check the case.
        Assert.Equal(0UL, unset.FiniteMilliseconds);

        var finite = GracefulShutdownBound.Parser.ParseFrom(
            new GracefulShutdownBound { FiniteMilliseconds = 105_000 }.ToByteArray());
        Assert.Equal(GracefulShutdownBound.LimitOneofCase.FiniteMilliseconds, finite.LimitCase);
        Assert.Equal(105_000UL, finite.FiniteMilliseconds);

        // The marker has no payload but is still present after a round trip.
        var unbounded = GracefulShutdownBound.Parser.ParseFrom(
            new GracefulShutdownBound { Unbounded = new UnboundedShutdownWait() }.ToByteArray());
        Assert.Equal(GracefulShutdownBound.LimitOneofCase.Unbounded, unbounded.LimitCase);

        // An alternative that a later schema adds (here field 3) is no recognized case.
        var future = GracefulShutdownBound.Parser.ParseFrom([0x18, 0x01]);
        Assert.Equal(GracefulShutdownBound.LimitOneofCase.None, future.LimitCase);
    }

    // The configured mode maps to SaInteractionMode by name, never by number.
    [Fact]
    public void ConfiguredModeNamesMatchSaInteractionModeAndTheirNumbersDiffer()
    {
        var configured = DiscoveryReflection.Descriptor.EnumTypes
            .Single(type => type.Name == "ConfiguredSpatialAnalyzerInteractionMode");
        var sa = SpatialAnalyzerValuesReflection.Descriptor.EnumTypes
            .Single(type => type.Name == "SaInteractionMode");
        const string ConfiguredPrefix = "CONFIGURED_SPATIAL_ANALYZER_INTERACTION_MODE_";
        const string SaPrefix = "SA_INTERACTION_MODE_";

        Assert.Equal(
            ["UNSPECIFIED", "UNCHANGED", "SILENT", "AUTOMATIC", "MANUAL"],
            configured.Values.Select(value => value.Name[ConfiguredPrefix.Length..]));
        Assert.Equal([0, 1, 2, 3, 4], configured.Values.Select(value => value.Number));
        var differentNumbers = 0;
        foreach (var saValue in sa.Values.Where(value => value.Number != 0))
        {
            var match = configured.FindValueByName(ConfiguredPrefix + saValue.Name[SaPrefix.Length..]);
            Assert.NotNull(match);
            if (match.Number != saValue.Number) differentNumbers++;
        }

        Assert.Equal(3, differentNumbers);
    }

    private static GetServerInfoResponse NewServerInfo() => new()
    {
        Version = new VersionCoordinates
        {
            BriosaVersion = "0.10.1-test",
            ProtocolPackage = "briosa",
            SpatialAnalyzerTarget = "exact-target",
            InteropFingerprint = "sha256:test"
        },
        WorkerState = WorkerRuntimeState.Ready,
        SpatialAnalyzerConnectionState = SpatialAnalyzerConnectionState.Connected,
        ReadyForMp = true,
        ConnectedSpatialAnalyzerVersion = "attested",
        ConnectedSpatialAnalyzerVersionState = ConnectedSpatialAnalyzerVersionState.OperatorAttestedMatch,
        SpatialAnalyzerExecutionReadinessState = SpatialAnalyzerExecutionReadinessState.ExecutionReady,
        TargetIsolationMode = TargetIsolationMode.SingleTenant,
        ActivatedSdkIdentity = new RuntimeIdentityEvidence
        {
            Version = "observed",
            Source = RuntimeIdentityEvidenceSource.RuntimeVerification,
            MatchState = RuntimeIdentityMatchState.ExactMatch
        },
        ConnectedSpatialAnalyzerIdentity = new RuntimeIdentityEvidence
        {
            Version = "attested",
            Source = RuntimeIdentityEvidenceSource.OperatorAttestation,
            MatchState = RuntimeIdentityMatchState.ExactMatch
        },
        Compatibility = new CompatibilityContract { Major = 3, Revision = 0 },
        LifecycleBounds = new LifecycleBounds
        {
            StartSdkMilliseconds = 45_000,
            LaunchSpatialAnalyzerMilliseconds = 30_000,
            ConnectSpatialAnalyzerMilliseconds = 60_000,
            RecoverSdkMilliseconds = 55_000,
            StopSdkMilliseconds = 15_000,
            CloseOwnedSpatialAnalyzerMilliseconds = 30_000,
            StartupWithLaunchMilliseconds = 100_000,
            StartupWithoutLaunchMilliseconds = 70_000,
            HostGracefulShutdown = new GracefulShutdownBound { FiniteMilliseconds = 105_000 }
        },
        ConfiguredSpatialAnalyzerInteractionMode = ConfiguredSpatialAnalyzerInteractionMode.Silent
    };

    private static GetServerInfoResponse WithoutNewFields(GetServerInfoResponse response)
    {
        var copy = response.Clone();
        copy.LifecycleBounds = null;
        copy.ConfiguredSpatialAnalyzerInteractionMode = ConfiguredSpatialAnalyzerInteractionMode.Unspecified;
        return copy;
    }

    // Every field of the frozen schema, by number, has the same name, presence, and value.
    private static void AssertKnownFieldsEqual(GetServerInfoResponse current, IMessage old)
    {
        var oldFields = Server0100.GetServerInfoResponse.Descriptor.Fields.InFieldNumberOrder();
        Assert.Equal(Enumerable.Range(1, 11), oldFields.Select(field => field.FieldNumber));
        foreach (var oldField in oldFields)
        {
            var currentField = GetServerInfoResponse.Descriptor.FindFieldByNumber(oldField.FieldNumber);
            Assert.Equal(oldField.Name, currentField.Name);
            Assert.Equal(oldField.FieldType, currentField.FieldType);
            if (oldField.HasPresence)
                Assert.Equal(currentField.Accessor.HasValue(current), oldField.Accessor.HasValue(old));
            Assert.Equal(currentField.Accessor.GetValue(current), oldField.Accessor.GetValue(old));
        }
    }
}
