using Briosa.Server.Security;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;
using Ops = Briosa.Server.Operations;

namespace Briosa.LicensedProbes;

/// <summary>
/// The reviewed public RPCs used by the probe session. Each binds the generated
/// client call to the target's own shipped command builder.
/// </summary>
internal static class ProbeOperations
{
    // Fixture and guard operations.
    public static ProbeOperation GetNumberOfCollections { get; } = Op<Api.GetNumberOfCollectionsRequest, Api.GetNumberOfCollectionsResult>(
        Ops.AnalysisOperations.GetNumberOfCollectionsOperation.Descriptor,
        Ops.AnalysisOperations.GetNumberOfCollectionsOperation.CreateCommand,
        static (invoker, request, options) => Analysis(invoker).GetNumberOfCollectionsAsync(request, options),
        static result => result.Execution,
        static (_, result) => Observations.Of(("collection_count", Observations.Count(result.TotalCount))),
        static (_, outputs) => Observations.Of(("collection_count", Observations.Count(outputs[0].RequireValue<WorkerIntegerValue>().Value))));

    public static ProbeOperation ConstructCollection { get; } = Op<Api.ConstructCollectionRequest, Api.ConstructCollectionResult>(
        Ops.ConstructionOperations.ConstructCollectionOperation.Descriptor,
        Ops.ConstructionOperations.ConstructCollectionOperation.CreateCommand,
        static (invoker, request, options) => Construction(invoker).ConstructCollectionAsync(request, options),
        static result => result.Execution);

    public static ProbeOperation ObjectWildcardSelection { get; } = Op<Api.MakeCollectionObjectNameRefListWildcardSelectionRequest, Api.MakeCollectionObjectNameRefListWildcardSelectionResult>(
        Ops.ConstructionOperations.MakeCollectionObjectNameRefListWildcardSelectionOperation.Descriptor,
        Ops.ConstructionOperations.MakeCollectionObjectNameRefListWildcardSelectionOperation.CreateCommand,
        static (invoker, request, options) => Construction(invoker).MakeCollectionObjectNameRefListWildcardSelectionAsync(request, options),
        static result => result.Execution,
        static (_, result) => Observations.Of(("match_count", Observations.Count(result.ResultantCollectionObjectNameRefList.Count))),
        static (_, outputs) => Observations.Of(("match_count", Observations.Count(
            outputs[0].RequireValue<WorkerCollectionObjectNameListValue>().Values.Count))));

    public static ProbeOperation ConstructPoint { get; } = Op<Api.ConstructPointInWorkingCoordinatesRequest, Api.ConstructPointInWorkingCoordinatesResult>(
        Ops.ConstructionOperations.ConstructPointInWorkingCoordinatesOperation.Descriptor,
        Ops.ConstructionOperations.ConstructPointInWorkingCoordinatesOperation.CreateCommand,
        static (invoker, request, options) => Construction(invoker).ConstructPointInWorkingCoordinatesAsync(request, options),
        static result => result.Execution);

    public static ProbeOperation ConstructPointsOnGrid { get; } = Op<Api.ConstructPointsLayoutOnGridRequest, Api.ConstructPointsLayoutOnGridResult>(
        Ops.ConstructionOperations.ConstructPointsLayoutOnGridOperation.Descriptor,
        Ops.ConstructionOperations.ConstructPointsLayoutOnGridOperation.CreateCommand,
        static (invoker, request, options) => Construction(invoker).ConstructPointsLayoutOnGridAsync(request, options),
        static result => result.Execution);

    public static ProbeOperation ConstructCloudFromGroup { get; } = Op<Api.ConstructPointCloudsFromExistingPointGroupRequest, Api.ConstructPointCloudsFromExistingPointGroupResult>(
        Ops.ConstructionOperations.ConstructPointCloudsFromExistingPointGroupOperation.Descriptor,
        Ops.ConstructionOperations.ConstructPointCloudsFromExistingPointGroupOperation.CreateCommand,
        static (invoker, request, options) => Construction(invoker).ConstructPointCloudsFromExistingPointGroupAsync(request, options),
        static result => result.Execution);

