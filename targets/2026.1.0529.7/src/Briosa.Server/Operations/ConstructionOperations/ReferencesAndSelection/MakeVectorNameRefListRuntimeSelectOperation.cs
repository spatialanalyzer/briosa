using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeVectorNameRefListRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_vector_name_ref_list_runtime_select",
        "Make a Vector Name Ref List - Runtime Select", "briosa.ConstructionOperations",
        "MakeVectorNameRefListRuntimeSelect", "/briosa.ConstructionOperations/MakeVectorNameRefListRuntimeSelect",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_vector_name_list", "Resultant Vector Name List", WorkerMpValueKind.VectorNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeVectorNameRefListRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("User Prompt", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasUserPrompt ? request.UserPrompt : " Select Vectors (ENTER when done) "),
                "SetStringArg")],
            [new("Resultant Vector Name List", WorkerMpValueKind.VectorNameList, "GetVectorNameRefListArg")]);
    }

    public static Api.MakeVectorNameRefListRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeVectorNameRefListRuntimeSelectResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerVectorNameListValue>().Values)
            result.ResultantVectorNameList.Add(VectorNameMapper.ToProtocol(value));
        return result;
    }
}
