using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;

namespace Briosa.Cli.Engine;

/// <summary>
/// Sends one unary MP call built from compiled descriptors alone: no generated
/// stub, no per-operation code. The call carries exactly one caller restriction
/// entry and is never retried.
/// </summary>
internal static class MpInvoker
{
    public static async Task<MpCallObservation> InvokeAsync(
        CallInvoker invoker,
        MethodDescriptor method,
        IMessage request,
        string callerRestriction,
        DateTime? deadline,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoker);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(request);
        var grpcMethod = new Method<IMessage, IMessage>(
            MethodType.Unary,
            method.Service.FullName,
            method.Name,
            Marshaller(method.InputType),
            Marshaller(method.OutputType));
        var headers = new Metadata { { BridgeContract.CallerRestrictionHeader, callerRestriction } };
        var options = new CallOptions(headers, deadline, cancellationToken);
        try
        {
            using var call = invoker.AsyncUnaryCall(grpcMethod, host: null, options, request);
            var response = await call.ResponseAsync.ConfigureAwait(false);
            return new MpCallObservation(response, StatusCode.OK, Trailers: null, CallerCancelled: false,
                CallerDeadlineReached: false);
        }
        catch (RpcException exception)
        {
            return new MpCallObservation(
                Response: null,
                exception.StatusCode,
                exception.Trailers,
                CallerCancelled: cancellationToken.IsCancellationRequested,
                CallerDeadlineReached: deadline is { } due && DateTime.UtcNow >= due);
        }
    }

    private static Marshaller<IMessage> Marshaller(MessageDescriptor descriptor) =>
        Marshallers.Create<IMessage>(message => message.ToByteArray(), bytes => descriptor.Parser.ParseFrom(bytes));
}

/// <summary>What the client observed for one sent MP call.</summary>
internal sealed record MpCallObservation(
    IMessage? Response,
    StatusCode Status,
    Metadata? Trailers,
    bool CallerCancelled,
    bool CallerDeadlineReached);
