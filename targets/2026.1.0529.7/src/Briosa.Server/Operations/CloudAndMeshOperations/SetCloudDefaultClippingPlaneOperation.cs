using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class SetCloudDefaultClippingPlaneOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.set_cloud_default_clipping_plane", "Set Cloud Default Clipping Plane",
        "briosa.CloudAndMeshOperations", "SetCloudDefaultClippingPlane", "/briosa.CloudAndMeshOperations/SetCloudDefaultClippingPlane",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetCloudDefaultClippingPlaneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var enabled = request.HasEnableCloudClipping && request.EnableCloudClipping;
        if (enabled && request.ReferenceObject is null)
            throw new ArgumentException("Request field 'reference_object' is required when cloud clipping is enabled.", nameof(request));

        var inputs = new List<WorkerMpInputArgument>
        {
            new("Enable Cloud Clipping?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(enabled), "SetBoolArg")
        };
        if (request.ReferenceObject is not null)
        {
            inputs.Add(new("Reference Object", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReferenceObject, "reference_object", WorkerObjectTypeValue.Any), "SetCollectionObjectNameArg2"));
        }

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.SetCloudDefaultClippingPlaneResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
