using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class ImportPosesMatchToFramesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.import_poses_match_to_frames", "Import Poses Match to Frames",
        "briosa.RobotOperations", "ImportPosesMatchToFrames", "/briosa.RobotOperations/ImportPosesMatchToFrames",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportPosesMatchToFramesRequest request)
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
            new("Frame Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.FrameNames, "frame_names"), "SetCollectionObjectNameRefListArg")
        };
        if (request.CsvJointSetFile is not null)
            inputs.Add(new("FilePath for CSV Joint Set File", WorkerMpValueKind.FileReference,
                new WorkerFileReferenceValue(request.CsvJointSetFile.Path, request.CsvJointSetFile.EmbeddedFile), "SetFilePathArg"));

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.ImportPosesMatchToFramesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}