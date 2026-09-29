using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class PopPolyBayAnalysisWindowOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.pop_poly_bay_analysis_window", "Pop PolyBay Analysis Window", "briosa.FileOperations",
        "PopPolyBayAnalysisWindow", "/briosa.FileOperations/PopPolyBayAnalysisWindow", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.PopPolyBayAnalysisWindowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Materials File Path", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.MaterialsFilePath), "SetStringArg"),
                new("Bay File Path", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.BayFilePath), "SetStringArg")], []);
    }

    public static Api.PopPolyBayAnalysisWindowResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
