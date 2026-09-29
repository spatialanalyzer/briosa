using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class BestFitTransformationGroupToGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.best_fit_transformation_group_to_group", "Best Fit Transformation - Group to Group",
        "briosa.AnalysisOperations", "BestFitTransformationGroupToGroup",
        "/briosa.AnalysisOperations/BestFitTransformationGroupToGroup",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("transform_in_working", "Transform in Working", WorkerMpValueKind.Transform),
        new("optimum_transform", "Optimum Transform", WorkerMpValueKind.WorldTransform),
        new("rms_deviation", "RMS Deviation", WorkerMpValueKind.FloatingPoint),
        new("maximum_absolute_deviation", "Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint),
        new("number_of_unknowns", "Number of Unknowns", WorkerMpValueKind.WholeNumber),
        new("number_of_equations", "Number of Equations", WorkerMpValueKind.WholeNumber),
        new("robustness", "Robustness", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.BestFitTransformationGroupToGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Reference Group", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ReferenceGroup, "reference_group"), "SetCollectionObjectNameArg2"),
                new("Corresponding Group", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.CorrespondingGroup, "corresponding_group"), "SetCollectionObjectNameArg2"),
                new("Show Interface", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasShowInterface && request.ShowInterface), "SetBoolArg"),
                new("RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasRmsTolerance ? request.RmsTolerance : 0d), "SetDoubleArg"),
                new("Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMaximumAbsoluteTolerance ? request.MaximumAbsoluteTolerance : 0d), "SetDoubleArg"),
                new("Allow Scale", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasAllowScale && request.AllowScale), "SetBoolArg"),
                new("Allow X", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasAllowX || request.AllowX), "SetBoolArg"),
                new("Allow Y", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasAllowY || request.AllowY), "SetBoolArg"),
                new("Allow Z", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasAllowZ || request.AllowZ), "SetBoolArg"),
                new("Allow Rx", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasAllowRx || request.AllowRx), "SetBoolArg"),
                new("Allow Ry", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasAllowRy || request.AllowRy), "SetBoolArg"),
                new("Allow Rz", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasAllowRz || request.AllowRz), "SetBoolArg"),
                new("Lock Degrees of Freedom", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasLockDegreesOfFreedom && request.LockDegreesOfFreedom), "SetBoolArg"),
                new("Generate Event", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasGenerateEvent && request.GenerateEvent), "SetBoolArg"),
                new("File Path for CSV Text Report (requires Show Interface = TRUE)", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.FilePathForCsvTextReport, "file_path_for_csv_text_report"), "SetFilePathArg")
            ],
            [
                new("Transform in Working", WorkerMpValueKind.Transform, "GetTransformArg"),
                new("Optimum Transform", WorkerMpValueKind.WorldTransform, "GetWorldTransformArg"),
                new("RMS Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Number of Unknowns", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Number of Equations", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Robustness", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.BestFitTransformationGroupToGroupResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            TransformInWorking = TransformMapper.ToProtocol(values[0].RequireValue<WorkerTransformValue>()),
            OptimumTransform = WorldTransformMapper.ToProtocol(values[1].RequireValue<WorkerWorldTransformValue>()),
            RmsDeviation = values[2].RequireValue<WorkerDoubleValue>().Value,
            MaximumAbsoluteDeviation = values[3].RequireValue<WorkerDoubleValue>().Value,
            NumberOfUnknowns = values[4].RequireValue<WorkerIntegerValue>().Value,
            NumberOfEquations = values[5].RequireValue<WorkerIntegerValue>().Value,
            Robustness = values[6].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
