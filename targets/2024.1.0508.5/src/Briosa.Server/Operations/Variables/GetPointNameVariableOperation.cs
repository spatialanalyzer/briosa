using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetPointNameVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_point_name_variable", "Get Point Name Variable",
        "briosa.Variables", "GetPointNameVariable", "/briosa.Variables/GetPointNameVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.PointName)];

    public static WorkerMpCommand CreateCommand(Api.GetPointNameVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Value", WorkerMpValueKind.PointName, "GetPointNameArg")]);
    }

    public static Api.GetPointNameVariableResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        Value = PointNameMapper.ToProtocol(completed.Execution.OutputValues[0].RequireValue<WorkerPointNameValue>())
    };
}
