using Briosa.Generator;
using System.Text.Json.Nodes;

namespace Briosa.Generator.Tests;

public sealed class CommandCatalogGeneratorTests
{
    [Fact]
    public void CommittedVerticalSliceMatchesDeterministicGeneration()
    {
        var repositoryRoot = FindRepositoryRoot();
        var outputRoot = Path.Combine(
            Path.GetTempPath(),
            $"briosa-generator-tests-{Guid.NewGuid():N}");
        try
        {
            var result = CommandCatalogGenerator.Generate(
                Path.Combine(repositoryRoot.FullName, "catalog"),
                outputRoot);

            Assert.Equal(
                [
                    "proto/briosa/sa/v2026_1_0529_7/v1alpha1/collection_operations.proto",
                    "proto/briosa/sa/v2026_1_0529_7/v1alpha1/file_operations.proto",
                    "src/Briosa.Server/Generated/Sa/V2026_1_0529_7/V1Alpha1/Operations.g.cs",
                    "docs/reference/generated/sa/2026.1.0529.7/operations.md",
                    "generated/catalog/sa/2026.1.0529.7/coverage.json",
                    "src/Briosa.Server/Generated/CatalogServiceRegistration.g.cs"
                ],
                result.Files);

            foreach (var relativePath in result.Files)
            {
                Assert.Equal(
                    File.ReadAllBytes(Path.Combine(repositoryRoot.FullName, relativePath)),
                    File.ReadAllBytes(Path.Combine(outputRoot, relativePath)));
            }
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void VerticalSlicePreservesExactStepAndResultGetterNames()
    {
        var repositoryRoot = FindRepositoryRoot();
        var outputRoot = Path.Combine(
            Path.GetTempPath(),
            $"briosa-generator-tests-{Guid.NewGuid():N}");
        try
        {
            _ = CommandCatalogGenerator.Generate(
                Path.Combine(repositoryRoot.FullName, "catalog"),
                outputRoot);

            var binding = File.ReadAllText(Path.Combine(
                outputRoot,
                "src",
                "Briosa.Server",
                "Generated",
                "Sa",
                "V2026_1_0529_7",
                "V1Alpha1",
                "Operations.g.cs"));
            Assert.Contains("StepName = \"Get Working Directory\"", binding, StringComparison.Ordinal);
            Assert.Contains("DirectoryArgumentName = \"Directory\"", binding, StringComparison.Ordinal);
            Assert.Contains("DirectoryFieldName = \"directory\"", binding, StringComparison.Ordinal);
            Assert.Contains("WorkerMpValueKind.Text", binding, StringComparison.Ordinal);
            Assert.Contains("DirectoryGetter = \"GetStringArg\"", binding, StringComparison.Ordinal);
            Assert.Contains("TargetCatalogMetadata", binding, StringComparison.Ordinal);
            Assert.Contains("TargetCatalogConformanceMetadata", binding, StringComparison.Ordinal);
            Assert.Contains("CatalogOperationConformanceBinding", binding, StringComparison.Ordinal);
            Assert.Contains("CatalogId = \"briosa.sa.2026.1.0529.7\"", binding, StringComparison.Ordinal);
            Assert.Contains("CoreProtocol.ReplaySafety.Safe", binding, StringComparison.Ordinal);
            Assert.Contains(
                "CoreProtocol.OperationExecutionScope.GlobalStateRead",
                binding,
                StringComparison.Ordinal);
            Assert.Contains("/briosa.sa.v2026_1_0529_7.v1alpha1.FileOperations/GetWorkingDirectory", binding, StringComparison.Ordinal);
            Assert.Contains("OutputContracts", binding, StringComparison.Ordinal);
            Assert.Contains("CreateResult(SuccessfulOperationExecution completed)", binding, StringComparison.Ordinal);
            Assert.Contains("internal sealed class FileOperationsService", binding, StringComparison.Ordinal);
            Assert.Contains("[OperationImplementation(FileOperationsGetWorkingDirectoryBinding.OperationId)]", binding, StringComparison.Ordinal);
            Assert.Contains("_operationExecutor.ExecuteAsync(", binding, StringComparison.Ordinal);

            var registration = File.ReadAllText(Path.Combine(
                outputRoot,
                "src",
                "Briosa.Server",
                "Generated",
                "CatalogServiceRegistration.g.cs"));
            Assert.Contains(
                "MapGrpcService<global::Briosa.Server.Generated.Sa.V2026_1_0529_7.V1Alpha1.FileOperationsService>()",
                registration,
                StringComparison.Ordinal);

            var proto = File.ReadAllText(Path.Combine(
                outputRoot,
                "proto",
                "briosa",
                "sa",
                "v2026_1_0529_7",
                "v1alpha1",
                "file_operations.proto"));
            Assert.Contains(
                "rpc GetWorkingDirectory(GetWorkingDirectoryRequest) returns (GetWorkingDirectoryResult)",
                proto,
                StringComparison.Ordinal);
            Assert.Contains("optional string directory = 1", proto, StringComparison.Ordinal);
            Assert.Contains(
                "briosa.core.v1alpha1.MpExecutionDetails execution = 1000",
                proto,
                StringComparison.Ordinal);

            var collectionProto = File.ReadAllText(Path.Combine(
                outputRoot,
                "proto",
                "briosa",
                "sa",
                "v2026_1_0529_7",
                "v1alpha1",
                "collection_operations.proto"));
            Assert.Contains(
                "rpc GetCollectionCount(GetCollectionCountRequest) returns (GetCollectionCountResult)",
                collectionProto,
                StringComparison.Ordinal);
            Assert.Contains(
                "rpc ConstructPointInWorkingCoordinates(ConstructPointInWorkingCoordinatesRequest) returns (ConstructPointInWorkingCoordinatesResult)",
                collectionProto,
                StringComparison.Ordinal);
            Assert.Contains(
                "rpc ConstructPointAtCircleCenter(ConstructPointAtCircleCenterRequest) returns (ConstructPointAtCircleCenterResult)",
                collectionProto,
                StringComparison.Ordinal);
            Assert.Contains(
                "rpc ConstructPointAtLineMidpoint(ConstructPointAtLineMidpointRequest) returns (ConstructPointAtLineMidpointResult)",
                collectionProto,
                StringComparison.Ordinal);
            Assert.Contains(
                "rpc ConstructPointFitToPoints(ConstructPointFitToPointsRequest) returns (ConstructPointFitToPointsResult)",
                collectionProto,
                StringComparison.Ordinal);
            Assert.Contains(
                "rpc ConstructPointGroupFromPointNameList(ConstructPointGroupFromPointNameListRequest) returns (ConstructPointGroupFromPointNameListResult)",
                collectionProto,
                StringComparison.Ordinal);
            Assert.Contains(
                "rpc DeletePoints(DeletePointsRequest) returns (DeletePointsResult)",
                collectionProto,
                StringComparison.Ordinal);
            Assert.Contains(
                "rpc GetCollectionNameByIndex(GetCollectionNameByIndexRequest) returns (GetCollectionNameByIndexResult)",
                collectionProto,
                StringComparison.Ordinal);
            Assert.Contains(
                "rpc ListGroupsInCollection(ListGroupsInCollectionRequest) returns (ListGroupsInCollectionResult)",
                collectionProto,
                StringComparison.Ordinal);
            Assert.Contains(
                "rpc ListPointsInGroup(ListPointsInGroupRequest) returns (ListPointsInGroupResult)",
                collectionProto,
                StringComparison.Ordinal);
            Assert.Contains(
                "rpc RenamePoint(RenamePointRequest) returns (RenamePointResult)",
                collectionProto,
                StringComparison.Ordinal);
            Assert.Contains("Vector3 working_coordinates = 2", collectionProto, StringComparison.Ordinal);

            var coverage = File.ReadAllText(Path.Combine(
                outputRoot,
                "generated",
                "catalog",
                "sa",
                "2026.1.0529.7",
                "coverage.json"));
            Assert.Contains(
                "\"inventory_key\": \"documentation:FileOperations/GetWorkingDirectory.htm\"",
                coverage,
                StringComparison.Ordinal);
            Assert.Contains("\"protocol_file\": \"file_operations.proto\"", coverage, StringComparison.Ordinal);
            Assert.Contains(
                "\"fully_qualified_method\": \"/briosa.sa.v2026_1_0529_7.v1alpha1.FileOperations/GetWorkingDirectory\"",
                coverage,
                StringComparison.Ordinal);
            Assert.Contains("\"sdk_order\": 0", coverage, StringComparison.Ordinal);
            Assert.Contains("\"field_number\": 1", coverage, StringComparison.Ordinal);
            Assert.Contains("\"request_validation\": true", coverage, StringComparison.Ordinal);
            Assert.Contains("\"request_adapter\": true", coverage, StringComparison.Ordinal);
            Assert.Contains("\"immutable_worker_command\": true", coverage, StringComparison.Ordinal);
            Assert.Contains("\"grpc_service\": true", coverage, StringComparison.Ordinal);
            Assert.Contains("\"service_registration\": true", coverage, StringComparison.Ordinal);
            Assert.Contains("\"capability\": true", coverage, StringComparison.Ordinal);
            Assert.Contains("\"portable_conformance\": true", coverage, StringComparison.Ordinal);
            Assert.Contains("\"argument_family_assignment\": true", coverage, StringComparison.Ordinal);
            Assert.Contains("\"membership_id\": \"v0.2-wave1-initial\"", coverage, StringComparison.Ordinal);
            Assert.Contains("\"membership_id\": \"v0.2-wave2-initial\"", coverage, StringComparison.Ordinal);
            Assert.Contains("\"catalog_id\": \"briosa.sa.2026.1.0529.7\"", coverage, StringComparison.Ordinal);
            Assert.Contains(
                "\"collection_operations.list_points_in_group\"",
                coverage,
                StringComparison.Ordinal);

            var documentation = File.ReadAllText(Path.Combine(
                outputRoot,
                "docs",
                "reference",
                "generated",
                "sa",
                "2026.1.0529.7",
                "operations.md"));
            Assert.Contains("Replay safety: `safe`", documentation, StringComparison.Ordinal);
            Assert.Contains("Replay safety: `unknown`", documentation, StringComparison.Ordinal);
            Assert.Contains(
                "Execution scope: `global_state_read`",
                documentation,
                StringComparison.Ordinal);
            Assert.Contains("`v0.2-wave1-initial` (`v0.2`, `wave_1`): 5 operation(s)", documentation, StringComparison.Ordinal);
            Assert.Contains("`v0.2-wave2-initial` (`v0.2`, `wave_2`): 15 operation(s)", documentation, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void ExplicitFieldNumbersDoNotFollowMutableMpOrdinals()
    {
        var repositoryRoot = FindRepositoryRoot();
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            $"briosa-generator-field-numbers-{Guid.NewGuid():N}");
        var catalogRoot = Path.Combine(temporaryRoot, "catalog");
        var outputRoot = Path.Combine(temporaryRoot, "output");
        try
        {
            CopyDirectory(Path.Combine(repositoryRoot.FullName, "catalog"), catalogRoot);
            CopyDirectory(
                Path.Combine(repositoryRoot.FullName, "proto"),
                Path.Combine(temporaryRoot, "proto"));
            var operationPath = Path.Combine(
                catalogRoot,
                "sa",
                "2026.1.0529.7",
                "operations",
                "file_operations.get_working_directory.json");
            var operation = JsonNode.Parse(File.ReadAllText(operationPath))!.AsObject();
            operation["arguments"]![0]!["ordinal"] = 42;
            File.WriteAllText(operationPath, operation.ToJsonString(new() { WriteIndented = true }));

            _ = CommandCatalogGenerator.Generate(catalogRoot, outputRoot);

            var proto = File.ReadAllText(Path.Combine(
                outputRoot,
                "proto",
                "briosa",
                "sa",
                "v2026_1_0529_7",
                "v1alpha1",
                "file_operations.proto"));
            Assert.Contains("optional string directory = 1", proto, StringComparison.Ordinal);
            Assert.DoesNotContain("directory = 43", proto, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void GenerationRefusesToOverwriteANonCatalogFile()
    {
        var repositoryRoot = FindRepositoryRoot();
        var outputRoot = Path.Combine(
            Path.GetTempPath(),
            $"briosa-generator-overwrite-{Guid.NewGuid():N}");
        var protectedPath = Path.Combine(
            outputRoot,
            "proto",
            "briosa",
            "sa",
            "v2026_1_0529_7",
            "v1alpha1",
            "file_operations.proto");
        const string sentinel = "syntax = \"proto3\"; // Maintainer-owned sentinel.\n";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(protectedPath)!);
            File.WriteAllText(protectedPath, sentinel);

            var exception = Assert.Throws<InvalidDataException>(() =>
                CommandCatalogGenerator.Generate(
                    Path.Combine(repositoryRoot.FullName, "catalog"),
                    outputRoot));

            Assert.Contains(
                "Refusing to overwrite non-catalog-generated file",
                exception.Message,
                StringComparison.Ordinal);
            Assert.Equal(sentinel, File.ReadAllText(protectedPath));
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

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
}
