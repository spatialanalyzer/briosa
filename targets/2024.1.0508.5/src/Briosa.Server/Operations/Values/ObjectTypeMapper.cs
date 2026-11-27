using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class ObjectTypeMapper
{
    public static WorkerChoiceValue<WorkerObjectTypeValue> RequiredChoice(Api.ObjectType value, string fieldName)
    {
        if (value == Api.ObjectType.Unspecified || !Enum.IsDefined(value))
            throw RequestFieldViolation.SupportedChoice(fieldName);
        return new((WorkerObjectTypeValue)value);
    }

    public static WorkerMpValue Required(Api.ObjectType value, string fieldName)
    {
        if (value == Api.ObjectType.Unspecified || !Enum.IsDefined(value))
        {
            throw RequestFieldViolation.SupportedChoice(fieldName);
        }

        return WorkerChoiceFactory.FromOrdinal(WorkerMpValueKind.ObjectType, (int)value - 1);
    }
}
