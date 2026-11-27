using Briosa.Server.Operations;
using Briosa.Worker.Control;
using Google.Protobuf;
using Api = global::Briosa;

namespace Briosa.Server.Services;

/// <summary>
/// One run of an operation's request mapping (#442). It records whether the mapping
/// produced a command and which <see cref="RequestFieldViolation"/> instance the mapping
/// itself threw, so that field detail is reported only for that instance, by origin
/// rather than by exception type.
/// </summary>
internal sealed class RequestMappingAttempt<TRequest>(
    TRequest request,
    Func<TRequest, WorkerMpCommand> createCommand)
    where TRequest : class
{
    private RequestFieldViolation? _violation;

    /// <summary>The mapping returned a command, which may since have been dispatched.</summary>
    public bool Completed { get; private set; }

    public WorkerMpCommand Create()
    {
        try
        {
            var command = createCommand(request);
            Completed = true;
            return command;
        }
        catch (RequestFieldViolation violation)
        {
            _violation = violation;
            throw;
        }
    }

    /// <summary>The exception is the violation that this mapping threw.</summary>
    public bool Threw(Exception exception) => _violation is not null && ReferenceEquals(exception, _violation);

    /// <summary>
    /// The detail of the mapping's own violation when its path names a field of this
    /// request; null for any other exception, or for a path that does not resolve.
    /// </summary>
    public Api.RequestValidationDetails? Details(Exception exception) =>
        Threw(exception) && request is IMessage message ? _violation!.ToDetails(message.Descriptor) : null;
}