    public static ProbeOperation ConstructPlane { get; } = Op<Api.ConstructPlaneRequest, Api.ConstructPlaneResult>(
        Ops.ConstructionOperations.ConstructPlaneOperation.Descriptor,
        Ops.ConstructionOperations.ConstructPlaneOperation.CreateCommand,
        static (invoker, request, options) => Construction(invoker).ConstructPlaneAsync(request, options),
        static result => result.Execution);

    public static ProbeOperation MakePointsToPointsRelationship { get; } = Op<Api.MakePointsToPointsRelationshipRequest, Api.MakePointsToPointsRelationshipResult>(
        Ops.RelationshipOperations.MakePointsToPointsRelationshipOperation.Descriptor,
        Ops.RelationshipOperations.MakePointsToPointsRelationshipOperation.CreateCommand,
        static (invoker, request, options) => Relationship(invoker).MakePointsToPointsRelationshipAsync(request, options),
        static result => result.Execution);

    public static ProbeOperation ConstructVectorGroupFromRelationship { get; } = Op<Api.ConstructVectorGroupFromRelationshipRequest, Api.ConstructVectorGroupFromRelationshipResult>(
        Ops.ConstructionOperations.ConstructVectorGroupFromRelationshipOperation.Descriptor,
        Ops.ConstructionOperations.ConstructVectorGroupFromRelationshipOperation.CreateCommand,
        static (invoker, request, options) => Construction(invoker).ConstructVectorGroupFromRelationshipAsync(request, options),
        static result => result.Execution);

    public static ProbeOperation GetCloudPointCount { get; } = Op<Api.GetCloudPointCountRequest, Api.GetCloudPointCountResult>(
        Ops.CloudAndMeshOperations.GetCloudPointCountOperation.Descriptor,
        Ops.CloudAndMeshOperations.GetCloudPointCountOperation.CreateCommand,
        static (invoker, request, options) => CloudAndMesh(invoker).GetCloudPointCountAsync(request, options),
        static result => result.Execution,
        static (_, result) => Observations.Of(("points_count", Observations.Count(result.PointsCount))));

    // Probe operations: omit versus blank (#1-6) and case sensitivity (#17).
    public static ProbeOperation LocateInstrumentsUsmn { get; } = Op<Api.LocateInstrumentsUsmnRequest, Api.LocateInstrumentsUsmnResult>(
        Ops.InstrumentOperations.LocateInstrumentsUsmnOperation.Descriptor,
        Ops.InstrumentOperations.LocateInstrumentsUsmnOperation.CreateCommand,
        static (invoker, request, options) => Instrument(invoker).LocateInstrumentsUsmnAsync(request, options),
        static result => result.Execution);

    public static ProbeOperation FitGeometryToPointGroup { get; } = Op<Api.FitGeometryToPointGroupRequest, Api.FitGeometryToPointGroupResult>(
        Ops.AnalysisOperations.FitGeometryToPointGroupOperation.Descriptor,
        Ops.AnalysisOperations.FitGeometryToPointGroupOperation.CreateCommand,
        static (invoker, request, options) => Analysis(invoker).FitGeometryToPointGroupAsync(request, options),
        static result => result.Execution);

    public static ProbeOperation CreateChartFromVectorGroup { get; } = Op<Api.CreateChartFromVectorGroupRequest, Api.CreateChartFromVectorGroupResult>(
        Ops.ReportingOperations.CreateChartFromVectorGroupOperation.Descriptor,
        Ops.ReportingOperations.CreateChartFromVectorGroupOperation.CreateCommand,
        static (invoker, request, options) => Reporting(invoker).CreateChartFromVectorGroupAsync(request, options),
        static result => result.Execution);

