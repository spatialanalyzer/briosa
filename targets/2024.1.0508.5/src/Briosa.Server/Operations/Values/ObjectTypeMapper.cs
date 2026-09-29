using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class ObjectTypeMapper
{
    public static WorkerChoiceValue<WorkerObjectTypeValue> Required(Api.ObjectType value)
    {
        if (value == Api.ObjectType.Unspecified || !Enum.IsDefined(value))
            throw new ArgumentException("Object type must specify a supported value.", nameof(value));
        return new((WorkerObjectTypeValue)value);
    }

    public static WorkerMpValue Required(Api.ObjectType value, string fieldName)
    {
        if (value == Api.ObjectType.Unspecified || !Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(fieldName, "The object type is not supported by this SA target.");
        }

        return WorkerChoiceFactory.FromOrdinal(WorkerMpValueKind.ObjectType, (int)value - 1);
    }
}
