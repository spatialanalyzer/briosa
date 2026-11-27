using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class OffsetDirectionTypeMapper
{
    public static WorkerChoiceValue<WorkerOffsetDirectionTypeValue> ToMpValue(Api.OffsetDirectionType direction, string fieldName) => direction switch
    {
        Api.OffsetDirectionType.Both => new(WorkerOffsetDirectionTypeValue.Both),
        Api.OffsetDirectionType.PositiveOnly => new(WorkerOffsetDirectionTypeValue.PositiveOnly),
        Api.OffsetDirectionType.NegativeOnly => new(WorkerOffsetDirectionTypeValue.NegativeOnly),
        _ => throw RequestFieldViolation.SupportedChoice(fieldName)
    };
}
