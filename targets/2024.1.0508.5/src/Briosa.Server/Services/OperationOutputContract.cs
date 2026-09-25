using Briosa.Worker.Control;

namespace Briosa.Server.Services;

internal sealed record OperationOutputContract(
    string FieldName,
    string ArgumentName,
    WorkerMpValueKind Kind);
