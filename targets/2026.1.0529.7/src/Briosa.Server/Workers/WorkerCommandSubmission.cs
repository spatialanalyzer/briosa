using Briosa.Server.Security;
using Briosa.Worker.Control;
using Google.Protobuf;

namespace Briosa.Server.Workers;

/// <summary>
/// Defers request mapping until policy and admission have accepted the operation.
/// </summary>
/// <param name="OperationId">The registered operation ID.</param>
/// <param name="CreateCommand">Maps the request after admission.</param>
/// <param name="RetainedBytes">The conservative retained-work admission budget.</param>
/// <param name="Request">
/// The typed public request. Policy reads its reviewed conditional options before
/// mapping or dispatch; an operation with such options fails closed without it.
/// </param>
/// <param name="DurationClass">
/// The effective duration class policy assigned to the admitted request. Callers
/// leave it unset; the policy enforcer sets it.
/// </param>
internal sealed record WorkerCommandSubmission(
    string OperationId,
    Func<WorkerMpCommand> CreateCommand,
    int RetainedBytes = 1024 * 1024,
    IMessage? Request = null,
    OperationDurationClass? DurationClass = null);
