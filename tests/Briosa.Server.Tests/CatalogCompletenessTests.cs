using System.Reflection;
using System.Text.Json;
using Briosa.Server.Generated.Sa.V2026_1_0529_7.V1Alpha1;
using Briosa.Server.Services;
using Google.Protobuf.Reflection;

namespace Briosa.Server.Tests;

public sealed class CatalogCompletenessTests
{
    [Fact]
    public void EveryCatalogSurfaceReportsTheExactReviewedOperationAndFamilySet()
    {
        var repositoryRoot = FindRepositoryRoot();
        var manifests = Directory.GetFiles(
            Path.Combine(repositoryRoot.FullName, "generated", "catalog", "sa"),
            "coverage.json",
            SearchOption.AllDirectories);
        Assert.NotEmpty(manifests);

        var catalogManifests = Directory.GetFiles(
                Path.Combine(repositoryRoot.FullName, "catalog", "sa"),
                "catalog.json",
                SearchOption.AllDirectories);
        var cataloged = catalogManifests.SelectMany(ReadCatalogOperations)
            .ToDictionary(operation => operation.OperationId, StringComparer.Ordinal);
        var releaseMemberships = catalogManifests
            .SelectMany(ReadReleaseMemberships)
            .ToDictionary(membership => membership.MembershipId, StringComparer.Ordinal);
        var generated = manifests
            .SelectMany(ReadCoverageOperations)
            .ToDictionary(operation => operation.OperationId, StringComparer.Ordinal);
        var generatedReleaseMemberships = manifests
            .SelectMany(ReadCoverageReleaseMemberships)
            .ToDictionary(membership => membership.MembershipId, StringComparer.Ordinal);
        var implemented = MarkedOperations<OperationImplementationAttribute>(
            typeof(OperationImplementationAttribute).Assembly,
            marker => marker.OperationId);
        var implementationMethods = typeof(OperationImplementationAttribute).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static))
            .Where(method => method.GetCustomAttribute<OperationImplementationAttribute>() is not null)
            .ToArray();
        var capabilities = TargetCatalogMetadata.Operations
            .Select(operation => operation.OperationId)
            .ToHashSet(StringComparer.Ordinal);
        var conformanceBindings = TargetCatalogConformanceMetadata.Operations
            .Select(operation => operation.Operation.OperationId)
            .ToHashSet(StringComparer.Ordinal);
        var protocolMethods = ReadProtocolMethods();
        var documented = ReadDocumentedOperations(repositoryRoot);

        Assert.Equal(cataloged.Keys.Order(), generated.Keys.Order());
        Assert.Equal(cataloged.Keys.Order(), implemented.Order());
        Assert.Equal(cataloged.Keys.Order(), capabilities.Order());
        Assert.Equal(cataloged.Keys.Order(), conformanceBindings.Order());
        Assert.Equal(
            generated.Values.Select(operation => operation.FullyQualifiedMethod).Order(),
            protocolMethods.Order());
        Assert.Equal(cataloged.Keys.Order(), documented.Order());
        Assert.Equal(releaseMemberships.Keys.Order(), generatedReleaseMemberships.Keys.Order());
        Assert.All(implementationMethods, method => Assert.NotNull(
            method.DeclaringType?.GetCustomAttribute<
                System.CodeDom.Compiler.GeneratedCodeAttribute>()));

        foreach (var operation in generated.Values)
        {
            Assert.True(operation.Protocol);
            Assert.True(operation.RequestValidation);
            Assert.True(operation.RequestAdapter);
            Assert.True(operation.ImmutableWorkerCommand);
            Assert.True(operation.ResultAdapter);
            Assert.True(operation.GrpcService);
            Assert.True(operation.ServiceRegistration);
            Assert.True(operation.Capability);
            Assert.True(operation.Documentation);
            Assert.True(operation.PortableConformance);

            var reviewedArguments = cataloged[operation.OperationId].Arguments;
            var generatedArguments = operation.Inputs.Concat(operation.Outputs)
                .ToDictionary(argument => argument.ArgumentId, StringComparer.Ordinal);
            Assert.Equal(reviewedArguments.Keys.Order(), generatedArguments.Keys.Order());
            foreach (var argument in generatedArguments.Values)
            {
                Assert.True(argument.ArgumentFamilyAssignment);
                Assert.Equal(reviewedArguments[argument.ArgumentId], argument.SemanticType);
                Assert.False(string.IsNullOrWhiteSpace(argument.Binding));
            }
        }

        foreach (var membership in releaseMemberships.Values)
        {
            var generatedMembership = generatedReleaseMemberships[membership.MembershipId];
            Assert.Equal(membership.CatalogId, generatedMembership.CatalogId);
            Assert.Equal(membership.OperationIds, generatedMembership.OperationIds);
            foreach (var operationId in membership.OperationIds)
            {
                Assert.Contains(operationId, generated.Keys);
                Assert.Contains(
                    membership.MembershipId,
                    generated[operationId].ReleaseMemberships);
            }
        }
    }

    private static IEnumerable<CatalogOperation> ReadCatalogOperations(string manifestPath)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        var targetRoot = Path.GetDirectoryName(manifestPath)!;
        foreach (var relativePath in manifest.RootElement.GetProperty("operation_files").EnumerateArray())
        {
            var operationPath = Path.Combine(
                targetRoot,
                relativePath.GetString()!.Replace('/', Path.DirectorySeparatorChar));
            using var operation = JsonDocument.Parse(File.ReadAllBytes(operationPath));
            yield return new CatalogOperation(
                operation.RootElement.GetProperty("operation_id").GetString()!,
                operation.RootElement.GetProperty("arguments").EnumerateArray()
                    .ToDictionary(
                        argument => argument.GetProperty("argument_id").GetString()!,
                        argument => argument.GetProperty("semantic_type").GetString()!,
                        StringComparer.Ordinal));
        }
    }

    private static IReadOnlyList<CoverageOperation> ReadCoverageOperations(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        return [.. document.RootElement.GetProperty("operations").EnumerateArray()
            .Select(operation =>
            {
                var generated = operation.GetProperty("generated");
                return new CoverageOperation(
                    operation.GetProperty("operation_id").GetString()!,
                    operation.GetProperty("fully_qualified_method").GetString()!,
                    [.. operation.GetProperty("release_memberships").EnumerateArray()
                        .Select(value => value.GetString()!)],
                    generated.GetProperty("protocol").GetBoolean(),
                    generated.GetProperty("request_validation").GetBoolean(),
                    generated.GetProperty("request_adapter").GetBoolean(),
                    generated.GetProperty("immutable_worker_command").GetBoolean(),
                    generated.GetProperty("result_adapter").GetBoolean(),
                    generated.GetProperty("grpc_service").GetBoolean(),
                    generated.GetProperty("service_registration").GetBoolean(),
                    generated.GetProperty("capability").GetBoolean(),
                    generated.GetProperty("documentation").GetBoolean(),
                    generated.GetProperty("portable_conformance").GetBoolean(),
                    [.. operation.GetProperty("inputs").EnumerateArray().Select(input =>
                        ReadCoverageArgument(input, "setter"))],
                    [.. operation.GetProperty("outputs").EnumerateArray().Select(output =>
                        ReadCoverageArgument(output, "getter"))]);
            })];
    }

    private static IEnumerable<ReleaseMembership> ReadReleaseMemberships(string manifestPath)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        var targetRoot = Path.GetDirectoryName(manifestPath)!;
        foreach (var relativePath in manifest.RootElement
                     .GetProperty("release_membership_files").EnumerateArray())
        {
            var membershipPath = Path.Combine(
                targetRoot,
                relativePath.GetString()!.Replace('/', Path.DirectorySeparatorChar));
            using var membership = JsonDocument.Parse(File.ReadAllBytes(membershipPath));
            yield return ReadReleaseMembership(membership.RootElement);
        }
    }

    private static IReadOnlyList<ReleaseMembership> ReadCoverageReleaseMemberships(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        return [.. document.RootElement.GetProperty("release_memberships").EnumerateArray()
            .Select(ReadReleaseMembership)];
    }

    private static ReleaseMembership ReadReleaseMembership(JsonElement membership) =>
        new(
            membership.GetProperty("membership_id").GetString()!,
            membership.GetProperty("catalog_id").GetString()!,
            [.. membership.GetProperty("operation_ids").EnumerateArray()
                .Select(operationId => operationId.GetString()!)]);

    private static CoverageArgument ReadCoverageArgument(
        JsonElement argument,
        string bindingProperty) =>
        new(
            argument.GetProperty("argument_id").GetString()!,
            argument.GetProperty("semantic_type").GetString()!,
            argument.GetProperty("argument_family_assignment").GetBoolean(),
            argument.GetProperty(bindingProperty).GetString()!);

    private static HashSet<string> ReadProtocolMethods() =>
        [.. Briosa.Protocol.ProtocolAssembly.MarkerType.Assembly.GetTypes()
            .Select(type => type.GetProperty(
                "Descriptor",
                BindingFlags.Public | BindingFlags.Static)?.GetValue(null))
            .OfType<ServiceDescriptor>()
            .Where(service => service.FullName.StartsWith("briosa.sa.", StringComparison.Ordinal))
            .SelectMany(service => service.Methods.Select(method =>
                $"/{service.FullName}/{method.Name}"))];

    private static HashSet<string> ReadDocumentedOperations(DirectoryInfo repositoryRoot) =>
        [.. Directory.GetFiles(
                Path.Combine(repositoryRoot.FullName, "docs", "reference", "generated", "sa"),
                "operations.md",
                SearchOption.AllDirectories)
            .SelectMany(File.ReadLines)
            .Where(line => line.StartsWith("- Briosa operation: `", StringComparison.Ordinal))
            .Select(line => line[21..^1])];

    private static HashSet<string> MarkedOperations<TAttribute>(
        Assembly assembly,
        Func<TAttribute, string> operationId)
        where TAttribute : Attribute =>
        [.. assembly.GetTypes()
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static))
            .SelectMany(method => method.GetCustomAttributes<TAttribute>())
            .Select(operationId)];

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ??
            throw new DirectoryNotFoundException("Could not locate the Briosa repository root.");
    }

    private sealed record CatalogOperation(
        string OperationId,
        IReadOnlyDictionary<string, string> Arguments);

    private sealed record CoverageOperation(
        string OperationId,
        string FullyQualifiedMethod,
        IReadOnlyList<string> ReleaseMemberships,
        bool Protocol,
        bool RequestValidation,
        bool RequestAdapter,
        bool ImmutableWorkerCommand,
        bool ResultAdapter,
        bool GrpcService,
        bool ServiceRegistration,
        bool Capability,
        bool Documentation,
        bool PortableConformance,
        IReadOnlyList<CoverageArgument> Inputs,
        IReadOnlyList<CoverageArgument> Outputs);

    private sealed record ReleaseMembership(
        string MembershipId,
        string CatalogId,
        IReadOnlyList<string> OperationIds);

    private sealed record CoverageArgument(
        string ArgumentId,
        string SemanticType,
        bool ArgumentFamilyAssignment,
        string Binding);
}
