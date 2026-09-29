using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class QuickAlignOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.quick_align", "Quick Align",
        "briosa.InstrumentOperations", "QuickAlign", "/briosa.InstrumentOperations/QuickAlign",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.QuickAlignRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Instrument IDs", WorkerMpValueKind.CollectionInstrumentIdList,
                InstrumentIdMapper.RequiredList(request.Instruments, "instruments"), "SetColInstIdRefListArg"),
            new("Objects", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.Objects, "objects"), "SetCollectionObjectNameRefListArg")
        };

        if (request.NominalPoints.Count > 0)
        {
            inputs.Add(new("Nominal Points (optional)", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.NominalPoints, "nominal_points"), "SetPointNameRefListArg"));
        }
        if (request.NominalPointOfViewNames.Count > 0)
        {
            inputs.Add(new("Nominal Point of View Names (optional)", WorkerMpValueKind.StringList,
                new WorkerStringListValue(request.NominalPointOfViewNames.ToArray()), "SetStringRefListArg"));
        }

        inputs.Add(new("Align to Individual Faces Only (not Entire Surface)", WorkerMpValueKind.Logical,
            new WorkerBooleanValue(request.AlignToIndividualFacesOnly), "SetBoolArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.QuickAlignResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
