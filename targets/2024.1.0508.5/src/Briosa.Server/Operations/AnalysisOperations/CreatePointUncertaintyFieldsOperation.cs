using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class CreatePointUncertaintyFieldsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.create_point_uncertainty_fields", "Create Point Uncertainty Fields",
        "briosa.AnalysisOperations", "CreatePointUncertaintyFields", "/briosa.AnalysisOperations/CreatePointUncertaintyFields",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CreatePointUncertaintyFieldsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Point Name List", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.PointNameList, "point_name_list"), "SetPointNameRefListArg"),
                new("Number of Samples", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasNumberOfSamples ? request.NumberOfSamples : 1000), "SetIntegerArg")
            ], []);
    }

    public static Api.CreatePointUncertaintyFieldsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
