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
        executor.ExecuteAsync(request, context, AddDoubleToNamedDoubleListVariableOperation.Descriptor,
            AddDoubleToNamedDoubleListVariableOperation.CreateCommand, AddDoubleToNamedDoubleListVariableOperation.OutputContracts,
            AddDoubleToNamedDoubleListVariableOperation.CreateResult);

    [OperationImplementation("variables.clear_named_double_list_variable")]
    public override Task<Api.ClearNamedDoubleListVariableResult> ClearNamedDoubleListVariable(
        Api.ClearNamedDoubleListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ClearNamedDoubleListVariableOperation.Descriptor,
            ClearNamedDoubleListVariableOperation.CreateCommand, ClearNamedDoubleListVariableOperation.OutputContracts,
            ClearNamedDoubleListVariableOperation.CreateResult);

    [OperationImplementation("variables.delete_variable")]
    public override Task<Api.DeleteVariableResult> DeleteVariable(
        Api.DeleteVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteVariableOperation.Descriptor,
            DeleteVariableOperation.CreateCommand, DeleteVariableOperation.OutputContracts,
            DeleteVariableOperation.CreateResult);

    [OperationImplementation("variables.delete_variables_wildcard_match")]
    public override Task<Api.DeleteVariablesWildcardMatchResult> DeleteVariablesWildcardMatch(
        Api.DeleteVariablesWildcardMatchRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteVariablesWildcardMatchOperation.Descriptor,
            DeleteVariablesWildcardMatchOperation.CreateCommand, DeleteVariablesWildcardMatchOperation.OutputContracts,
            DeleteVariablesWildcardMatchOperation.CreateResult);

    [OperationImplementation("variables.get_boolean_variable")]
    public override Task<Api.GetBooleanVariableResult> GetBooleanVariable(
        Api.GetBooleanVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetBooleanVariableOperation.Descriptor,
            GetBooleanVariableOperation.CreateCommand, GetBooleanVariableOperation.OutputContracts,
            GetBooleanVariableOperation.CreateResult);

    [OperationImplementation("variables.get_collection_object_name_variable")]
    public override Task<Api.GetCollectionObjectNameVariableResult> GetCollectionObjectNameVariable(
        Api.GetCollectionObjectNameVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCollectionObjectNameVariableOperation.Descriptor,
            GetCollectionObjectNameVariableOperation.CreateCommand, GetCollectionObjectNameVariableOperation.OutputContracts,
            GetCollectionObjectNameVariableOperation.CreateResult);

    [OperationImplementation("variables.get_collection_object_ref_list_variable")]
    public override Task<Api.GetCollectionObjectRefListVariableResult> GetCollectionObjectRefListVariable(
        Api.GetCollectionObjectRefListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCollectionObjectRefListVariableOperation.Descriptor,
            GetCollectionObjectRefListVariableOperation.CreateCommand, GetCollectionObjectRefListVariableOperation.OutputContracts,
            GetCollectionObjectRefListVariableOperation.CreateResult);

    [OperationImplementation("variables.get_double_variable")]
    public override Task<Api.GetDoubleVariableResult> GetDoubleVariable(
        Api.GetDoubleVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetDoubleVariableOperation.Descriptor,
            GetDoubleVariableOperation.CreateCommand, GetDoubleVariableOperation.OutputContracts,
            GetDoubleVariableOperation.CreateResult);

    [OperationImplementation("variables.get_integer_variable")]
    public override Task<Api.GetIntegerVariableResult> GetIntegerVariable(
        Api.GetIntegerVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetIntegerVariableOperation.Descriptor,
            GetIntegerVariableOperation.CreateCommand, GetIntegerVariableOperation.OutputContracts,
            GetIntegerVariableOperation.CreateResult);

    [OperationImplementation("variables.get_named_double_list_variable")]
    public override Task<Api.GetNamedDoubleListVariableResult> GetNamedDoubleListVariable(
        Api.GetNamedDoubleListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetNamedDoubleListVariableOperation.Descriptor,
            GetNamedDoubleListVariableOperation.CreateCommand, GetNamedDoubleListVariableOperation.OutputContracts,
            GetNamedDoubleListVariableOperation.CreateResult);

    [OperationImplementation("variables.get_named_double_list_variable_min_max")]
    public override Task<Api.GetNamedDoubleListVariableMinMaxResult> GetNamedDoubleListVariableMinMax(
        Api.GetNamedDoubleListVariableMinMaxRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetNamedDoubleListVariableMinMaxOperation.Descriptor,
            GetNamedDoubleListVariableMinMaxOperation.CreateCommand, GetNamedDoubleListVariableMinMaxOperation.OutputContracts,
            GetNamedDoubleListVariableMinMaxOperation.CreateResult);

    [OperationImplementation("variables.get_point_name_ref_list_variable")]
    public override Task<Api.GetPointNameRefListVariableResult> GetPointNameRefListVariable(
        Api.GetPointNameRefListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPointNameRefListVariableOperation.Descriptor,
            GetPointNameRefListVariableOperation.CreateCommand, GetPointNameRefListVariableOperation.OutputContracts,
            GetPointNameRefListVariableOperation.CreateResult);

    [OperationImplementation("variables.get_point_name_variable")]
    public override Task<Api.GetPointNameVariableResult> GetPointNameVariable(
        Api.GetPointNameVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPointNameVariableOperation.Descriptor,
            GetPointNameVariableOperation.CreateCommand, GetPointNameVariableOperation.OutputContracts,
            GetPointNameVariableOperation.CreateResult);

    [OperationImplementation("variables.get_relationship_ref_list_variable")]
    public override Task<Api.GetRelationshipRefListVariableResult> GetRelationshipRefListVariable(
        Api.GetRelationshipRefListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRelationshipRefListVariableOperation.Descriptor,
            GetRelationshipRefListVariableOperation.CreateCommand, GetRelationshipRefListVariableOperation.OutputContracts,
            GetRelationshipRefListVariableOperation.CreateResult);

    [OperationImplementation("variables.get_report_items_reference_list_variable")]
    public override Task<Api.GetReportItemsReferenceListVariableResult> GetReportItemsReferenceListVariable(
        Api.GetReportItemsReferenceListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetReportItemsReferenceListVariableOperation.Descriptor,
            GetReportItemsReferenceListVariableOperation.CreateCommand, GetReportItemsReferenceListVariableOperation.OutputContracts,
            GetReportItemsReferenceListVariableOperation.CreateResult);

    [OperationImplementation("variables.get_string_ref_list_variable")]
    public override Task<Api.GetStringRefListVariableResult> GetStringRefListVariable(
        Api.GetStringRefListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetStringRefListVariableOperation.Descriptor,
            GetStringRefListVariableOperation.CreateCommand, GetStringRefListVariableOperation.OutputContracts,
            GetStringRefListVariableOperation.CreateResult);

    [OperationImplementation("variables.get_string_variable")]
    public override Task<Api.GetStringVariableResult> GetStringVariable(
        Api.GetStringVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetStringVariableOperation.Descriptor,
            GetStringVariableOperation.CreateCommand, GetStringVariableOperation.OutputContracts,
            GetStringVariableOperation.CreateResult);

    [OperationImplementation("variables.get_transform_variable")]
    public override Task<Api.GetTransformVariableResult> GetTransformVariable(
        Api.GetTransformVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetTransformVariableOperation.Descriptor,
            GetTransformVariableOperation.CreateCommand, GetTransformVariableOperation.OutputContracts,
            GetTransformVariableOperation.CreateResult);

    [OperationImplementation("variables.get_vector_name_ref_list_variable")]
    public override Task<Api.GetVectorNameRefListVariableResult> GetVectorNameRefListVariable(
        Api.GetVectorNameRefListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetVectorNameRefListVariableOperation.Descriptor,
            GetVectorNameRefListVariableOperation.CreateCommand, GetVectorNameRefListVariableOperation.OutputContracts,
            GetVectorNameRefListVariableOperation.CreateResult);

    [OperationImplementation("variables.get_vector_variable")]
    public override Task<Api.GetVectorVariableResult> GetVectorVariable(
        Api.GetVectorVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetVectorVariableOperation.Descriptor,
            GetVectorVariableOperation.CreateCommand, GetVectorVariableOperation.OutputContracts,
            GetVectorVariableOperation.CreateResult);

    [OperationImplementation("variables.set_boolean_variable")]
    public override Task<Api.SetBooleanVariableResult> SetBooleanVariable(
        Api.SetBooleanVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetBooleanVariableOperation.Descriptor,
            SetBooleanVariableOperation.CreateCommand, SetBooleanVariableOperation.OutputContracts,
            SetBooleanVariableOperation.CreateResult);

    [OperationImplementation("variables.set_collection_object_name_variable")]
    public override Task<Api.SetCollectionObjectNameVariableResult> SetCollectionObjectNameVariable(
        Api.SetCollectionObjectNameVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCollectionObjectNameVariableOperation.Descriptor,
            SetCollectionObjectNameVariableOperation.CreateCommand, SetCollectionObjectNameVariableOperation.OutputContracts,
            SetCollectionObjectNameVariableOperation.CreateResult);

    [OperationImplementation("variables.set_collection_object_ref_list_variable")]
    public override Task<Api.SetCollectionObjectRefListVariableResult> SetCollectionObjectRefListVariable(
        Api.SetCollectionObjectRefListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCollectionObjectRefListVariableOperation.Descriptor,
            SetCollectionObjectRefListVariableOperation.CreateCommand, SetCollectionObjectRefListVariableOperation.OutputContracts,
            SetCollectionObjectRefListVariableOperation.CreateResult);

    [OperationImplementation("variables.set_double_variable")]
    public override Task<Api.SetDoubleVariableResult> SetDoubleVariable(
        Api.SetDoubleVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetDoubleVariableOperation.Descriptor,
            SetDoubleVariableOperation.CreateCommand, SetDoubleVariableOperation.OutputContracts,
            SetDoubleVariableOperation.CreateResult);

    [OperationImplementation("variables.set_font_variable")]
    public override Task<Api.SetFontVariableResult> SetFontVariable(
        Api.SetFontVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetFontVariableOperation.Descriptor,
            SetFontVariableOperation.CreateCommand, SetFontVariableOperation.OutputContracts,
            SetFontVariableOperation.CreateResult);

    [OperationImplementation("variables.set_integer_variable")]
    public override Task<Api.SetIntegerVariableResult> SetIntegerVariable(
        Api.SetIntegerVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetIntegerVariableOperation.Descriptor,
            SetIntegerVariableOperation.CreateCommand, SetIntegerVariableOperation.OutputContracts,
            SetIntegerVariableOperation.CreateResult);

    [OperationImplementation("variables.set_named_double_list_variable")]
    public override Task<Api.SetNamedDoubleListVariableResult> SetNamedDoubleListVariable(
        Api.SetNamedDoubleListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetNamedDoubleListVariableOperation.Descriptor,
            SetNamedDoubleListVariableOperation.CreateCommand, SetNamedDoubleListVariableOperation.OutputContracts,
            SetNamedDoubleListVariableOperation.CreateResult);

    [OperationImplementation("variables.set_point_name_ref_list_variable")]
    public override Task<Api.SetPointNameRefListVariableResult> SetPointNameRefListVariable(
        Api.SetPointNameRefListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetPointNameRefListVariableOperation.Descriptor,
            SetPointNameRefListVariableOperation.CreateCommand, SetPointNameRefListVariableOperation.OutputContracts,
            SetPointNameRefListVariableOperation.CreateResult);

    [OperationImplementation("variables.set_point_name_variable")]
    public override Task<Api.SetPointNameVariableResult> SetPointNameVariable(
        Api.SetPointNameVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetPointNameVariableOperation.Descriptor,
            SetPointNameVariableOperation.CreateCommand, SetPointNameVariableOperation.OutputContracts,
            SetPointNameVariableOperation.CreateResult);

    [OperationImplementation("variables.set_relationship_ref_list_variable")]
    public override Task<Api.SetRelationshipRefListVariableResult> SetRelationshipRefListVariable(
        Api.SetRelationshipRefListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipRefListVariableOperation.Descriptor,
            SetRelationshipRefListVariableOperation.CreateCommand, SetRelationshipRefListVariableOperation.OutputContracts,
            SetRelationshipRefListVariableOperation.CreateResult);

    [OperationImplementation("variables.set_report_items_reference_list_variable")]
    public override Task<Api.SetReportItemsReferenceListVariableResult> SetReportItemsReferenceListVariable(
        Api.SetReportItemsReferenceListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetReportItemsReferenceListVariableOperation.Descriptor,
            SetReportItemsReferenceListVariableOperation.CreateCommand, SetReportItemsReferenceListVariableOperation.OutputContracts,
            SetReportItemsReferenceListVariableOperation.CreateResult);

    [OperationImplementation("variables.set_string_ref_list_variable")]
    public override Task<Api.SetStringRefListVariableResult> SetStringRefListVariable(
        Api.SetStringRefListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetStringRefListVariableOperation.Descriptor,
            SetStringRefListVariableOperation.CreateCommand, SetStringRefListVariableOperation.OutputContracts,
            SetStringRefListVariableOperation.CreateResult);

    [OperationImplementation("variables.set_string_variable")]
    public override Task<Api.SetStringVariableResult> SetStringVariable(
        Api.SetStringVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetStringVariableOperation.Descriptor,
            SetStringVariableOperation.CreateCommand, SetStringVariableOperation.OutputContracts,
            SetStringVariableOperation.CreateResult);

    [OperationImplementation("variables.set_transform_variable")]
    public override Task<Api.SetTransformVariableResult> SetTransformVariable(
        Api.SetTransformVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetTransformVariableOperation.Descriptor,
            SetTransformVariableOperation.CreateCommand, SetTransformVariableOperation.OutputContracts,
            SetTransformVariableOperation.CreateResult);

    [OperationImplementation("variables.set_vector_name_ref_list_variable")]
    public override Task<Api.SetVectorNameRefListVariableResult> SetVectorNameRefListVariable(
        Api.SetVectorNameRefListVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetVectorNameRefListVariableOperation.Descriptor,
            SetVectorNameRefListVariableOperation.CreateCommand, SetVectorNameRefListVariableOperation.OutputContracts,
            SetVectorNameRefListVariableOperation.CreateResult);

    [OperationImplementation("variables.set_vector_variable")]
    public override Task<Api.SetVectorVariableResult> SetVectorVariable(
        Api.SetVectorVariableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetVectorVariableOperation.Descriptor,
            SetVectorVariableOperation.CreateCommand, SetVectorVariableOperation.OutputContracts,
            SetVectorVariableOperation.CreateResult);

}
