using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GuideObjectsIn6dBasedOnPointMeasurementsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.guide_objects_in_6d_based_on_point_measurements", "Guide Objects in 6D based on Point Measurements",
        "briosa.InstrumentOperations", "GuideObjectsIn6dBasedOnPointMeasurements", "/briosa.InstrumentOperations/GuideObjectsIn6dBasedOnPointMeasurements",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.GuideObjectsIn6dBasedOnPointMeasurementsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Destination Group (goal)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.DestinationGroup, "destination_group", WorkerObjectTypeValue.PointGroup),
                "SetCollectionObjectNameArg2"),
            new("Moving Reference Group (attached to objects)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.MovingReferenceGroup, "moving_reference_group", WorkerObjectTypeValue.PointGroup),
                "SetCollectionObjectNameArg2"),
            new("Objects to Move", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ObjectsToMove, "objects_to_move"),
                "SetCollectionObjectNameRefListArg")
        };

        if (request.InitialSurveyGroup is not null)
        {
            inputs.Add(new("Initially surveyed Group (First Position Measurements - Optional)",
                WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.InitialSurveyGroup, "initial_survey_group", WorkerObjectTypeValue.PointGroup),
                "SetCollectionObjectNameArg2"));
        }
        if (request.PositionalTolerance is not null)
        {
            inputs.Add(new("Positional Tolerance - Optional", WorkerMpValueKind.ToleranceVectorOptions,
                ToleranceVectorOptionsMapper.Required(request.PositionalTolerance, "positional_tolerance"),
                "SetToleranceVectorOptionsArg"));
        }
        if (request.RotationalTolerance is not null)
        {
            inputs.Add(new("Rotational Tolerance - Optional", WorkerMpValueKind.ToleranceVectorOptions,
                ToleranceVectorOptionsMapper.Required(request.RotationalTolerance, "rotational_tolerance"),
                "SetToleranceVectorOptionsArg"));
        }

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.GuideObjectsIn6dBasedOnPointMeasurementsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
