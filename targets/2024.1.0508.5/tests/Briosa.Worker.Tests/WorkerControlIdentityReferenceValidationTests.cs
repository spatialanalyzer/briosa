using Briosa.Worker.Control;

namespace Briosa.Worker.Tests;

public sealed class WorkerControlIdentityReferenceValidationTests
{
    [Fact]
    public void IncompleteCollectionObjectIsRejectedBeforeTransport()
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        var message = WorkerControlMessage.Execute(
            Guid.NewGuid(),
            new WorkerMpCommand(
                "incomplete-object",
                "Incomplete Object",
                [
                    new WorkerMpInputArgument("Object", WorkerMpValueKind.CollectionObjectName, new WorkerCollectionObjectNameValue(
                                "Collection",
                                "Object",
                                WorkerObjectTypeValue.Unspecified), sdkBinding: "SetCollectionObjectNameArg2")
                ],
                []));

        Assert.Throws<WorkerMessageRejectedException>(() => channel.Send(message));
        Assert.Equal(0, stream.Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(43)]
    public void InvalidCollectionItemTypeIsRejectedBeforeTransport(int rawValue)
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        var message = WorkerControlMessage.Execute(
            Guid.NewGuid(),
            new WorkerMpCommand(
                "invalid-item",
                "Invalid Item",
                [
                    new WorkerMpInputArgument("Item", WorkerMpValueKind.CollectionItemName, new WorkerCollectionItemNameValue(
                                "Collection",
                                "Item",
                                (WorkerItemTypeValue)rawValue), sdkBinding: "SetCollectionObjectNameArg2")
                ],
                []));

        Assert.Throws<WorkerMessageRejectedException>(() => channel.Send(message));
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public void EmptyReferenceListsRemainValidInputs()
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        var message = WorkerControlMessage.Execute(
            Guid.NewGuid(),
            new WorkerMpCommand(
                "empty-list",
                "Empty List",
                [
                    new WorkerMpInputArgument("Points", WorkerMpValueKind.PointNameList, new WorkerPointNameListValue([]), sdkBinding: "SetPointNameRefListArg")
                ],
                []));

        channel.Send(message);

        Assert.True(stream.Length > 0);
    }
}
