using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Briosa.Cli.Tests;

/// <summary>
/// Read-only access to this target's handwritten operation registry
/// (<c>SpatialAnalyzerApi.Operations</c>) and to the exact discovery projection the
/// server builds from it. The server keeps these internal, and the CLI must not
/// change the server, so the tests read them by reflection; a rename fails loudly.
/// </summary>
internal static class ServerProjection
{
    private static readonly Assembly Server = Assembly.Load(new AssemblyName("Briosa.Server"));
    private static readonly Lazy<IReadOnlyList<RegisteredOperation>> LazyRegistry = new(ReadRegistry);
    private static readonly Lazy<ListCapabilitiesResponse> LazyAll = new(() => Build("standard", includeDenied: true));
    private static readonly Lazy<ListCapabilitiesResponse> LazyAdmitted = new(() => Build("standard", includeDenied: false));

    /// <summary>Every registered operation, in registry order.</summary>
    public static IReadOnlyList<RegisteredOperation> Registry => LazyRegistry.Value;

    /// <summary>The packaged <c>standard</c> profile's discovery response with <c>include_denied</c>.</summary>
    public static ListCapabilitiesResponse AllCapabilities => LazyAll.Value.Clone();

    /// <summary>The packaged <c>standard</c> profile's default discovery response.</summary>
    public static ListCapabilitiesResponse AdmittedCapabilities => LazyAdmitted.Value.Clone();

    public static OperationCapability Capability(string operationId) =>
        AllCapabilities.Operations.Single(operation => operation.OperationId == operationId);

    private static RegisteredOperation[] ReadRegistry()
    {
        var api = Server.GetType("Briosa.Server.Operations.SpatialAnalyzerApi", throwOnError: true)!;
        var operations = (IEnumerable)api.GetProperty("Operations", BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;
        return operations.Cast<object>().Select(descriptor => new RegisteredOperation(
            (string)descriptor.GetType().GetProperty("OperationId")!.GetValue(descriptor)!,
            (string)descriptor.GetType().GetProperty("FullyQualifiedMethod")!.GetValue(descriptor)!)).ToArray();
    }

    private static ListCapabilitiesResponse Build(string profile, bool includeDenied)
    {
        var api = Server.GetType("Briosa.Server.Operations.SpatialAnalyzerApi", throwOnError: true)!;
        var operations = api.GetProperty("Operations", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);
        var policyType = Server.GetType("Briosa.Server.Security.OperationPolicy", throwOnError: true)!;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Briosa:Security:Operations:Profile"] = profile
            })
            .Build();
        var policy = policyType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [configuration, operations, null, null]);
        var discovery = Server.GetType("Briosa.Server.Services.ServerDiscoveryService", throwOnError: true)!;
        return (ListCapabilitiesResponse)discovery
            .GetMethod("BuildCapabilities", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [policy, includeDenied])!;
    }
}

internal sealed record RegisteredOperation(string OperationId, string FullyQualifiedMethod);