    public static ProbeOperation SetUserInterfaceProfile { get; } = Op<Api.SetUserInterfaceProfileRequest, Api.SetUserInterfaceProfileResult>(
        Ops.UtilityOperations.SetUserInterfaceProfileOperation.Descriptor,
        Ops.UtilityOperations.SetUserInterfaceProfileOperation.CreateCommand,
        static (invoker, request, options) => Utility(invoker).SetUserInterfaceProfileAsync(request, options),
        static result => result.Execution);

    public static ProbeOperation ConstructPlanesBoundingPointGroup { get; } = Op<Api.ConstructPlanesBoundingPointGroupRequest, Api.ConstructPlanesBoundingPointGroupResult>(
        Ops.ConstructionOperations.ConstructPlanesBoundingPointGroupOperation.Descriptor,
        Ops.ConstructionOperations.ConstructPlanesBoundingPointGroupOperation.CreateCommand,
        static (invoker, request, options) => Construction(invoker).ConstructPlanesBoundingPointGroupAsync(request, options),
        static result => result.Execution);

    // The remaining NOT_SUPPORTED substitution (#11); #7-10 were retired with their operations.
    public static ProbeOperation ConstructPolygonizedSurface { get; } = Op<Api.ConstructPolygonizedSurfaceFromPointCloudsRequest, Api.ConstructPolygonizedSurfaceFromPointCloudsResult>(
        Ops.ConstructionOperations.ConstructPolygonizedSurfaceFromPointCloudsOperation.Descriptor,
        Ops.ConstructionOperations.ConstructPolygonizedSurfaceFromPointCloudsOperation.CreateCommand,
        static (invoker, request, options) => Construction(invoker).ConstructPolygonizedSurfaceFromPointCloudsAsync(request, options),
        static result => result.Execution);

    // Getters without licensed evidence (#12-13; #14 was retired with its operation) and the 0.9.1 getters (#22-23).
    public static ProbeOperation MakePointNameEnsureUnique { get; } = Op<Api.MakePointNameEnsureUniqueRequest, Api.MakePointNameEnsureUniqueResult>(
        Ops.ConstructionOperations.MakePointNameEnsureUniqueOperation.Descriptor,
        Ops.ConstructionOperations.MakePointNameEnsureUniqueOperation.CreateCommand,
        static (invoker, request, options) => Construction(invoker).MakePointNameEnsureUniqueAsync(request, options),
        static result => result.Execution,
        static (request, result) => Observations.Of(
            ("name_retrieved", Observations.Flag(result.ResultantPointName is not null)),
            ("differs_from_input", Observations.Flag(result.ResultantPointName is not null &&
                !string.Equals(result.ResultantPointName.TargetName, request.PointName.TargetName, StringComparison.Ordinal)))),
        static (request, outputs) =>
        {
            var value = outputs[0].RequireValue<WorkerPointNameValue>();
            return Observations.Of(
                ("name_retrieved", Observations.Flag(true)),
                ("differs_from_input", Observations.Flag(
                    !string.Equals(value.TargetName, request.PointName.TargetName, StringComparison.Ordinal))));
        });

    public static ProbeOperation MakeCollectionObjectNameEnsureUnique { get; } = Op<Api.MakeCollectionObjectNameEnsureUniqueRequest, Api.MakeCollectionObjectNameEnsureUniqueResult>(
        Ops.ConstructionOperations.MakeCollectionObjectNameEnsureUniqueOperation.Descriptor,
        Ops.ConstructionOperations.MakeCollectionObjectNameEnsureUniqueOperation.CreateCommand,
        static (invoker, request, options) => Construction(invoker).MakeCollectionObjectNameEnsureUniqueAsync(request, options),
        static result => result.Execution,
        static (request, result) => Observations.Of(
            ("name_retrieved", Observations.Flag(result.CollectionObjectName is not null)),
            ("differs_from_input", Observations.Flag(result.CollectionObjectName is not null &&
                !string.Equals(result.CollectionObjectName.ObjectName, request.CollectionObjectName.ObjectName, StringComparison.Ordinal)))));

