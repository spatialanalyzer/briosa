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
                    new WorkerMpInputArgument(
                        "Object",
                        WorkerMpValueKind.CollectionObjectName,
                        CollectionObjectNameValue:
                            new WorkerCollectionObjectNameValue(
                                "Collection",
                                "Object",
                                ObjectType: null),
                        SdkBinding: "SetCollectionObjectNameArg2")
                ],
                []));

        Assert.Throws<InvalidDataException>(() => channel.Send(message));
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
                    new WorkerMpInputArgument(
                        "Points",
                        WorkerMpValueKind.PointNameList,
                        PointNameListValue: new WorkerPointNameListValue([]),
                        SdkBinding: "SetPointNameRefListArg")
                ],
                []));

        channel.Send(message);

        Assert.True(stream.Length > 0);
    }
}