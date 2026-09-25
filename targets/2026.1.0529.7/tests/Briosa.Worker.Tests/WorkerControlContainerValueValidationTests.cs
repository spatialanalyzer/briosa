using Briosa.Worker.Control;

namespace Briosa.Worker.Tests;

public sealed class WorkerControlContainerValueValidationTests
{
    [Fact]
    public void ContainerValuesRoundTripWithoutComTypes()
    {
        using var stream = new MemoryStream();
        using var sender = new WorkerControlChannel(stream, leaveOpen: true);
        var transform = Enumerable.Range(0, 16).Select(value => (double)value).ToArray();
        var message = WorkerControlMessage.Execute(
            Guid.NewGuid(),
            new WorkerMpCommand(
                "container-values",
                "Container Values",
                [
                    new WorkerMpInputArgument("Array", WorkerMpValueKind.DoubleArray, new WorkerDoubleArrayValue([1d, 2d])),
                    new WorkerMpInputArgument("Edit", WorkerMpValueKind.EditText, new WorkerStringListValue(["A", ""])),
                    new WorkerMpInputArgument("Transform", WorkerMpValueKind.Transform, new WorkerTransformValue(transform)),
                    new WorkerMpInputArgument("World", WorkerMpValueKind.WorldTransform, new WorkerWorldTransformValue(new(transform), 0)),
                    new WorkerMpInputArgument("Color", WorkerMpValueKind.RgbColor, new WorkerRgbColorValue(0, 127, 255)),
                    new WorkerMpInputArgument("File", WorkerMpValueKind.FileReference, new WorkerFileReferenceValue("", false)),
                    new WorkerMpInputArgument("Angle", WorkerMpValueKind.AngularUnit, new WorkerAngularUnitChoice(WorkerAngularUnitValue.DegreesMinutesSeconds)),
                    new WorkerMpInputArgument("Distance", WorkerMpValueKind.DistanceUnit, new WorkerDistanceUnitChoice(WorkerDistanceUnitValue.UsSurveyFeet)),
                    new WorkerMpInputArgument("Temperature", WorkerMpValueKind.TemperatureUnit, new WorkerTemperatureUnitChoice(WorkerTemperatureUnitValue.Celsius)),
                    new WorkerMpInputArgument("Font", WorkerMpValueKind.Font, new WorkerFontValue("Segoe UI", 12, new(1, 2, 3)))
                ],
                [new("Array Result", WorkerMpValueKind.DoubleArray, ArraySize: 6)]));

        sender.Send(message);
        stream.Position = 0;
        using var receiver = new WorkerControlChannel(stream, leaveOpen: true);

        var roundTrip = receiver.Receive();
        var inputs = roundTrip.Command!.InputArguments;
        var output = Assert.Single(roundTrip.Command.OutputArguments);

        Assert.Equal(10, inputs.Count);
        Assert.Equal([1d, 2d], (inputs[0].Value as WorkerDoubleArrayValue)!.Values);
        Assert.Equal(["A", ""], (inputs[1].Value as WorkerStringListValue)!.Values);
        Assert.Equal(15d, (inputs[2].Value as WorkerTransformValue)!.Values[15]);
        Assert.Equal(0d, (inputs[3].Value as WorkerWorldTransformValue)!.ScaleFactor);
        Assert.Equal((byte)255, (inputs[4].Value as WorkerRgbColorValue)!.Blue);
        Assert.Equal("", (inputs[5].Value as WorkerFileReferenceValue)!.Path);
        Assert.False((inputs[5].Value as WorkerFileReferenceValue)!.EmbeddedFile);
        Assert.Equal(
            WorkerAngularUnitValue.DegreesMinutesSeconds,
            ((inputs[6].Value as WorkerAngularUnitChoice)?.Value));
        Assert.Equal("Segoe UI", (inputs[9].Value as WorkerFontValue)!.FontName);
        Assert.Equal(6, output.ArraySize);
        Assert.DoesNotContain(
            inputs.SelectMany(input => input.GetType().GetProperties()),
            property => property.PropertyType == typeof(object));
    }

    [Fact]
    public void TransformWithWrongElementCountIsRejectedBeforeTransport()
    {
        var message = CreateSingleInput(new WorkerMpInputArgument("Transform", WorkerMpValueKind.Transform, new WorkerTransformValue([1d, 2d])));

        AssertRejected(message);
    }

    [Fact]
    public void UnspecifiedUnitIsRejectedBeforeTransport()
    {
        var message = CreateSingleInput(new WorkerMpInputArgument("Units", WorkerMpValueKind.DistanceUnit, new WorkerDistanceUnitChoice(WorkerDistanceUnitValue.Unspecified)));

        AssertRejected(message);
    }

    [Fact]
    public void ArraySizeOnANonArrayOutputIsRejectedBeforeTransport()
    {
        var message = WorkerControlMessage.Execute(
            Guid.NewGuid(),
            new WorkerMpCommand(
                "invalid",
                "Invalid",
                [],
                [new("Value", WorkerMpValueKind.FloatingPoint, ArraySize: 3)]));

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

        Assert.Throws<WorkerMessageRejectedException>(() => channel.Send(message));
        Assert.Equal(0, stream.Length);
    }
}
