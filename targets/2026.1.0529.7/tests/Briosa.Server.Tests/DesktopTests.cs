using System.Buffers.Binary;
using System.IO.Compression;
using System.Text.Json;
using Briosa.Desktop;
using Google.Protobuf;

namespace Briosa.Server.Tests;

public sealed class DesktopTests
{
    [Fact]
    public void ConnectionSetupRequiresIndependentCompleteEvidenceAndUsesOnlyChildEnvironment()
    {
        Assert.Throws<ArgumentException>(() => new DesktopIdentitySettings(ApplicationVersion: DesktopProtocol.Target).Validate());
        Assert.Throws<ArgumentException>(() => new DesktopIdentitySettings(ApplicationReference: "record").Validate());
        Assert.Throws<ArgumentException>(() => new DesktopIdentitySettings(ApplicationVersion: DesktopProtocol.Target, ApplicationReference: "line\nbreak").Validate());
        var settings = new DesktopIdentitySettings(ApplicationVersion: DesktopProtocol.Target, ApplicationReference: "local-verification-record");
        var start = new System.Diagnostics.ProcessStartInfo();
        var key = "Briosa__SpatialAnalyzer__Identity__ConnectedSpatialAnalyzer__OperatorAttestation__Version";
        start.Environment.Remove(key);
        settings.ApplyTo(start);
        Assert.Equal(DesktopProtocol.Target, start.Environment[key]);
        new DesktopIdentitySettings().ApplyTo(start);
        Assert.Equal(DesktopProtocol.Target, start.Environment[key]);
    }

    [Fact]
    public void MismatchedSdkCannotConnectAndMissingEvidenceDoesNotOfferFutileReconnect()
    {
        var sdk = new SpatialAnalyzerSdkLifecycleState { SdkState = SpatialAnalyzerSdkState.Running,
            SdkGeneration = 1, ConnectionState = SpatialAnalyzerConnectionState.Disconnected, RecoveryState = SpatialAnalyzerSdkRecoveryState.NotRequired };
        var info = new GetServerInfoResponse { ActivatedSdkIdentity = new() { Version = "2024.1.0508.5",
            Source = RuntimeIdentityEvidenceSource.RuntimeVerification, MatchState = RuntimeIdentityMatchState.Mismatch } };
        var state = DesktopState.FromReply(Reply(info, sdk));
        Assert.Equal("Version mismatch", state.Heading);
        Assert.False(state.CanConnect);
        Assert.True(state.CanStopSdk);
        info.ActivatedSdkIdentity.MatchState = RuntimeIdentityMatchState.ExactMatch;
        sdk.ConnectionState = SpatialAnalyzerConnectionState.Connected;
        Assert.False(DesktopState.FromReply(Reply(info, sdk)).CanReconnect);
        info.ConnectedSpatialAnalyzerIdentity = new() { MatchState = RuntimeIdentityMatchState.ExactMatch };
        sdk.ConnectionState = SpatialAnalyzerConnectionState.Faulted;
        Assert.True(DesktopState.FromReply(Reply(info, sdk)).CanReconnect);
        Assert.Contains("Attachment succeeded", DesktopActionFeedback.Rejected("runtime-identity-not-ready"), StringComparison.Ordinal);
    }

    [Fact]
    public void AttachmentAndPartialReadinessNeverProduceReady()
    {
        var sdk = new SpatialAnalyzerSdkLifecycleState { SdkState = SpatialAnalyzerSdkState.Running,
            ConnectionState = SpatialAnalyzerConnectionState.Connected, ReadyForMp = false };
        var info = new GetServerInfoResponse { ReadyForMp = true };
        var state = DesktopState.FromReply(Reply(info, sdk));
        Assert.False(state.Ready);
        Assert.Equal("Version identity unavailable", state.Heading);
        sdk.ReadyForMp = true;
        Assert.True(DesktopState.FromReply(Reply(info, sdk)).Ready);
        Assert.False(DesktopState.FromReply(Reply(info, sdk) with { HostState = "Stopping" }).Ready);
        Assert.False(DesktopState.FromReply(Reply(info, sdk)).Unavailable().Ready);
    }

    [Fact]
    public void MonitorCannotUseLifecycleControlsAndUnknownOutcomePersistsAlongsideReadiness()
    {
        var sdk = new SpatialAnalyzerSdkLifecycleState { SdkState = SpatialAnalyzerSdkState.Ready, ReadyForMp = true,
            SdkGeneration = 7, RecoveryState = SpatialAnalyzerSdkRecoveryState.RecoveryAvailable,
            LastIncident = new SpatialAnalyzerSdkIncident { ExecutionDisposition = ExecutionDisposition.StartedOutcomeUnknown } };
        var state = DesktopState.FromReply(Reply(new() { ReadyForMp = true }, sdk) with { CanManage = false });
        Assert.True(state.Ready);
        Assert.True(state.OutcomeUnknown);
        Assert.True(state.NeedsAttention);
        Assert.False(state.CanStartSdk);
        Assert.False(state.CanConnect);
        Assert.False(state.CanRecover);
        Assert.False(state.CanStopSdk);
        Assert.Equal("Ready — earlier command outcome unknown", state.Heading);
    }

