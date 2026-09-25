using Briosa.Worker.Control;

namespace Briosa.Worker.Tests;

public sealed class WorkerControlSpecializedValueValidationTests
{
    [Fact]
    public void SpecializedValuesRoundTripWithoutComOrUntypedValues()
    {
        using var stream = new MemoryStream();
        using var sender = new WorkerControlChannel(stream, leaveOpen: true);
        var message = WorkerControlMessage.Execute(
            Guid.NewGuid(),
            new WorkerMpCommand(
                "specialized-values",
                "Specialized Values",
                [
                    new WorkerMpInputArgument("Render", WorkerMpValueKind.RenderModeType, new WorkerSpecializedEnumValue(2)),
                    new WorkerMpInputArgument("Filter", WorkerMpValueKind.AutoFilterProximitySettings, new WorkerAutoFilterProximitySettingsValue(
                            1, 2, 3, 4, 5, 6,
                            SurfaceProximityMode: 0,
                            PlanarProximityMode: 1,
                            RadialProximityMode: 2,
                            ProjectToPlane: true,
                            AssertPlaneBoundaries: false)),
                    new WorkerMpInputArgument("Tolerance", WorkerMpValueKind.ToleranceScalarOptions, new WorkerToleranceScalarOptionsValue(
                            new(true, 1.25),
                            new(false, -2.5))),
                    new WorkerMpInputArgument("Projection", WorkerMpValueKind.ProjectionOptions, new WorkerProjectionOptionsValue(
                            "Object To Probe Vectors",
                            true,
                            true,
                            1.5,
                            false,
                            0)),
                    new WorkerMpInputArgument("Point Delta", WorkerMpValueKind.PointDeltaReportOptions, new WorkerPointDeltaReportOptionsValue(
                            0,
                            "Single",
                            true,
                            true,
                            true,
                            true,
                            true,
                            true,
                            true,
                            false,
                            true,
                            true))
                ],
                []));

        sender.Send(message);
        stream.Position = 0;
        using var receiver = new WorkerControlChannel(stream, leaveOpen: true);

        var inputs = receiver.Receive().Command!.InputArguments;

        Assert.Equal(2, (inputs[0].Value as WorkerSpecializedEnumValue)!.Value);
        Assert.Equal(2, (inputs[1].Value as WorkerAutoFilterProximitySettingsValue)!.RadialProximityMode);
        Assert.Equal(-2.5, (inputs[2].Value as WorkerToleranceScalarOptionsValue)!.Low.Value);
        Assert.Equal(1.5, (inputs[3].Value as WorkerProjectionOptionsValue)!.OverrideTargetOffsetsValue);
        Assert.Equal("Single", (inputs[4].Value as WorkerPointDeltaReportOptionsValue)!.DetailsFormat);
        Assert.DoesNotContain(
            inputs.SelectMany(input => input.GetType().GetProperties()),
            property => property.PropertyType == typeof(object));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(int.MaxValue)]
    public void UnknownSpecializedEnumValueIsRejectedBeforeTransport(int value)
    {
        var message = CreateSingleInput(new WorkerMpInputArgument("Render", WorkerMpValueKind.RenderModeType, new WorkerSpecializedEnumValue(value)));

        AssertRejected(message);
    }

    [Theory]
    [InlineData(WorkerMpValueKind.AsciiImportFileFormat, 43, 44)]
    [InlineData(WorkerMpValueKind.AsciiFrameSetFormat, 7, 8)]
    [InlineData(WorkerMpValueKind.AxisIdentifier, 5, 6)]
    [InlineData(WorkerMpValueKind.WcfAxisIdentifier, 2, 3)]
    [InlineData(WorkerMpValueKind.VectorComponent, 3, 4)]
    [InlineData(WorkerMpValueKind.ObjectType, 25, 26)]
    [InlineData(WorkerMpValueKind.InstrumentType, 184, 185)]
    [InlineData(WorkerMpValueKind.SystemString, 10, 11)]
    [InlineData(WorkerMpValueKind.CompTechnique, 2, 3)]
    [InlineData(WorkerMpValueKind.DegreeOfFreedom, 2, 3)]
    [InlineData(WorkerMpValueKind.FitMethod, 1, 2)]
    [InlineData(WorkerMpValueKind.MeasuredSideForPlanarOffset, 2, 3)]
    [InlineData(WorkerMpValueKind.MeasuredSideForRadialOffset, 2, 3)]
    [InlineData(WorkerMpValueKind.MpDialogInteractionMode, 1, 2)]
    [InlineData(WorkerMpValueKind.MpInteractionMode, 2, 3)]
    [InlineData(WorkerMpValueKind.NormalDirection, 2, 3)]
    [InlineData(WorkerMpValueKind.SaInteractionMode, 2, 3)]
    [InlineData(WorkerMpValueKind.SlotType, 1, 2)]
    [InlineData(WorkerMpValueKind.SphereFitComputationMode, 2, 3)]
    [InlineData(WorkerMpValueKind.WindowState, 4, 5)]
    public void ExactEnumDomainBoundariesAreEnforced(
        WorkerMpValueKind kind,
        int maximumValidValue,
        int firstInvalidValue)
    {
        var valid = CreateSingleInput(new WorkerMpInputArgument("Value", kind, new WorkerSpecializedEnumValue(maximumValidValue)));
        var invalid = CreateSingleInput(new WorkerMpInputArgument("Value", kind, new WorkerSpecializedEnumValue(firstInvalidValue)));

        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        channel.Send(valid);

        Assert.True(stream.Length > 0);
        AssertRejected(invalid);
    }

