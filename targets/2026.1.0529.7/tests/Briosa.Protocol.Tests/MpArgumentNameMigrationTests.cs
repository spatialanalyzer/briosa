using Google.Protobuf;

namespace Briosa.Protocol.Tests;

public sealed class MpArgumentNameMigrationTests
{
    [Fact]
    public void AngleToleranceKeepsThePublishedFieldNumberAndExplicitZeroPresence()
    {
        // A v0.7 client encodes its old angle_tolerance_0_0_for_none name as
        // field 4, fixed64. Names never appear in this binary payload.
        byte[] oldZero = [0x21, 0, 0, 0, 0, 0, 0, 0, 0];
        var parsed = AngleBetweenLineAndPlaneRequest.Parser.ParseFrom(oldZero);
        Assert.True(parsed.HasAngleTolerance);
        Assert.Equal(0, parsed.AngleTolerance);
        Assert.Equal(oldZero, parsed.ToByteArray());

        var absent = AngleBetweenLineAndPlaneRequest.Parser.ParseFrom(Array.Empty<byte>());
        Assert.False(absent.HasAngleTolerance);
        Assert.Empty(absent.ToByteArray());

        // Explicit positive tolerances retain their value and wire identity.
        byte[] oldQuarter = [0x21, 0, 0, 0, 0, 0, 0, 0xd0, 0x3f];
        Assert.Equal(0.25, AngleBetweenLineAndPlaneRequest.Parser.ParseFrom(oldQuarter).AngleTolerance);
        Assert.Equal(oldQuarter, new AngleBetweenLineAndPlaneRequest { AngleTolerance = 0.25 }.ToByteArray());
    }

    [Fact]
    public void JsonUsesTheDocumentedNewName()
    {
        var request = new AngleBetweenLineAndPlaneRequest { AngleTolerance = 0 };
        Assert.Contains("\"angleTolerance\"", JsonFormatter.Default.Format(request), StringComparison.Ordinal);
        Assert.Throws<InvalidProtocolBufferException>(() => JsonParser.Default.Parse<AngleBetweenLineAndPlaneRequest>(
            "{\"angleTolerance00ForNone\":0}"));
    }

    [Fact]
    public void UnqualifiedTimeoutNamesOnlyChangeTheReviewedCommands()
    {
        Assert.NotNull(SetInstrumentInterfaceResponseTimeoutRequest.Descriptor.FindFieldByName("timeout"));
        Assert.NotNull(GetInstrumentInterfaceResponseTimeoutResult.Descriptor.FindFieldByName("timeout"));
        Assert.NotNull(ConfigureAndMeasureRequest.Descriptor.FindFieldByName("timeout_seconds"));
        Assert.NotNull(SetProbeOffsetFrameOnlineRequest.Descriptor.FindFieldByName("timeout_seconds"));
    }
}
