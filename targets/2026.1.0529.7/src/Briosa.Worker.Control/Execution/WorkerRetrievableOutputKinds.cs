using System.Collections.Frozen;

namespace Briosa.Worker.Control;

/// <summary>
/// Output kinds the production SDK adapter can retrieve after MP success.
/// </summary>
/// <remarks>
/// This is the single shared declaration of the worker's output-getter surface. The worker adapter
/// tests assert that exactly these kinds are retrieved and every other kind comes back unavailable;
/// the server tests assert that every operation declares only these kinds; and the fake test worker
/// never fabricates values for any other kind. Add a kind here only together with its adapter getter.
/// </remarks>
public static class WorkerRetrievableOutputKinds
{
    public static IReadOnlySet<WorkerMpValueKind> All { get; } = new[]
    {
        WorkerMpValueKind.Logical,
        WorkerMpValueKind.WholeNumber,
        WorkerMpValueKind.FloatingPoint,
        WorkerMpValueKind.Text,
        WorkerMpValueKind.DoubleArray,
        WorkerMpValueKind.EditText,
        WorkerMpValueKind.Transform,
        WorkerMpValueKind.WorldTransform,
        WorkerMpValueKind.FileReference,
        WorkerMpValueKind.FitConstraintScalarOptions,
        WorkerMpValueKind.ToleranceScalarOptions,
        WorkerMpValueKind.PointName,
        WorkerMpValueKind.Vector,
        WorkerMpValueKind.ToleranceVectorOptions,
        WorkerMpValueKind.CollectionInstrumentId,
        WorkerMpValueKind.CollectionInstrumentIdList,
        WorkerMpValueKind.CollectionName,
        WorkerMpValueKind.CollectionItemName,
        WorkerMpValueKind.CollectionItemNameList,
        WorkerMpValueKind.CollectionObjectName,
        WorkerMpValueKind.CollectionObjectNameList,
        WorkerMpValueKind.CollectionVectorGroupNameList,
        WorkerMpValueKind.PointNameList,
        WorkerMpValueKind.StringList,
        WorkerMpValueKind.VectorNameList,
    }.ToFrozenSet();

    public static bool Contains(WorkerMpValueKind kind) => All.Contains(kind);
}
