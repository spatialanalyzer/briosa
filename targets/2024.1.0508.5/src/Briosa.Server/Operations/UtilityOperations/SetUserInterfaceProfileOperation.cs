using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetUserInterfaceProfileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_user_interface_profile", "Set User Interface Profile", "briosa.UtilityOperations",
        "SetUserInterfaceProfile", "/briosa.UtilityOperations/SetUserInterfaceProfile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetUserInterfaceProfileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Profile Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasProfileName ? request.ProfileName : "Default"), "SetStringArg"),
            new("Profile File Name (optional)", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.ProfileFileName, "profile_file_name"), "SetFilePathArg")
        ], []);
    }

    public static Api.SetUserInterfaceProfileResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
