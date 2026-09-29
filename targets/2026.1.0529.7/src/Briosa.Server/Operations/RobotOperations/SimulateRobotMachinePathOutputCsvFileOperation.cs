using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class SimulateRobotMachinePathOutputCsvFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.simulate_robot_machine_path_output_csv_file", "Simulate Robot/Machine Path, Output CSV File",
        "briosa.RobotOperations", "SimulateRobotMachinePathOutputCsvFile", "/briosa.RobotOperations/SimulateRobotMachinePathOutputCsvFile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SimulateRobotMachinePathOutputCsvFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));

        var inputs = new List<WorkerMpInputArgument>
        {
            new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId), "SetColMachineIdArg"),
            new("Path Frames", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.PathFrames, "path_frames"), "SetCollectionObjectNameRefListArg")
        };
        if (request.OutputCsvFile is not null)
            inputs.Add(new("Output CSV File", WorkerMpValueKind.FileReference,
                new WorkerFileReferenceValue(request.OutputCsvFile.Path, request.OutputCsvFile.EmbeddedFile), "SetFilePathArg"));

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.SimulateRobotMachinePathOutputCsvFileResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}