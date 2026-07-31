using System.Reflection;
using Briosa.Server.Operations;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Services;

namespace Briosa.Server.Tests;

public sealed class ImplementedOperationRegistryTests
{
    [Fact]
    public void RegistryContainsTheReviewedHandwrittenOperations()
    {
        Assert.Equal(
            [
                GetIThCollectionNameOperation.Descriptor,
                GetWorkingDirectoryOperation.Descriptor
            ],
            SpatialAnalyzerApi.Operations);
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
