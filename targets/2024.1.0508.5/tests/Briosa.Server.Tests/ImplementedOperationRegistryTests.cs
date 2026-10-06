using System.Reflection;
using global::Briosa;
using Briosa.Server.Operations;
using Briosa.Server.Services;
using Briosa.Server.Security;
using Microsoft.Extensions.Configuration;

namespace Briosa.Server.Tests;

public sealed class ImplementedOperationRegistryTests
{
    // get_working_directory, the eight Briosa.SmokeClient reads, the five
    // briosa-examples tutorial operations, and get_working_frame_properties
    // (the default-profile contract decided on #242).
    internal static readonly string[] DefaultProfileContract =
        [
            "file_operations.get_working_directory",
            "utility_operations.get_active_units",
            "utility_operations.get_working_frame_properties",
            "construction_operations.get_active_collection_name",
            "analysis_operations.get_number_of_collections",
            "analysis_operations.get_ith_collection_name",
            "analysis_operations.get_object_reporting_frame",
            "relationship_operations.get_relationship_reporting_frame",
            "construction_operations.construct_collection",
            "construction_operations.construct_point_in_working_coordinates",
            "analysis_operations.get_point_coordinate",
            "analysis_operations.get_point_to_point_distance"
        ];

    [Fact]
    public void ShippedPolicyStartsSuccessfullyAndAdmitsTheReviewedTargetSurface()
    {
        var policy = OperationPolicy.Create(ShippedConfiguration(), SpatialAnalyzerApi.Operations);

        const OperationRisks standardRisks = OperationRisks.FilesystemMetadata | OperationRisks.FilesystemRead |
            OperationRisks.FilesystemWrite | OperationRisks.Destructive;
        var expected = SpatialAnalyzerApi.Operations
            .Where(operation => OperationClassification.Find(operation.OperationId).Row is
            {
                Isolation: OperationIsolationClass.Admissible
            } row && (row.Risks & ~standardRisks) == OperationRisks.None)
            .Select(operation => operation.OperationId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal("standard", policy.Profile.Name);
        Assert.Same(OperationAdmissionProfile.Standard, policy.Profile);
        Assert.Equal(0, policy.FlagSettingCount);
        Assert.Equal(0, policy.OverrideCount);
        Assert.Equal(expected, policy.AllowedOperations.Select(operation => operation.OperationId));
        Assert.All(policy.AllowedOperations, operation =>
        {
            Assert.NotEqual(OperationExecutionScope.ExclusiveWorkflow, operation.ExecutionScope);
            Assert.Equal(
                OperationIsolationClass.Admissible,
                OperationClassification.Find(operation.OperationId).Row!.Isolation);
            Assert.False(OperationClassification.Find(operation.OperationId).Row!.Risks
                .HasFlag(OperationRisks.InteractiveUi));
        });
    }

    [Fact]
    public void ShippedPolicyKeepsTheDefaultProfileContract()
    {
        var policy = OperationPolicy.Create(ShippedConfiguration(), SpatialAnalyzerApi.Operations);
        var admitted = policy.AllowedOperations
            .Select(operation => operation.OperationId)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(12, DefaultProfileContract.Distinct(StringComparer.Ordinal).Count());
        foreach (var operationId in DefaultProfileContract)
        {
            Assert.True(admitted.Contains(operationId), operationId);
        }
    }

    [Fact]
    public void ShippedConfigurationUsesOnlyTheNamedPolicyKeys()
    {
        var operations = ShippedConfiguration().GetSection(OperationPolicy.SectionKey);

        Assert.Equal(["Profile"], operations.GetChildren().Select(child => child.Key));
        Assert.Equal(OperationAdmissionProfile.DefaultName, operations["Profile"]);
    }

    internal static IConfiguration ShippedConfiguration()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Briosa.slnx")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        return new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(root.FullName, "src", "Briosa.Server", "appsettings.json"))
            .Build();
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
