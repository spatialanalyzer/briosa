namespace Briosa.Server.Services;

// A cancellation token does not say why its caller stopped waiting. The lifecycle
// gRPC services apply the rule of the MP path (OperationExecutor): a caller that
// stopped waiting once the call's gRPC deadline had passed exceeded that deadline.
// gRPC reports DateTime.MaxValue for a call without a deadline.
internal static class CallerDeadline
{
    public static bool HasPassed(DateTime deadline, TimeProvider timeProvider) =>
        deadline != DateTime.MaxValue &&
        deadline <= timeProvider.GetUtcNow().UtcDateTime;
}
