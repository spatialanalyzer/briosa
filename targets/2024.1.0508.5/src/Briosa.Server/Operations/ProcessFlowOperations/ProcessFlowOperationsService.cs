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
        MpOperationServiceExecutor.ExecuteAsync<Api.AskForDoubleRequest, Api.AskForDoubleResult>(
            executor,
            request,
            context,
            "process_flow_operations.ask_for_double");

    [OperationImplementation("process_flow_operations.ask_for_integer")]
    public override Task<Api.AskForIntegerResult> AskForInteger(
        Api.AskForIntegerRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AskForIntegerRequest, Api.AskForIntegerResult>(
            executor,
            request,
            context,
            "process_flow_operations.ask_for_integer");

    [OperationImplementation("process_flow_operations.ask_for_point_name")]
    public override Task<Api.AskForPointNameResult> AskForPointName(
        Api.AskForPointNameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AskForPointNameRequest, Api.AskForPointNameResult>(
            executor,
            request,
            context,
            "process_flow_operations.ask_for_point_name");

    [OperationImplementation("process_flow_operations.ask_for_string")]
    public override Task<Api.AskForStringResult> AskForString(
        Api.AskForStringRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AskForStringRequest, Api.AskForStringResult>(
            executor,
            request,
            context,
            "process_flow_operations.ask_for_string");

    [OperationImplementation("process_flow_operations.ask_for_string_pull_down_version")]
    public override Task<Api.AskForStringPullDownVersionResult> AskForStringPullDownVersion(
        Api.AskForStringPullDownVersionRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AskForStringPullDownVersionRequest, Api.AskForStringPullDownVersionResult>(
            executor,
            request,
            context,
            "process_flow_operations.ask_for_string_pull_down_version");

    [OperationImplementation("process_flow_operations.ask_for_user_decision_from_image")]
    public override Task<Api.AskForUserDecisionFromImageResult> AskForUserDecisionFromImage(
        Api.AskForUserDecisionFromImageRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AskForUserDecisionFromImageRequest, Api.AskForUserDecisionFromImageResult>(
            executor,
            request,
            context,
            "process_flow_operations.ask_for_user_decision_from_image");

    [OperationImplementation("process_flow_operations.ask_for_user_decision_from_strings")]
    public override Task<Api.AskForUserDecisionFromStringsResult> AskForUserDecisionFromStrings(
        Api.AskForUserDecisionFromStringsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AskForUserDecisionFromStringsRequest, Api.AskForUserDecisionFromStringsResult>(
            executor,
            request,
            context,
            "process_flow_operations.ask_for_user_decision_from_strings");

    [OperationImplementation("process_flow_operations.object_existence_test_check_only")]
    public override Task<Api.ObjectExistenceTestCheckOnlyResult> ObjectExistenceTestCheckOnly(
        Api.ObjectExistenceTestCheckOnlyRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ObjectExistenceTestCheckOnlyRequest, Api.ObjectExistenceTestCheckOnlyResult>(
            executor,
            request,
            context,
            "process_flow_operations.object_existence_test_check_only");

}
