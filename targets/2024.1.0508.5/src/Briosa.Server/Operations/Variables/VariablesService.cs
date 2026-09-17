using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal sealed class VariablesService(OperationExecutor executor)
    : Api.Variables.VariablesBase
{
    [OperationImplementation("variables.add_double_to_named_double_list_variable")]
    public override Task<Api.AddDoubleToNamedDoubleListVariableResult> AddDoubleToNamedDoubleListVariable(
        Api.AddDoubleToNamedDoubleListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddDoubleToNamedDoubleListVariableRequest, Api.AddDoubleToNamedDoubleListVariableResult>(
            executor,
            request,
            context,
            "variables.add_double_to_named_double_list_variable");

    [OperationImplementation("variables.clear_named_double_list_variable")]
    public override Task<Api.ClearNamedDoubleListVariableResult> ClearNamedDoubleListVariable(
        Api.ClearNamedDoubleListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ClearNamedDoubleListVariableRequest, Api.ClearNamedDoubleListVariableResult>(
            executor,
            request,
            context,
            "variables.clear_named_double_list_variable");

    [OperationImplementation("variables.delete_variable")]
    public override Task<Api.DeleteVariableResult> DeleteVariable(
        Api.DeleteVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteVariableRequest, Api.DeleteVariableResult>(
            executor,
            request,
            context,
            "variables.delete_variable");

    [OperationImplementation("variables.delete_variables_wildcard_match")]
    public override Task<Api.DeleteVariablesWildcardMatchResult> DeleteVariablesWildcardMatch(
        Api.DeleteVariablesWildcardMatchRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteVariablesWildcardMatchRequest, Api.DeleteVariablesWildcardMatchResult>(
            executor,
            request,
            context,
            "variables.delete_variables_wildcard_match");

    [OperationImplementation("variables.get_boolean_variable")]
    public override Task<Api.GetBooleanVariableResult> GetBooleanVariable(
        Api.GetBooleanVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetBooleanVariableRequest, Api.GetBooleanVariableResult>(
            executor,
            request,
            context,
            "variables.get_boolean_variable");

    [OperationImplementation("variables.get_collection_object_name_variable")]
    public override Task<Api.GetCollectionObjectNameVariableResult> GetCollectionObjectNameVariable(
        Api.GetCollectionObjectNameVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCollectionObjectNameVariableRequest, Api.GetCollectionObjectNameVariableResult>(
            executor,
            request,
            context,
            "variables.get_collection_object_name_variable");

    [OperationImplementation("variables.get_collection_object_ref_list_variable")]
    public override Task<Api.GetCollectionObjectRefListVariableResult> GetCollectionObjectRefListVariable(
        Api.GetCollectionObjectRefListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCollectionObjectRefListVariableRequest, Api.GetCollectionObjectRefListVariableResult>(
            executor,
            request,
            context,
            "variables.get_collection_object_ref_list_variable");

    [OperationImplementation("variables.get_double_variable")]
    public override Task<Api.GetDoubleVariableResult> GetDoubleVariable(
        Api.GetDoubleVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetDoubleVariableRequest, Api.GetDoubleVariableResult>(
            executor,
            request,
            context,
            "variables.get_double_variable");

    [OperationImplementation("variables.get_integer_variable")]
    public override Task<Api.GetIntegerVariableResult> GetIntegerVariable(
        Api.GetIntegerVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetIntegerVariableRequest, Api.GetIntegerVariableResult>(
            executor,
            request,
            context,
            "variables.get_integer_variable");

    [OperationImplementation("variables.get_named_double_list_variable")]
    public override Task<Api.GetNamedDoubleListVariableResult> GetNamedDoubleListVariable(
        Api.GetNamedDoubleListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetNamedDoubleListVariableRequest, Api.GetNamedDoubleListVariableResult>(
            executor,
            request,
            context,
            "variables.get_named_double_list_variable");

    [OperationImplementation("variables.get_named_double_list_variable_min_max")]
    public override Task<Api.GetNamedDoubleListVariableMinMaxResult> GetNamedDoubleListVariableMinMax(
        Api.GetNamedDoubleListVariableMinMaxRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetNamedDoubleListVariableMinMaxRequest, Api.GetNamedDoubleListVariableMinMaxResult>(
            executor,
            request,
            context,
            "variables.get_named_double_list_variable_min_max");

    [OperationImplementation("variables.get_point_name_ref_list_variable")]
    public override Task<Api.GetPointNameRefListVariableResult> GetPointNameRefListVariable(
        Api.GetPointNameRefListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPointNameRefListVariableRequest, Api.GetPointNameRefListVariableResult>(
            executor,
            request,
            context,
            "variables.get_point_name_ref_list_variable");

    [OperationImplementation("variables.get_point_name_variable")]
    public override Task<Api.GetPointNameVariableResult> GetPointNameVariable(
        Api.GetPointNameVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPointNameVariableRequest, Api.GetPointNameVariableResult>(
            executor,
            request,
            context,
            "variables.get_point_name_variable");

    [OperationImplementation("variables.get_relationship_ref_list_variable")]
    public override Task<Api.GetRelationshipRefListVariableResult> GetRelationshipRefListVariable(
        Api.GetRelationshipRefListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRelationshipRefListVariableRequest, Api.GetRelationshipRefListVariableResult>(
            executor,
            request,
            context,
            "variables.get_relationship_ref_list_variable");

    [OperationImplementation("variables.get_report_items_reference_list_variable")]
    public override Task<Api.GetReportItemsReferenceListVariableResult> GetReportItemsReferenceListVariable(
        Api.GetReportItemsReferenceListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetReportItemsReferenceListVariableRequest, Api.GetReportItemsReferenceListVariableResult>(
            executor,
            request,
            context,
            "variables.get_report_items_reference_list_variable");

    [OperationImplementation("variables.get_string_ref_list_variable")]
    public override Task<Api.GetStringRefListVariableResult> GetStringRefListVariable(
        Api.GetStringRefListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetStringRefListVariableRequest, Api.GetStringRefListVariableResult>(
            executor,
            request,
            context,
            "variables.get_string_ref_list_variable");

    [OperationImplementation("variables.get_string_variable")]
    public override Task<Api.GetStringVariableResult> GetStringVariable(
        Api.GetStringVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetStringVariableRequest, Api.GetStringVariableResult>(
            executor,
            request,
            context,
            "variables.get_string_variable");

    [OperationImplementation("variables.get_transform_variable")]
    public override Task<Api.GetTransformVariableResult> GetTransformVariable(
        Api.GetTransformVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetTransformVariableRequest, Api.GetTransformVariableResult>(
            executor,
            request,
            context,
            "variables.get_transform_variable");

    [OperationImplementation("variables.get_vector_name_ref_list_variable")]
    public override Task<Api.GetVectorNameRefListVariableResult> GetVectorNameRefListVariable(
        Api.GetVectorNameRefListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetVectorNameRefListVariableRequest, Api.GetVectorNameRefListVariableResult>(
            executor,
            request,
            context,
            "variables.get_vector_name_ref_list_variable");

    [OperationImplementation("variables.get_vector_variable")]
    public override Task<Api.GetVectorVariableResult> GetVectorVariable(
        Api.GetVectorVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetVectorVariableRequest, Api.GetVectorVariableResult>(
            executor,
            request,
            context,
            "variables.get_vector_variable");

    [OperationImplementation("variables.set_boolean_variable")]
    public override Task<Api.SetBooleanVariableResult> SetBooleanVariable(
        Api.SetBooleanVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetBooleanVariableRequest, Api.SetBooleanVariableResult>(
            executor,
            request,
            context,
            "variables.set_boolean_variable");

    [OperationImplementation("variables.set_collection_object_name_variable")]
    public override Task<Api.SetCollectionObjectNameVariableResult> SetCollectionObjectNameVariable(
        Api.SetCollectionObjectNameVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCollectionObjectNameVariableRequest, Api.SetCollectionObjectNameVariableResult>(
            executor,
            request,
            context,
            "variables.set_collection_object_name_variable");

    [OperationImplementation("variables.set_collection_object_ref_list_variable")]
    public override Task<Api.SetCollectionObjectRefListVariableResult> SetCollectionObjectRefListVariable(
        Api.SetCollectionObjectRefListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCollectionObjectRefListVariableRequest, Api.SetCollectionObjectRefListVariableResult>(
            executor,
            request,
            context,
            "variables.set_collection_object_ref_list_variable");

    [OperationImplementation("variables.set_double_variable")]
    public override Task<Api.SetDoubleVariableResult> SetDoubleVariable(
        Api.SetDoubleVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetDoubleVariableRequest, Api.SetDoubleVariableResult>(
            executor,
            request,
            context,
            "variables.set_double_variable");

    [OperationImplementation("variables.set_font_variable")]
    public override Task<Api.SetFontVariableResult> SetFontVariable(
        Api.SetFontVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetFontVariableRequest, Api.SetFontVariableResult>(
            executor,
            request,
            context,
            "variables.set_font_variable");

    [OperationImplementation("variables.set_integer_variable")]
    public override Task<Api.SetIntegerVariableResult> SetIntegerVariable(
        Api.SetIntegerVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetIntegerVariableRequest, Api.SetIntegerVariableResult>(
            executor,
            request,
            context,
            "variables.set_integer_variable");

    [OperationImplementation("variables.set_named_double_list_variable")]
    public override Task<Api.SetNamedDoubleListVariableResult> SetNamedDoubleListVariable(
        Api.SetNamedDoubleListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetNamedDoubleListVariableRequest, Api.SetNamedDoubleListVariableResult>(
            executor,
            request,
            context,
            "variables.set_named_double_list_variable");

    [OperationImplementation("variables.set_point_name_ref_list_variable")]
    public override Task<Api.SetPointNameRefListVariableResult> SetPointNameRefListVariable(
        Api.SetPointNameRefListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetPointNameRefListVariableRequest, Api.SetPointNameRefListVariableResult>(
            executor,
            request,
            context,
            "variables.set_point_name_ref_list_variable");

    [OperationImplementation("variables.set_point_name_variable")]
    public override Task<Api.SetPointNameVariableResult> SetPointNameVariable(
        Api.SetPointNameVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetPointNameVariableRequest, Api.SetPointNameVariableResult>(
            executor,
            request,
            context,
            "variables.set_point_name_variable");

    [OperationImplementation("variables.set_relationship_ref_list_variable")]
    public override Task<Api.SetRelationshipRefListVariableResult> SetRelationshipRefListVariable(
        Api.SetRelationshipRefListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipRefListVariableRequest, Api.SetRelationshipRefListVariableResult>(
            executor,
            request,
            context,
            "variables.set_relationship_ref_list_variable");

    [OperationImplementation("variables.set_report_items_reference_list_variable")]
    public override Task<Api.SetReportItemsReferenceListVariableResult> SetReportItemsReferenceListVariable(
        Api.SetReportItemsReferenceListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetReportItemsReferenceListVariableRequest, Api.SetReportItemsReferenceListVariableResult>(
            executor,
            request,
            context,
            "variables.set_report_items_reference_list_variable");

    [OperationImplementation("variables.set_string_ref_list_variable")]
    public override Task<Api.SetStringRefListVariableResult> SetStringRefListVariable(
        Api.SetStringRefListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetStringRefListVariableRequest, Api.SetStringRefListVariableResult>(
            executor,
            request,
            context,
            "variables.set_string_ref_list_variable");

    [OperationImplementation("variables.set_string_variable")]
    public override Task<Api.SetStringVariableResult> SetStringVariable(
        Api.SetStringVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetStringVariableRequest, Api.SetStringVariableResult>(
            executor,
            request,
            context,
            "variables.set_string_variable");

    [OperationImplementation("variables.set_transform_variable")]
    public override Task<Api.SetTransformVariableResult> SetTransformVariable(
        Api.SetTransformVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetTransformVariableRequest, Api.SetTransformVariableResult>(
            executor,
            request,
            context,
            "variables.set_transform_variable");

    [OperationImplementation("variables.set_vector_name_ref_list_variable")]
    public override Task<Api.SetVectorNameRefListVariableResult> SetVectorNameRefListVariable(
        Api.SetVectorNameRefListVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetVectorNameRefListVariableRequest, Api.SetVectorNameRefListVariableResult>(
            executor,
            request,
            context,
            "variables.set_vector_name_ref_list_variable");

    [OperationImplementation("variables.set_vector_variable")]
    public override Task<Api.SetVectorVariableResult> SetVectorVariable(
        Api.SetVectorVariableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetVectorVariableRequest, Api.SetVectorVariableResult>(
            executor,
            request,
            context,
            "variables.set_vector_variable");

}
