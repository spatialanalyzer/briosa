using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal static class SortVectorsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "vector_operations.sort_vectors", "Sort Vectors", "briosa.VectorOperations",
        "SortVectors", "/briosa.VectorOperations/SortVectors",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("sorted_vectors", "Sorted Vectors", WorkerMpValueKind.VectorNameList)];
    public static WorkerMpCommand CreateCommand(Api.SortVectorsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var coordinateSystem = request.HasCoordinateSystem
            ? request.CoordinateSystem : Api.CoordinateSystemType.Cartesian;
        if (!Enum.IsDefined(coordinateSystem) || coordinateSystem == Api.CoordinateSystemType.Unspecified)
            throw new ArgumentException("Unsupported coordinate system.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Source Vectors", WorkerMpValueKind.VectorNameList,
                VectorNameMapper.RequiredList(request.SourceVectors, "source_vectors"), "SetVectorNameRefListArg"),
            new("Sort Method", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasSortMethod ? request.SortMethod : "Magnitude"), "SetStringArg"),
            new("Coordinate System", WorkerMpValueKind.CoordinateSystemType,
                new WorkerChoiceValue<WorkerCoordinateSystemTypeValue>((WorkerCoordinateSystemTypeValue)((int)coordinateSystem - 1)),
                "SetCoordinateSystemTypeArg"),
            new("Primary Sort Coordinate", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasPrimarySortCoordinate ? request.PrimarySortCoordinate : "X (R)"), "SetStringArg"),
            new("Secondary Sort Coordinate", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasSecondarySortCoordinate ? request.SecondarySortCoordinate : "Y (Theta)"), "SetStringArg"),
            new("Tertiary Sort Coordinate", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasTertiarySortCoordinate ? request.TertiarySortCoordinate : "Z (Phi)"), "SetStringArg"),
            new("Primary Coordinate Granularity", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.PrimaryCoordinateGranularity), "SetDoubleArg"),
            new("Secondary Coordinate Granularity", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.SecondaryCoordinateGranularity), "SetDoubleArg"),
            new("Tertiary Coordinate Granularity", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.TertiaryCoordinateGranularity), "SetDoubleArg"),
            new("Ascending?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.Ascending), "SetBoolArg")
        ], [new("Sorted Vectors", WorkerMpValueKind.VectorNameList, "GetVectorNameRefListArg")]);
    }
    public static Api.SortVectorsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.SortVectorsResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerVectorNameListValue>().Values)
            result.SortedVectors.Add(VectorNameMapper.ToProtocol(value));
        return result;
    }
}
