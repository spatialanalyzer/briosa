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
        executor.ExecuteAsync(request, context, AddAVectorToVectorNameRefListOperation.Descriptor,
            AddAVectorToVectorNameRefListOperation.CreateCommand, AddAVectorToVectorNameRefListOperation.OutputContracts,
            AddAVectorToVectorNameRefListOperation.CreateResult);

    [OperationImplementation("vector_operations.auto_range_and_set_vector_group_colorization_all")]
    public override Task<Api.AutoRangeAndSetVectorGroupColorizationAllResult> AutoRangeAndSetVectorGroupColorizationAll(
        Api.AutoRangeAndSetVectorGroupColorizationAllRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AutoRangeAndSetVectorGroupColorizationAllOperation.Descriptor,
            AutoRangeAndSetVectorGroupColorizationAllOperation.CreateCommand, AutoRangeAndSetVectorGroupColorizationAllOperation.OutputContracts,
            AutoRangeAndSetVectorGroupColorizationAllOperation.CreateResult);

    [OperationImplementation("vector_operations.auto_range_and_set_vector_group_colorization_selected")]
    public override Task<Api.AutoRangeAndSetVectorGroupColorizationSelectedResult> AutoRangeAndSetVectorGroupColorizationSelected(
        Api.AutoRangeAndSetVectorGroupColorizationSelectedRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AutoRangeAndSetVectorGroupColorizationSelectedOperation.Descriptor,
            AutoRangeAndSetVectorGroupColorizationSelectedOperation.CreateCommand, AutoRangeAndSetVectorGroupColorizationSelectedOperation.OutputContracts,
            AutoRangeAndSetVectorGroupColorizationSelectedOperation.CreateResult);

    [OperationImplementation("vector_operations.delete_ith_vector_from_vector_group")]
    public override Task<Api.DeleteIthVectorFromVectorGroupResult> DeleteIthVectorFromVectorGroup(
        Api.DeleteIthVectorFromVectorGroupRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteIthVectorFromVectorGroupOperation.Descriptor,
            DeleteIthVectorFromVectorGroupOperation.CreateCommand, DeleteIthVectorFromVectorGroupOperation.OutputContracts,
            DeleteIthVectorFromVectorGroupOperation.CreateResult);

    [OperationImplementation("vector_operations.delete_vector_by_name")]
    public override Task<Api.DeleteVectorByNameResult> DeleteVectorByName(
        Api.DeleteVectorByNameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteVectorByNameOperation.Descriptor,
            DeleteVectorByNameOperation.CreateCommand, DeleteVectorByNameOperation.OutputContracts,
            DeleteVectorByNameOperation.CreateResult);

    [OperationImplementation("vector_operations.delete_vectors")]
    public override Task<Api.DeleteVectorsResult> DeleteVectors(
        Api.DeleteVectorsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteVectorsOperation.Descriptor,
            DeleteVectorsOperation.CreateCommand, DeleteVectorsOperation.OutputContracts,
            DeleteVectorsOperation.CreateResult);

    [OperationImplementation("vector_operations.get_ith_vector_from_vector_group")]
    public override Task<Api.GetIthVectorFromVectorGroupResult> GetIthVectorFromVectorGroup(
        Api.GetIthVectorFromVectorGroupRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetIthVectorFromVectorGroupOperation.Descriptor,
            GetIthVectorFromVectorGroupOperation.CreateCommand, GetIthVectorFromVectorGroupOperation.OutputContracts,
            GetIthVectorFromVectorGroupOperation.CreateResult);

    [OperationImplementation("vector_operations.get_ith_vector_from_vector_name_ref_list")]
    public override Task<Api.GetIthVectorFromVectorNameRefListResult> GetIthVectorFromVectorNameRefList(
        Api.GetIthVectorFromVectorNameRefListRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetIthVectorFromVectorNameRefListOperation.Descriptor,
            GetIthVectorFromVectorNameRefListOperation.CreateCommand, GetIthVectorFromVectorNameRefListOperation.OutputContracts,
            GetIthVectorFromVectorNameRefListOperation.CreateResult);

    [OperationImplementation("vector_operations.get_number_of_vectors_in_vector_group")]
    public override Task<Api.GetNumberOfVectorsInVectorGroupResult> GetNumberOfVectorsInVectorGroup(
        Api.GetNumberOfVectorsInVectorGroupRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetNumberOfVectorsInVectorGroupOperation.Descriptor,
            GetNumberOfVectorsInVectorGroupOperation.CreateCommand, GetNumberOfVectorsInVectorGroupOperation.OutputContracts,
            GetNumberOfVectorsInVectorGroupOperation.CreateResult);

    [OperationImplementation("vector_operations.get_number_of_vectors_in_vector_name_ref_list")]
    public override Task<Api.GetNumberOfVectorsInVectorNameRefListResult> GetNumberOfVectorsInVectorNameRefList(
        Api.GetNumberOfVectorsInVectorNameRefListRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetNumberOfVectorsInVectorNameRefListOperation.Descriptor,
            GetNumberOfVectorsInVectorNameRefListOperation.CreateCommand, GetNumberOfVectorsInVectorNameRefListOperation.OutputContracts,
            GetNumberOfVectorsInVectorNameRefListOperation.CreateResult);

    [OperationImplementation("vector_operations.get_vector_from_vector_group_by_name")]
    public override Task<Api.GetVectorFromVectorGroupByNameResult> GetVectorFromVectorGroupByName(
        Api.GetVectorFromVectorGroupByNameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetVectorFromVectorGroupByNameOperation.Descriptor,
            GetVectorFromVectorGroupByNameOperation.CreateCommand, GetVectorFromVectorGroupByNameOperation.OutputContracts,
            GetVectorFromVectorGroupByNameOperation.CreateResult);

    [OperationImplementation("vector_operations.get_vector_group_properties")]
    public override Task<Api.GetVectorGroupPropertiesResult> GetVectorGroupProperties(
        Api.GetVectorGroupPropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetVectorGroupPropertiesOperation.Descriptor,
            GetVectorGroupPropertiesOperation.CreateCommand, GetVectorGroupPropertiesOperation.OutputContracts,
            GetVectorGroupPropertiesOperation.CreateResult);

    [OperationImplementation("vector_operations.set_vector_group_colorization_options_all")]
    public override Task<Api.SetVectorGroupColorizationOptionsAllResult> SetVectorGroupColorizationOptionsAll(
        Api.SetVectorGroupColorizationOptionsAllRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetVectorGroupColorizationOptionsAllOperation.Descriptor,
            SetVectorGroupColorizationOptionsAllOperation.CreateCommand, SetVectorGroupColorizationOptionsAllOperation.OutputContracts,
            SetVectorGroupColorizationOptionsAllOperation.CreateResult);

    [OperationImplementation("vector_operations.set_vector_group_colorization_options_selected")]
    public override Task<Api.SetVectorGroupColorizationOptionsSelectedResult> SetVectorGroupColorizationOptionsSelected(
        Api.SetVectorGroupColorizationOptionsSelectedRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetVectorGroupColorizationOptionsSelectedOperation.Descriptor,
            SetVectorGroupColorizationOptionsSelectedOperation.CreateCommand, SetVectorGroupColorizationOptionsSelectedOperation.OutputContracts,
            SetVectorGroupColorizationOptionsSelectedOperation.CreateResult);

    [OperationImplementation("vector_operations.sort_vectors")]
    public override Task<Api.SortVectorsResult> SortVectors(
        Api.SortVectorsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SortVectorsOperation.Descriptor,
            SortVectorsOperation.CreateCommand, SortVectorsOperation.OutputContracts,
            SortVectorsOperation.CreateResult);

}
