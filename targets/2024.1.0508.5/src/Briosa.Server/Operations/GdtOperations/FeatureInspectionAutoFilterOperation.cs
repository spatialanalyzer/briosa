using System.Collections.Immutable;
using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class FeatureInspectionAutoFilterOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.feature_inspection_auto_filter", "Feature Inspection Auto Filter",
        "briosa.GdtOperations", "FeatureInspectionAutoFilter", "/briosa.GdtOperations/FeatureInspectionAutoFilter",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.FeatureInspectionAutoFilterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.HasOffsetDirection || request.OffsetDirection == Api.OffsetDirectionType.Unspecified ||
            !Enum.IsDefined(request.OffsetDirection))
            throw new ArgumentException("Request field 'offset_direction' is required.", nameof(request));

        var points = request.PointNames.Select(point =>
            new WorkerPointNameValue(point.CollectionName, point.GroupName, point.TargetName)).ToImmutableArray();
        if (points.IsEmpty)
            throw new ArgumentException("Request field 'point_names' is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Names", WorkerMpValueKind.PointNameList, new WorkerPointNameListValue(points), "SetPointNameRefListArg"),
            new("Group Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.GroupNames, "group_names"), "SetCollectionObjectNameRefListArg"),
            new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.CloudNames, "cloud_names"), "SetCollectionObjectNameRefListArg"),
            new("Surface Offset", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasSurfaceOffset ? request.SurfaceOffset : 0.1), "SetDoubleArg"),
            new("Edge Offset", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasEdgeOffset ? request.EdgeOffset : 0.1), "SetDoubleArg"),
            new("Offset Direction", WorkerMpValueKind.OffsetDirectionType,
                WorkerChoiceFactory.FromOrdinal(WorkerMpValueKind.OffsetDirectionType, (int)request.OffsetDirection - 1),
                "SetOffsetDirectionTypeArg"),
            new("Include Pts within Cylinder Axis Proximity?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.IncludePointsWithinCylinderAxisProximity), "SetBoolArg"),
            new("Enforce Max Pts per Face in Output?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.EnforceMaxPointsPerFaceInOutput), "SetBoolArg"),
            new("Max Pts per Face", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.MaxPointsPerFace), "SetIntegerArg"),
            new("Feature Check Name List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.FeatureCheckNameList, "feature_check_name_list"),
                "SetCollectionObjectNameRefListArg"),
            new("Include Datums?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasIncludeDatums ? request.IncludeDatums : true), "SetBoolArg"),
            new("Create Cloud for Each Datum/Check", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.CreateCloudForEachDatumOrCheck), "SetBoolArg")
        ], []);
    }

    public static Api.FeatureInspectionAutoFilterResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