    [Fact]
    public void EnhancedCloudGapIsRejectedBeforeWorkerTransport()
    {
        AssertRejected(CreateSingleInput(new WorkerMpInputArgument("Type", WorkerMpValueKind.ObjectType, new WorkerSpecializedEnumValue(4))));
    }

    [Fact]
    public void ReportOutputRequiresExactlyOneTypedDestination()
    {
        var external = CreateSingleInput(new WorkerMpInputArgument("Output", WorkerMpValueKind.ReportOutputOptions, new WorkerReportOutputOptionsValue(3, "report.pdf", null)));
        var embedded = CreateSingleInput(new WorkerMpInputArgument("Output", WorkerMpValueKind.ReportOutputOptions, new WorkerReportOutputOptionsValue(1, null, new("", "Report"))));
        var neither = CreateSingleInput(new WorkerMpInputArgument("Output", WorkerMpValueKind.ReportOutputOptions, new WorkerReportOutputOptionsValue(0, null, null)));
        var both = CreateSingleInput(new WorkerMpInputArgument("Output", WorkerMpValueKind.ReportOutputOptions, new WorkerReportOutputOptionsValue(0, "report.sar", new("Collection", "Report"))));

        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        channel.Send(external);
        channel.Send(embedded);

        AssertRejected(neither);
        AssertRejected(both);
    }
    [Fact]
    public void MissingSpecializedStructureComponentIsRejectedBeforeTransport()
    {
        var message = CreateSingleInput(new WorkerMpInputArgument("Tolerance", WorkerMpValueKind.ToleranceScalarOptions, new WorkerToleranceScalarOptionsValue(null!, new(false, -2.5))));

        AssertRejected(message);
    }

    [Fact]
    public void InvalidNestedSpecializedEnumIsRejectedBeforeTransport()
    {
        var message = CreateSingleInput(new WorkerMpInputArgument("Filter", WorkerMpValueKind.AutoFilterProximitySettings, new WorkerAutoFilterProximitySettingsValue(
                1, 2, 3, 4, 5, 6,
                SurfaceProximityMode: 0,
                PlanarProximityMode: 1,
                RadialProximityMode: 3,
                ProjectToPlane: true,
                AssertPlaneBoundaries: false)));

        AssertRejected(message);
    }

    [Fact]
    public void OmittedObjectTypeFallbackRoundTripsForCollectionObjectOutputs()
    {
        using var stream = new MemoryStream();
        using var sender = new WorkerControlChannel(stream, leaveOpen: true);
        sender.Send(CreateSingleOutput(new(
            "Working Frame",
            WorkerMpValueKind.CollectionObjectName,
            "GetCollectionObjectNameArg",
            WorkerObjectTypeValue.Frame)));
        stream.Position = 0;
        using var receiver = new WorkerControlChannel(stream, leaveOpen: true);

        var output = Assert.Single(receiver.Receive().Command!.OutputArguments);

        Assert.Equal(WorkerObjectTypeValue.Frame, output.ObjectTypeWhenOmitted);
    }

    [Fact]
    public void OmittedObjectTypeFallbackIsRejectedForInvalidOutputContracts()
    {
        AssertRejected(CreateSingleOutput(new(
            "Text",
            WorkerMpValueKind.Text,
            "GetStringArg",
            WorkerObjectTypeValue.Frame)));
        AssertRejected(CreateSingleOutput(new(
            "Object",
            WorkerMpValueKind.CollectionObjectName,
            "GetCollectionObjectNameArg",
            WorkerObjectTypeValue.Unspecified)));
    }

    private static WorkerControlMessage CreateSingleInput(WorkerMpInputArgument input) =>
        WorkerControlMessage.Execute(
            Guid.NewGuid(),
            new WorkerMpCommand("invalid", "Invalid", [input], []));

    private static WorkerControlMessage CreateSingleOutput(WorkerMpOutputArgument output) =>
        WorkerControlMessage.Execute(
            Guid.NewGuid(),
            new WorkerMpCommand("output", "Output", [], [output]));

    private static void AssertRejected(WorkerControlMessage message)
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);

        Assert.Throws<WorkerMessageRejectedException>(() => channel.Send(message));
        Assert.Equal(0, stream.Length);
    }
}
