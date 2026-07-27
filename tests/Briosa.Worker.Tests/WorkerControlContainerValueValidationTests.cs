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
                    new("Array", WorkerMpValueKind.DoubleArray,
                        DoubleArrayValue: new([1d, 2d])),
                    new("Edit", WorkerMpValueKind.EditText,
                        StringListValue: new(["A", ""])),
                    new("Transform", WorkerMpValueKind.Transform,
                        TransformValue: new(transform)),
                    new("World", WorkerMpValueKind.WorldTransform,
                        WorldTransformValue: new(new(transform), 0)),
                    new("Color", WorkerMpValueKind.RgbColor,
                        RgbColorValue: new(0, 127, 255)),
                    new("File", WorkerMpValueKind.FileReference,
                        FileReferenceValue: new("", false)),
                    new("Angle", WorkerMpValueKind.AngularUnit,
                        AngularUnitValue: WorkerAngularUnitValue.DegreesMinutesSeconds),
                    new("Distance", WorkerMpValueKind.DistanceUnit,
                        DistanceUnitValue: WorkerDistanceUnitValue.UsSurveyFeet),
                    new("Temperature", WorkerMpValueKind.TemperatureUnit,
                        TemperatureUnitValue: WorkerTemperatureUnitValue.Celsius),
                    new("Font", WorkerMpValueKind.Font,
                        FontValue: new("Segoe UI", 12, new(1, 2, 3)))
                ],
                []));

        sender.Send(message);
        stream.Position = 0;
        using var receiver = new WorkerControlChannel(stream, leaveOpen: true);

        var roundTrip = receiver.Receive();
        var inputs = roundTrip.Command!.InputArguments;

        Assert.Equal(10, inputs.Count);
        Assert.Equal([1d, 2d], inputs[0].DoubleArrayValue!.Values);
        Assert.Equal(["A", ""], inputs[1].StringListValue!.Values);
        Assert.Equal(15d, inputs[2].TransformValue!.Values[15]);
        Assert.Equal(0d, inputs[3].WorldTransformValue!.ScaleFactor);
        Assert.Equal((byte)255, inputs[4].RgbColorValue!.Blue);
        Assert.Equal("", inputs[5].FileReferenceValue!.Path);
        Assert.False(inputs[5].FileReferenceValue!.EmbeddedFile);
        Assert.Equal(
            WorkerAngularUnitValue.DegreesMinutesSeconds,
            inputs[6].AngularUnitValue);
        Assert.Equal("Segoe UI", inputs[9].FontValue!.FontName);
        Assert.DoesNotContain(
            inputs.SelectMany(input => input.GetType().GetProperties()),
            property => property.PropertyType == typeof(object));
    }

    [Fact]
    public void TransformWithWrongElementCountIsRejectedBeforeTransport()
    {
        var message = CreateSingleInput(new WorkerMpInputArgument(
            "Transform",
            WorkerMpValueKind.Transform,
            TransformValue: new([1d, 2d])));

        AssertRejected(message);
    }

    [Fact]
    public void UnspecifiedUnitIsRejectedBeforeTransport()
    {
        var message = CreateSingleInput(new WorkerMpInputArgument(
            "Units",
            WorkerMpValueKind.DistanceUnit,
            DistanceUnitValue: WorkerDistanceUnitValue.Unspecified));

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