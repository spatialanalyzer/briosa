using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetObscuredPointsFromInstrumentOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.ReadOnly(
        "instrument_operations.get_obscured_points_from_instrument", "Get Obscured Points from Instrument", "GetObscuredPointsFromInstrument");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("obscured_points", "Obscured Points", WorkerMpValueKind.PointNameList)];

    public static WorkerMpCommand CreateCommand(Api.GetObscuredPointsFromInstrumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Candidate Points", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.CandidatePoints, "candidate_points"), "SetPointNameRefListArg"),
                new("Show Obscured Shots", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasShowObscuredShots && request.ShowObscuredShots), "SetBoolArg")
            ],
            [new("Obscured Points", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg")]);
    }

    public static Api.GetObscuredPointsFromInstrumentResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetObscuredPointsFromInstrumentResult { Execution = completed.Details };
        result.ObscuredPoints.AddRange(completed.Execution.OutputValues[0]
            .RequireValue<WorkerPointNameListValue>().Values.Select(PointNameMapper.ToProtocol));
        return result;
    }
}
