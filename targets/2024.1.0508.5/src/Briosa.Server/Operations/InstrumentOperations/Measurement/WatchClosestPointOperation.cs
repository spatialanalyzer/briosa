using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class WatchClosestPointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.watch_closest_point", "Watch Closest Point",
        "briosa.InstrumentOperations", "WatchClosestPoint",
        "/briosa.InstrumentOperations/WatchClosestPoint",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.WatchClosestPointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                    CollectionInstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Groups to Consider", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.GroupsToConsider, "groups_to_consider"),
                    "SetCollectionObjectNameRefListArg"),
                new("3 DOF Watch Window Properties", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.WatchWindowProperties,
                        "watch_window_properties"), "SetCollectionObjectNameArg2"),
                new("Measurement Mode", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasMeasurementMode ? request.MeasurementMode : string.Empty),
                    "SetStringArg"),
                new("Pause MP Until Closed", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasPauseMpUntilClosed && request.PauseMpUntilClosed), "SetBoolArg"),
                new("Window Top Left X Position", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasWindowTopLeftX ? request.WindowTopLeftX : 0), "SetIntegerArg"),
                new("Window Top Left Y Position", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasWindowTopLeftY ? request.WindowTopLeftY : 0), "SetIntegerArg"),
                new("Window Width", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasWindowWidth ? request.WindowWidth : 0), "SetIntegerArg"),
                new("Window Height", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasWindowHeight ? request.WindowHeight : 0), "SetIntegerArg")
            ], []);
    }

    public static Api.WatchClosestPointResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