    [Fact]
    public void CuratedActivityRejectsWrongInstancesAndDiscardsRawText()
    {
        var instance = DesktopProtocol.NewInstance();
        var json = LogRecord(instance);
        var entry = ActivityReader.Parse(json, instance);
        Assert.NotNull(entry);
        Assert.Equal("Execution", entry.Category);
        Assert.Contains("StartedOutcomeUnknown", entry.Details, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET", JsonSerializer.Serialize(entry), StringComparison.Ordinal);
        Assert.Null(ActivityReader.Parse(json, DesktopProtocol.NewInstance()));
        Assert.Null(ActivityReader.Parse(json.Replace("ExecutionResolved", "SECRET", StringComparison.Ordinal), instance));
        Assert.Null(ActivityReader.Parse("{partial", instance));
        Assert.Null(ActivityReader.Parse(new string('x', 17 * 1024), instance));
    }

    [Fact]
    public void RestartedServersDoNotInheritCachedActivityFromThePreviousInstance()
    {
        var directory = Path.Combine(Path.GetTempPath(), "briosa-activity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var first = DesktopProtocol.NewInstance();
        var second = DesktopProtocol.NewInstance();
        var path = Path.Combine(directory, "briosa-" + first + "-20260915.jsonl");
        try
        {
            File.WriteAllText(path, LogRecord(first) + "\n");
            var reader = new ActivityReader();
            reader.ReadRecent(directory, first);
            Assert.Single(reader.Entries);
            reader.ReadRecent(directory, second);
            Assert.Empty(reader.Entries);
            Assert.Equal("No log file is available yet.", reader.Status);
        }
        finally { File.Delete(path); Directory.Delete(directory); }
    }

    [Fact]
    public void SupportBundleContainsOnlyProjectedFieldsAndCanReplaceAnExport()
    {
        var directory = Path.Combine(Path.GetTempPath(), "briosa-desktop-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "support.zip");
        try
        {
            var state = DesktopState.FromReply(Reply(new()
            {
                Version = new() { BriosaVersion = "1.0.0", SourceRevision = "SECRET", InteropFingerprint = "SECRET" },
                ActivatedSdkIdentity = new() { Version = "C:\\SECRET" }
            }, new() { DiagnosticCode = "C:\\SECRET" }) with { LogDirectory = "C:\\SECRET", Endpoint = "http://127.0.0.1:1234" });
            var entry = ActivityReader.Parse(LogRecord("0123456789abcdef0123456789abcdef"), "0123456789abcdef0123456789abcdef")!;
            SupportBundle.Export(path, state, [entry], true, directory);
            SupportBundle.Export(path, state, [entry], true, directory);
            using var archive = ZipFile.OpenRead(path);
            Assert.Equal(2, archive.Entries.Count);
            foreach (var item in archive.Entries)
            {
                using var reader = new StreamReader(item.Open());
                var text = reader.ReadToEnd();
                Assert.DoesNotContain("SECRET", text, StringComparison.Ordinal);
                Assert.DoesNotContain("127.0.0.1", text, StringComparison.Ordinal);
                Assert.DoesNotContain("Credential", text, StringComparison.Ordinal);
            }
        }
        finally { File.Delete(path); Directory.Delete(directory); }
    }

    [Fact]
    public async Task FramingRejectsOversizeAndTruncatedMessages()
    {
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, DesktopProtocol.MaximumMessageBytes + 1);
        using var oversized = new MemoryStream(header);
        await Assert.ThrowsAsync<InvalidDataException>(() => DesktopProtocol.ReadAsync<DesktopRequest>(oversized, CancellationToken.None));
        using var truncated = new MemoryStream([1, 0, 0, 0]);
        await Assert.ThrowsAsync<EndOfStreamException>(() => DesktopProtocol.ReadAsync<DesktopRequest>(truncated, CancellationToken.None));
        using var stream = new MemoryStream();
        var request = new DesktopRequest(DesktopProtocol.NewInstance());
        await DesktopProtocol.WriteAsync(stream, request, CancellationToken.None);
        stream.Position = 0;
        Assert.Equal(request, await DesktopProtocol.ReadAsync<DesktopRequest>(stream, CancellationToken.None));
    }

    [Fact]
    public void OwnershipRequiresACompletePerLaunchCredential()
    {
        var credential = DesktopProtocol.NewCredential();
        Assert.True(DesktopProtocol.CredentialMatches(credential, credential));
        Assert.False(DesktopProtocol.CredentialMatches(null, null));
        Assert.False(DesktopProtocol.CredentialMatches(credential, ""));
        Assert.False(DesktopProtocol.CredentialMatches(credential, DesktopProtocol.NewCredential()));
        Assert.Throws<ArgumentException>(() => DesktopProtocol.PipeName("../../another-server"));
    }

    private static DesktopReply Reply(GetServerInfoResponse info, SpatialAnalyzerSdkLifecycleState sdk) => new()
    {
        Instance = DesktopProtocol.NewInstance(), Target = DesktopProtocol.Target, Endpoint = "http://127.0.0.1:50051",
        HostState = "Running", CanManage = true, ServerInfoJson = JsonFormatter.Default.Format(info),
        SdkStateJson = JsonFormatter.Default.Format(sdk), ApplicationStateJson = "{}"
    };

    private static string LogRecord(string instance) => JsonSerializer.Serialize(new
    {
        Timestamp = DateTimeOffset.UtcNow, Level = "Error", MessageTemplate = "ExecutionResolved",
        Exception = "SECRET", RenderedMessage = "C:\\SECRET",
        Properties = new
        {
            SchemaVersion = 1, ServerInstanceId = instance, SpatialAnalyzerTarget = DesktopProtocol.Target,
            ExecutionDisposition = "StartedOutcomeUnknown", DiagnosticCode = "worker-watchdog-timeout", Geometry = "SECRET",
            OperationId = "GetWorkingFrameProperties", CorrelationId = Guid.NewGuid(), MpResultCode = 2
        }
    });
}