    // The reviewed destructive operation: probe 24, the #307 re-probe control R0 (probes 15
    // and 16 were retired with Delete Cloud Points by X Y Z Range). ProbePlan limits it to
    // one harness-created cloud.
    public static ProbeOperation DeleteCloudPointsByRadialDistanceFromPoints { get; } = Op<Api.DeleteCloudPointsByRadialDistanceFromPointsRequest, Api.DeleteCloudPointsByRadialDistanceFromPointsResult>(
        Ops.CloudAndMeshOperations.DeleteCloudPointsByRadialDistanceFromPointsOperation.Descriptor,
        Ops.CloudAndMeshOperations.DeleteCloudPointsByRadialDistanceFromPointsOperation.CreateCommand,
        static (invoker, request, options) => CloudAndMesh(invoker).DeleteCloudPointsByRadialDistanceFromPointsAsync(request, options),
        static result => result.Execution);

    public static ProbeOperation VectorGroupRuntimeSelect { get; } = Op<Api.MakeCollectionVectorGroupNameRefListRuntimeSelectRequest, Api.MakeCollectionVectorGroupNameRefListRuntimeSelectResult>(
        Ops.ConstructionOperations.MakeCollectionVectorGroupNameRefListRuntimeSelectOperation.Descriptor,
        Ops.ConstructionOperations.MakeCollectionVectorGroupNameRefListRuntimeSelectOperation.CreateCommand,
        static (invoker, request, options) => Construction(invoker).MakeCollectionVectorGroupNameRefListRuntimeSelectAsync(request, options),
        static result => result.Execution,
        static (_, result) => Observations.Of(("vector_group_count", Observations.Count(
            result.ResultantCollectionVectorGroupNameReferenceList.Count))));

    public static ProbeOperation GetVectorGroupProperties { get; } = Op<Api.GetVectorGroupPropertiesRequest, Api.GetVectorGroupPropertiesResult>(
        Ops.VectorOperations.GetVectorGroupPropertiesOperation.Descriptor,
        Ops.VectorOperations.GetVectorGroupPropertiesOperation.CreateCommand,
        static (invoker, request, options) => Vector(invoker).GetVectorGroupPropertiesAsync(request, options),
        static result => result.Execution,
        static (_, result) => Observations.Of(
            ("total_vectors", Observations.Count(result.TotalVectors)),
            ("percent_in_tolerance_fractional", Observations.Flag(IsFractional(result.VectorsInTolerance2))),
            ("percent_out_of_tolerance_fractional", Observations.Flag(IsFractional(result.VectorsOutOfTolerance2)))));

    // Probe 25 (#313): SA Interaction Mode and an operator prompt under it.
    public static ProbeOperation SetInteractionMode { get; } = Op<Api.SetInteractionModeRequest, Api.SetInteractionModeResult>(
        Ops.UtilityOperations.SetInteractionModeOperation.Descriptor,
        Ops.UtilityOperations.SetInteractionModeOperation.CreateCommand,
        static (invoker, request, options) => Utility(invoker).SetInteractionModeAsync(request, options),
        static result => result.Execution);

    // The answer is a value, so only its comparison with the two harness constants is recorded.
    public static ProbeOperation AskForString { get; } = Op<Api.AskForStringRequest, Api.AskForStringResult>(
        Ops.ProcessFlowOperations.AskForStringOperation.Descriptor,
        Ops.ProcessFlowOperations.AskForStringOperation.CreateCommand,
        static (invoker, request, options) => ProcessFlow(invoker).AskForStringAsync(request, options),
        static result => result.Execution,
        static (request, result) => Observations.Of(
            ("answer_is_initial", Observations.Flag(string.Equals(result.Answer, request.InitialAnswer, StringComparison.Ordinal))),
            ("answer_is_operator_entry", Observations.Flag(
                string.Equals(result.Answer, ProbeCatalog.AskOperatorEntry, StringComparison.Ordinal)))));

