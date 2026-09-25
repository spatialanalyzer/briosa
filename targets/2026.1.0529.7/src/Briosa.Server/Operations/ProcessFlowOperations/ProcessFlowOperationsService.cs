using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ProcessFlowOperations;

internal sealed class ProcessFlowOperationsService(OperationExecutor executor)
    : Api.ProcessFlowOperations.ProcessFlowOperationsBase
{
    [OperationImplementation("process_flow_operations.ask_for_double")]
    public override Task<Api.AskForDoubleResult> AskForDouble(
        Api.AskForDoubleRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AskForDoubleOperation.Descriptor,
            AskForDoubleOperation.CreateCommand, AskForDoubleOperation.OutputContracts,
            AskForDoubleOperation.CreateResult);

    [OperationImplementation("process_flow_operations.ask_for_integer")]
    public override Task<Api.AskForIntegerResult> AskForInteger(
        Api.AskForIntegerRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AskForIntegerOperation.Descriptor,
            AskForIntegerOperation.CreateCommand, AskForIntegerOperation.OutputContracts,
            AskForIntegerOperation.CreateResult);

    [OperationImplementation("process_flow_operations.ask_for_point_name")]
    public override Task<Api.AskForPointNameResult> AskForPointName(
        Api.AskForPointNameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AskForPointNameOperation.Descriptor,
            AskForPointNameOperation.CreateCommand, AskForPointNameOperation.OutputContracts,
            AskForPointNameOperation.CreateResult);

    [OperationImplementation("process_flow_operations.ask_for_string")]
    public override Task<Api.AskForStringResult> AskForString(
        Api.AskForStringRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AskForStringOperation.Descriptor,
            AskForStringOperation.CreateCommand, AskForStringOperation.OutputContracts,
            AskForStringOperation.CreateResult);

    [OperationImplementation("process_flow_operations.ask_for_string_pull_down_version")]
    public override Task<Api.AskForStringPullDownVersionResult> AskForStringPullDownVersion(
        Api.AskForStringPullDownVersionRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AskForStringPullDownVersionOperation.Descriptor,
            AskForStringPullDownVersionOperation.CreateCommand, AskForStringPullDownVersionOperation.OutputContracts,
            AskForStringPullDownVersionOperation.CreateResult);

    [OperationImplementation("process_flow_operations.ask_for_user_decision_from_image")]
    public override Task<Api.AskForUserDecisionFromImageResult> AskForUserDecisionFromImage(
        Api.AskForUserDecisionFromImageRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AskForUserDecisionFromImageOperation.Descriptor,
            AskForUserDecisionFromImageOperation.CreateCommand, AskForUserDecisionFromImageOperation.OutputContracts,
            AskForUserDecisionFromImageOperation.CreateResult);

    [OperationImplementation("process_flow_operations.ask_for_user_decision_from_strings")]
    public override Task<Api.AskForUserDecisionFromStringsResult> AskForUserDecisionFromStrings(
        Api.AskForUserDecisionFromStringsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AskForUserDecisionFromStringsOperation.Descriptor,
            AskForUserDecisionFromStringsOperation.CreateCommand, AskForUserDecisionFromStringsOperation.OutputContracts,
            AskForUserDecisionFromStringsOperation.CreateResult);

    [OperationImplementation("process_flow_operations.object_existence_test_check_only")]
    public override Task<Api.ObjectExistenceTestCheckOnlyResult> ObjectExistenceTestCheckOnly(
        Api.ObjectExistenceTestCheckOnlyRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ObjectExistenceTestCheckOnlyOperation.Descriptor,
            ObjectExistenceTestCheckOnlyOperation.CreateCommand, ObjectExistenceTestCheckOnlyOperation.OutputContracts,
            ObjectExistenceTestCheckOnlyOperation.CreateResult);

}
