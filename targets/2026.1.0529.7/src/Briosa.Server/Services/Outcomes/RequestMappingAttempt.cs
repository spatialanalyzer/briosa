using Briosa.Server.Operations;
using Briosa.Worker.Control;
using Google.Protobuf;
using Api = global::Briosa;

namespace Briosa.Server.Services;

/// <summary>
/// One run of an operation's request mapping (#442). It records whether the mapping
/// produced a command and which <see cref="RequestFieldViolation"/> instance the mapping
/// itself threw, so that field detail is reported only for that instance, by origin
/// rather than by exception type. The pipeline maps a request once. A second invocation
/// breaks that assumption: no violation it throws counts as the mapping's own, and once
/// a command exists the executor treats any argument failure as one it cannot prove
/// did not start.
/// </summary>
internal sealed class RequestMappingAttempt<TRequest>(
    TRequest request,
    Func<TRequest, WorkerMpCommand> createCommand)
    where TRequest : class
{
    private RequestFieldViolation? _violation;
    private int _invocations;
    private int _completed;

    /// <summary>The mapping returned a command, which may since have been dispatched.</summary>
    public bool Completed => Volatile.Read(ref _completed) != 0;

    /// <summary>The mapping delegate ran more than once.</summary>
    public bool Repeated => Volatile.Read(ref _invocations) > 1;

    public WorkerMpCommand Create()
    {
        var first = Interlocked.Increment(ref _invocations) == 1;
        try
        {
            var command = createCommand(request);
            Volatile.Write(ref _completed, 1);
            return command;
        }
        catch (RequestFieldViolation violation) when (first)
        {
            _violation = violation;
            throw;
        }
    }

    /// <summary>
    /// The exception is the violation that the mapping threw on its first and only
    /// invocation.
    /// </summary>
    public bool Threw(Exception exception) =>
        !Repeated && _violation is not null && ReferenceEquals(exception, _violation);

    /// <summary>
    /// An argument failure that the executor cannot report as a request validation
    /// failure that did not start: a command was mapped and either the failure is a typed
    /// violation the mapping did not throw, or the mapping ran more than once.
    /// </summary>
    public bool FailedAfterMapping(Exception exception) =>
        Completed && (Repeated || exception is RequestFieldViolation && !Threw(exception));

    /// <summary>
    /// The detail of the mapping's own violation when its path names a field of this
    /// request; null for any other exception, or for a path that does not resolve.
    /// </summary>
    public Api.RequestValidationDetails? Details(Exception exception) =>
        Threw(exception) && request is IMessage message ? _violation!.ToDetails(message.Descriptor) : null;
}
