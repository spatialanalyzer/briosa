using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal sealed class VectorOperationsService(OperationExecutor executor)
    : Api.VectorOperations.VectorOperationsBase
{
    [OperationImplementation("vector_operations.add_a_vector_to_vector_name_ref_list")]
    public override Task<Api.AddAVectorToVectorNameRefListResult> AddAVectorToVectorNameRefList(
        Api.AddAVectorToVectorNameRefListRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddAVectorToVectorNameRefListRequest, Api.AddAVectorToVectorNameRefListResult>(
            executor,
            request,
            context,
            "vector_operations.add_a_vector_to_vector_name_ref_list");

    [OperationImplementation("vector_operations.auto_range_and_set_vector_group_colorization_all")]
    public override Task<Api.AutoRangeAndSetVectorGroupColorizationAllResult> AutoRangeAndSetVectorGroupColorizationAll(
        Api.AutoRangeAndSetVectorGroupColorizationAllRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AutoRangeAndSetVectorGroupColorizationAllRequest, Api.AutoRangeAndSetVectorGroupColorizationAllResult>(
            executor,
            request,
            context,
            "vector_operations.auto_range_and_set_vector_group_colorization_all");

    [OperationImplementation("vector_operations.auto_range_and_set_vector_group_colorization_selected")]
    public override Task<Api.AutoRangeAndSetVectorGroupColorizationSelectedResult> AutoRangeAndSetVectorGroupColorizationSelected(
        Api.AutoRangeAndSetVectorGroupColorizationSelectedRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AutoRangeAndSetVectorGroupColorizationSelectedRequest, Api.AutoRangeAndSetVectorGroupColorizationSelectedResult>(
            executor,
            request,
            context,
            "vector_operations.auto_range_and_set_vector_group_colorization_selected");

    [OperationImplementation("vector_operations.delete_ith_vector_from_vector_group")]
    public override Task<Api.DeleteIthVectorFromVectorGroupResult> DeleteIthVectorFromVectorGroup(
        Api.DeleteIthVectorFromVectorGroupRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteIthVectorFromVectorGroupRequest, Api.DeleteIthVectorFromVectorGroupResult>(
            executor,
            request,
            context,
            "vector_operations.delete_ith_vector_from_vector_group");

    [OperationImplementation("vector_operations.delete_vector_by_name")]
    public override Task<Api.DeleteVectorByNameResult> DeleteVectorByName(
        Api.DeleteVectorByNameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteVectorByNameRequest, Api.DeleteVectorByNameResult>(
            executor,
            request,
            context,
            "vector_operations.delete_vector_by_name");

    [OperationImplementation("vector_operations.delete_vectors")]
    public override Task<Api.DeleteVectorsResult> DeleteVectors(
        Api.DeleteVectorsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteVectorsRequest, Api.DeleteVectorsResult>(
            executor,
            request,
            context,
            "vector_operations.delete_vectors");

    [OperationImplementation("vector_operations.get_ith_vector_from_vector_group")]
    public override Task<Api.GetIthVectorFromVectorGroupResult> GetIthVectorFromVectorGroup(
        Api.GetIthVectorFromVectorGroupRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetIthVectorFromVectorGroupRequest, Api.GetIthVectorFromVectorGroupResult>(
            executor,
            request,
            context,
            "vector_operations.get_ith_vector_from_vector_group");

    [OperationImplementation("vector_operations.get_ith_vector_from_vector_name_ref_list")]
    public override Task<Api.GetIthVectorFromVectorNameRefListResult> GetIthVectorFromVectorNameRefList(
        Api.GetIthVectorFromVectorNameRefListRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetIthVectorFromVectorNameRefListRequest, Api.GetIthVectorFromVectorNameRefListResult>(
            executor,
            request,
            context,
            "vector_operations.get_ith_vector_from_vector_name_ref_list");

    [OperationImplementation("vector_operations.get_number_of_vectors_in_vector_group")]
    public override Task<Api.GetNumberOfVectorsInVectorGroupResult> GetNumberOfVectorsInVectorGroup(
        Api.GetNumberOfVectorsInVectorGroupRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetNumberOfVectorsInVectorGroupRequest, Api.GetNumberOfVectorsInVectorGroupResult>(
            executor,
            request,
            context,
            "vector_operations.get_number_of_vectors_in_vector_group");

    [OperationImplementation("vector_operations.get_number_of_vectors_in_vector_name_ref_list")]
    public override Task<Api.GetNumberOfVectorsInVectorNameRefListResult> GetNumberOfVectorsInVectorNameRefList(
        Api.GetNumberOfVectorsInVectorNameRefListRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetNumberOfVectorsInVectorNameRefListRequest, Api.GetNumberOfVectorsInVectorNameRefListResult>(
            executor,
            request,
            context,
            "vector_operations.get_number_of_vectors_in_vector_name_ref_list");

    [OperationImplementation("vector_operations.get_vector_from_vector_group_by_name")]
    public override Task<Api.GetVectorFromVectorGroupByNameResult> GetVectorFromVectorGroupByName(
        Api.GetVectorFromVectorGroupByNameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetVectorFromVectorGroupByNameRequest, Api.GetVectorFromVectorGroupByNameResult>(
            executor,
            request,
            context,
            "vector_operations.get_vector_from_vector_group_by_name");

    [OperationImplementation("vector_operations.get_vector_group_properties")]
    public override Task<Api.GetVectorGroupPropertiesResult> GetVectorGroupProperties(
        Api.GetVectorGroupPropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetVectorGroupPropertiesRequest, Api.GetVectorGroupPropertiesResult>(
            executor,
            request,
            context,
            "vector_operations.get_vector_group_properties");

    [OperationImplementation("vector_operations.set_vector_group_colorization_options_all")]
    public override Task<Api.SetVectorGroupColorizationOptionsAllResult> SetVectorGroupColorizationOptionsAll(
        Api.SetVectorGroupColorizationOptionsAllRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetVectorGroupColorizationOptionsAllRequest, Api.SetVectorGroupColorizationOptionsAllResult>(
            executor,
            request,
            context,
            "vector_operations.set_vector_group_colorization_options_all");

    [OperationImplementation("vector_operations.set_vector_group_colorization_options_selected")]
    public override Task<Api.SetVectorGroupColorizationOptionsSelectedResult> SetVectorGroupColorizationOptionsSelected(
        Api.SetVectorGroupColorizationOptionsSelectedRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetVectorGroupColorizationOptionsSelectedRequest, Api.SetVectorGroupColorizationOptionsSelectedResult>(
            executor,
            request,
            context,
            "vector_operations.set_vector_group_colorization_options_selected");

    [OperationImplementation("vector_operations.sort_vectors")]
    public override Task<Api.SortVectorsResult> SortVectors(
        Api.SortVectorsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SortVectorsRequest, Api.SortVectorsResult>(
            executor,
            request,
            context,
            "vector_operations.sort_vectors");

}
