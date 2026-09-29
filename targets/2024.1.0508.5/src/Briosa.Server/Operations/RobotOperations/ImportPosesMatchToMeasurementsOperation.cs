using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class ImportPosesMatchToMeasurementsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.import_poses_match_to_measurements", "Import Poses Match to Measurements",
        "briosa.RobotOperations", "ImportPosesMatchToMeasurements", "/briosa.RobotOperations/ImportPosesMatchToMeasurements",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportPosesMatchToMeasurementsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));

        var inputs = new List<WorkerMpInputArgument>
        {
            new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId), "SetColMachineIdArg"),
            new("Calibration Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.CalibrationName), "SetStringArg"),
            new("Point Names", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointNames, "point_names"), "SetPointNameRefListArg")
        };
        if (request.CsvJointSetFile is not null)
            inputs.Add(new("FilePath for CSV Joint Set File", WorkerMpValueKind.FileReference,
                new WorkerFileReferenceValue(request.CsvJointSetFile.Path, request.CsvJointSetFile.EmbeddedFile), "SetFilePathArg"));

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.ImportPosesMatchToMeasurementsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}