using System.Reflection;
using Briosa.Server.Operations;
using Briosa.Server.Services;
using Briosa.Server.Security;
using Microsoft.Extensions.Configuration;

namespace Briosa.Server.Tests;

public sealed class ImplementedOperationRegistryTests
{
    [Fact]
    public void ShippedPolicyStartsSuccessfullyAndAdmitsTheReviewedTargetSurface()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Briosa.slnx")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(root.FullName, "src", "Briosa.Server", "appsettings.json"))
            .Build();
        var policy = OperationPolicy.Create(configuration, SpatialAnalyzerApi.Operations);
        Assert.Equal(
            SpatialAnalyzerApi.Operations.Select(operation => operation.OperationId).Order(StringComparer.Ordinal),
            policy.AllowedOperations.Select(operation => operation.OperationId).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void RegistryAndHandwrittenGrpcImplementationsStayInSync()
    {
        var registeredIds = SpatialAnalyzerApi.Operations
            .Select(operation => operation.OperationId)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var implementedIds = typeof(SpatialAnalyzerApi).Assembly
            .GetTypes()
            .SelectMany(type => type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic))
            .Select(method => method.GetCustomAttribute<OperationImplementationAttribute>())
            .Where(attribute => attribute is not null)
            .Select(attribute => attribute!.OperationId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(registeredIds.Distinct(StringComparer.Ordinal), registeredIds);
        Assert.Equal(implementedIds.Distinct(StringComparer.Ordinal), implementedIds);
        Assert.Equal(registeredIds, implementedIds);
    }
}
