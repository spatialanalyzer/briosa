using Briosa.Worker.Control;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

public sealed class WorkerValueOwnershipTests
{
    [Fact]
    public void EveryCollectionFamilyOwnsItsStorage()
    {
        Verify<double, WorkerDoubleArrayValue>(1, 2, values => new(values), value => value.Values);
        Verify<double, WorkerTransformValue>(1, 2, values => new(values), value => value.Values);
        Verify<string, WorkerStringListValue>("original", "replacement", values => new(values), value => value.Values);
        Verify<WorkerPointNameValue, WorkerPointNameListValue>(new("c", "g", "p"), new("x", "y", "z"), values => new(values), value => value.Values);
        Verify<WorkerVectorNameValue, WorkerVectorNameListValue>(new("c", "g", "v"), new("x", "y", "z"), values => new(values), value => value.Values);
        Verify<WorkerCollectionInstrumentIdValue, WorkerCollectionInstrumentIdListValue>(new("c", 1), new("x", 2), values => new(values), value => value.Values);
        Verify<WorkerCollectionGroupNameValue, WorkerCollectionGroupNameListValue>(new("c", "g"), new("x", "y"), values => new(values), value => value.Values);
        Verify<WorkerCollectionObjectNameValue, WorkerCollectionObjectNameListValue>(new("c", "o", WorkerObjectTypeValue.Frame), new("x", "y", WorkerObjectTypeValue.Plane), values => new(values), value => value.Values);
        Verify<WorkerCollectionItemNameValue, WorkerCollectionItemNameListValue>(new("c", "i", WorkerItemTypeValue.Picture), new("x", "y", WorkerItemTypeValue.SaReport), values => new(values), value => value.Values);
        Verify<WorkerCollectionVectorGroupNameValue, WorkerCollectionVectorGroupNameListValue>(new("c", "v"), new("x", "y"), values => new(values), value => value.Values);
    }

    [Fact]
    public void CommandSharesOwnedValuesAndPreservesLegacyObjectChoice()
    {
        var values = new WorkerDoubleArrayValue([0, 1, 2]);
        var command = new WorkerMpCommand("ownership", "Ownership",
        [
            new WorkerMpInputArgument("Values", WorkerMpValueKind.DoubleArray, values),
            // The specialized choice uses the original zero-based enum domain.
            new WorkerMpInputArgument("Object", WorkerMpValueKind.ObjectType, WorkerChoiceFactory.FromOrdinal(WorkerMpValueKind.ObjectType, 11))
        ], [new("Result", WorkerMpValueKind.DoubleArray)]);
        Assert.Same(values, (command.InputArguments[0].Value as WorkerDoubleArrayValue));
        Assert.Equal(WorkerObjectTypeValue.Frame,
            Assert.IsType<WorkerChoiceValue<WorkerObjectTypeValue>>(command.InputArguments[1].Value).Value);
    }

    [Fact]
    public void ReferenceTypeParsingRejectsUnspecifiedAndUnknownText()
    {
        Assert.False(SdkSpecializedValueCodec.TryParseObjectType("Unspecified", out _));
        Assert.False(SdkSpecializedValueCodec.TryParseItemType("unreviewed item", out _));
        Assert.True(SdkSpecializedValueCodec.TryParseObjectType("Frame", out var frame));
        Assert.Equal(WorkerObjectTypeValue.Frame, frame);
        Assert.Throws<ArgumentOutOfRangeException>(() => SdkSpecializedValueCodec.ToSdkString(WorkerObjectTypeValue.Unspecified));
    }

    private static void Verify<T, TValue>(T original, T replacement,
        Func<IReadOnlyList<T>, TValue> create, Func<TValue, IReadOnlyList<T>> read)
    {
        T[] source = [original];
        var value = create(source);
        source[0] = replacement;
        Assert.Equal(original, Assert.Single(read(value)));
        Assert.NotSame(source, read(value));
        if (read(value) is IList<T> mutable)
            Assert.Throws<NotSupportedException>(() => mutable[0] = replacement);
    }
}
