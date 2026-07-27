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
                    new(
                        "Render",
                        WorkerMpValueKind.RenderModeType,
                        SpecializedEnumValue: new(2)),
                    new(
                        "Filter",
                        WorkerMpValueKind.AutoFilterProximitySettings,
                        AutoFilterProximitySettingsValue: new(
                            1, 2, 3, 4, 5, 6,
                            SurfaceProximityMode: 0,
                            PlanarProximityMode: 1,
                            RadialProximityMode: 2,
                            ProjectToPlane: true,
                            AssertPlaneBoundaries: false)),
                    new(
                        "Tolerance",
                        WorkerMpValueKind.ToleranceScalarOptions,
                        ToleranceScalarOptionsValue: new(
                            new(true, 1.25),
                            new(false, -2.5)))
                ],
                []));

        sender.Send(message);
        stream.Position = 0;
        using var receiver = new WorkerControlChannel(stream, leaveOpen: true);

        var inputs = receiver.Receive().Command!.InputArguments;

        Assert.Equal(2, inputs[0].SpecializedEnumValue!.Value);
        Assert.Equal(2, inputs[1].AutoFilterProximitySettingsValue!.RadialProximityMode);
        Assert.Equal(-2.5, inputs[2].ToleranceScalarOptionsValue!.Low.Value);
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
        var message = CreateSingleInput(new(
            "Render",
            WorkerMpValueKind.RenderModeType,
            SpecializedEnumValue: new(value)));

        AssertRejected(message);
    }

    [Fact]
    public void MissingSpecializedStructureComponentIsRejectedBeforeTransport()
    {
        var message = CreateSingleInput(new(
            "Tolerance",
            WorkerMpValueKind.ToleranceScalarOptions,
            ToleranceScalarOptionsValue: new(null!, new(false, -2.5))));

        AssertRejected(message);
    }

    [Fact]
    public void InvalidNestedSpecializedEnumIsRejectedBeforeTransport()
    {
        var message = CreateSingleInput(new(
            "Filter",
            WorkerMpValueKind.AutoFilterProximitySettings,
            AutoFilterProximitySettingsValue: new(
                1, 2, 3, 4, 5, 6,
                SurfaceProximityMode: 0,
                PlanarProximityMode: 1,
                RadialProximityMode: 3,
                ProjectToPlane: true,
                AssertPlaneBoundaries: false)));

        AssertRejected(message);
    }

    private static WorkerControlMessage CreateSingleInput(WorkerMpInputArgument input) =>
        WorkerControlMessage.Execute(
            Guid.NewGuid(),
            new WorkerMpCommand("invalid", "Invalid", [input], []));

    private static void AssertRejected(WorkerControlMessage message)
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);

        Assert.Throws<InvalidDataException>(() => channel.Send(message));
        Assert.Equal(0, stream.Length);
    }
}
