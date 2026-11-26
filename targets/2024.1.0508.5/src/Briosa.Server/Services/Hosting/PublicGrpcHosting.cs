using Microsoft.Extensions.DependencyInjection;

namespace Briosa.Server.Services;

/// <summary>
/// Configures the public gRPC endpoint. Its receive limit is its own value, not
/// one of the private worker channel budgets in
/// <see cref="Briosa.Worker.Control.WorkerControlProtocol"/>, so raising the
/// worker response budget cannot move it.
/// </summary>
internal static class PublicGrpcHosting
{
    /// <summary>
    /// The public inbound limit: the largest request message the endpoint
    /// accepts. Server 0.10.0 accepted 64 KiB. Lowering it would reject requests
    /// that release accepts; raising it is a separate decision.
    /// </summary>
    internal const int MaximumReceiveMessageBytes = 64 * 1024;

    // The send size keeps the gRPC default (no limit). A response is already
    // bounded upstream by the worker response budget and the host's mapping.
    // Every unary call passes the audit interceptor, so no RPC, present or
    // future, lacks an arrival and completion record (#450). Its records are
    // Debug; with Debug off it only checks the level.
    public static IServiceCollection AddBriosaGrpc(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<UnaryCallAuditInterceptor>();
        services.AddGrpc(options =>
        {
            options.MaxReceiveMessageSize = MaximumReceiveMessageBytes;
            options.Interceptors.Add<UnaryCallAuditInterceptor>();
        });
        return services;
    }
}
