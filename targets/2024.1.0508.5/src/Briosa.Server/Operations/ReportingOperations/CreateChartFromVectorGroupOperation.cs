using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class CreateChartFromVectorGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.create_chart_from_vector_group", "Create Chart from Vector Group", "briosa.ReportingOperations",
        "CreateChartFromVectorGroup", "/briosa.ReportingOperations/CreateChartFromVectorGroup", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CreateChartFromVectorGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("New Chart Name", WorkerMpValueKind.ChartName,
                ChartNameMapper.Required(request.NewChartName, "new_chart_name"), "SetChartNameArg"),
            new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroupName, "vector_group_name"), "SetCollectionObjectNameArg2"),
            new("Chart Type", WorkerMpValueKind.ChartType,
                new WorkerChoiceValue<WorkerChartTypeValue>(ChartTypeMapper.Required(
                    request.HasChartType ? request.ChartType : null, "chart_type")), "SetChartTypeArg"),
            new("Data Set to Chart", WorkerMpValueKind.VectorComponent,
                new WorkerChoiceValue<WorkerVectorComponentValue>(DatasetTypeMapper.Required(
                    request.HasDataSetToChart ? request.DataSetToChart : null, "data_set_to_chart")), "SetDatasetTypeArg"),
            new("Aux Data Set to Chart", WorkerMpValueKind.VectorComponent,
                new WorkerChoiceValue<WorkerVectorComponentValue>(DatasetTypeMapper.Required(
                    request.HasAuxDataSetToChart ? request.AuxDataSetToChart : null, "aux_data_set_to_chart")), "SetDatasetTypeArg"),
            new("Template Chart Name (optional)", WorkerMpValueKind.ChartName,
                ChartNameMapper.Required(request.TemplateChartName, "template_chart_name"), "SetChartNameArg"),
            new("Show Interface?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ShowInterface), "SetBoolArg")
        ], []);
    }

    public static Api.CreateChartFromVectorGroupResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
