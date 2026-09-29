using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class SetPointWeightsFromUncertaintiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.set_point_weights_from_uncertainties", "Set Point Weights From Uncertainties",
        "briosa.AnalysisOperations", "SetPointWeightsFromUncertainties", "/briosa.AnalysisOperations/SetPointWeightsFromUncertainties",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("output_weighted_point_list", "Output Weighted Point List", WorkerMpValueKind.PointNameList)];

    public static WorkerMpCommand CreateCommand(Api.SetPointWeightsFromUncertaintiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Point Name List", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.PointNameList, "point_name_list"), "SetPointNameRefListArg"),
                new("Uncertainty Reference Frame Mode", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasUncertaintyReferenceFrameMode
                        ? request.UncertaintyReferenceFrameMode : "With respect to WORLD"), "SetStringArg"),
                new("Reporting Frame", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ReportingFrame, "reporting_frame"), "SetCollectionObjectNameArg2"),
                new("Weight Normalization Mode", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasWeightNormalizationMode
                        ? request.WeightNormalizationMode : "Set to fixed value"), "SetStringArg"),
                new("Fixed Weight Value", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasFixedWeightValue ? request.FixedWeightValue : 1), "SetDoubleArg"),
                new("Output Weighted Point Group", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.OutputWeightedPointGroup, "output_weighted_point_group"), "SetCollectionObjectNameArg2")
            ],
            [new("Output Weighted Point List", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg")]);
    }

    public static Api.SetPointWeightsFromUncertaintiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.SetPointWeightsFromUncertaintiesResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerPointNameListValue>().Values)
            result.OutputWeightedPointList.Add(PointNameMapper.ToProtocol(value));
        return result;
    }
}
