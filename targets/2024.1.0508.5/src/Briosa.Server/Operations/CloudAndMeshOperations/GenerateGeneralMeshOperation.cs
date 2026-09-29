using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class GenerateGeneralMeshOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.generate_general_mesh", "Generate General Mesh",
        "briosa.CloudAndMeshOperations", "GenerateGeneralMesh", "/briosa.CloudAndMeshOperations/GenerateGeneralMesh",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.GenerateGeneralMeshRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Output Mesh Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.OutputMeshName, "output_mesh_name", WorkerObjectTypeValue.ScanStripeMesh), "SetCollectionObjectNameArg2"),
            new("Clouds to Mesh", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.CloudsToMesh, "clouds_to_mesh", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameRefListArg"),
            new("Maximum Triangle Size", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMaximumTriangleSize ? request.MaximumTriangleSize : 0), "SetDoubleArg"),
            new("Smallest Hole Diameter", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasSmallestHoleDiameter ? request.SmallestHoleDiameter : 0), "SetDoubleArg"),
            new("Finalize", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasFinalize || request.Finalize), "SetBoolArg"),
            new("Use Scan Direction For Point Normal", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasUseScanDirectionForPointNormal || request.UseScanDirectionForPointNormal), "SetBoolArg")
        };
        if (request.JsonFile is not null)
            inputs.Add(new("JSON File(optional)", WorkerMpValueKind.Text, new WorkerTextValue(request.JsonFile.Path), "SetStringArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.GenerateGeneralMeshResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