    // Step-text probes (#19-21).
    public static ProbeOperation ComputeGroupToGroupOrientation { get; } = Op<Api.ComputeGroupToGroupOrientationRxRyRzRequest, Api.ComputeGroupToGroupOrientationRxRyRzResult>(
        Ops.AnalysisOperations.ComputeGroupToGroupOrientationRxRyRzOperation.Descriptor,
        Ops.AnalysisOperations.ComputeGroupToGroupOrientationRxRyRzOperation.CreateCommand,
        static (invoker, request, options) => Analysis(invoker).ComputeGroupToGroupOrientationRxRyRzAsync(request, options),
        static result => result.Execution);

    public static ProbeOperation AngleBetweenTwoPlanesNormals { get; } = Op<Api.AngleBetweenTwoPlanesNormalsRequest, Api.AngleBetweenTwoPlanesNormalsResult>(
        Ops.AnalysisOperations.AngleBetweenTwoPlanesNormalsOperation.Descriptor,
        Ops.AnalysisOperations.AngleBetweenTwoPlanesNormalsOperation.CreateCommand,
        static (invoker, request, options) => Analysis(invoker).AngleBetweenTwoPlanesNormalsAsync(request, options),
        static result => result.Execution);

    public static ProbeOperation ShowHidePoints { get; } = Op<Api.ShowHidePointsRequest, Api.ShowHidePointsResult>(
        Ops.ViewControl.ShowHidePointsOperation.Descriptor,
        Ops.ViewControl.ShowHidePointsOperation.CreateCommand,
        static (invoker, request, options) => View(invoker).ShowHidePointsAsync(request, options),
        static result => result.Execution);

    internal static bool IsFractional(double value) =>
        double.IsFinite(value) && Math.Abs(value - Math.Round(value)) > 1e-9;

    private static ProbeOperation<TRequest, TResult> Op<TRequest, TResult>(
        OperationDescriptor descriptor,
        Func<TRequest, WorkerMpCommand> build,
        Func<CallInvoker, TRequest, CallOptions, AsyncUnaryCall<TResult>> call,
        Func<TResult, Api.MpExecutionDetails?> execution,
        Func<TRequest, TResult, IReadOnlyDictionary<string, string>>? observePublic = null,
        Func<TRequest, IReadOnlyList<WorkerMpOutputValue>, IReadOnlyDictionary<string, string>>? observeWorker = null)
        where TRequest : class, Google.Protobuf.IMessage<TRequest>
        where TResult : class, Google.Protobuf.IMessage<TResult> =>
        new(descriptor.FullyQualifiedMethod, descriptor.RiskFlags.Contains("destructive", StringComparer.Ordinal),
            build, call, execution, observePublic, observeWorker);

    private static Api.AnalysisOperations.AnalysisOperationsClient Analysis(CallInvoker invoker) => new(invoker);

    private static Api.CloudAndMeshOperations.CloudAndMeshOperationsClient CloudAndMesh(CallInvoker invoker) => new(invoker);

    private static Api.ConstructionOperations.ConstructionOperationsClient Construction(CallInvoker invoker) => new(invoker);

    private static Api.InstrumentOperations.InstrumentOperationsClient Instrument(CallInvoker invoker) => new(invoker);

    private static Api.ProcessFlowOperations.ProcessFlowOperationsClient ProcessFlow(CallInvoker invoker) => new(invoker);

    private static Api.RelationshipOperations.RelationshipOperationsClient Relationship(CallInvoker invoker) => new(invoker);

    private static Api.ReportingOperations.ReportingOperationsClient Reporting(CallInvoker invoker) => new(invoker);

    private static Api.UtilityOperations.UtilityOperationsClient Utility(CallInvoker invoker) => new(invoker);

    private static Api.VectorOperations.VectorOperationsClient Vector(CallInvoker invoker) => new(invoker);

    private static Api.ViewControl.ViewControlClient View(CallInvoker invoker) => new(invoker);
}
