using System.Text.Json;
using System.Text.Json.Nodes;
using Briosa.Generator;

namespace Briosa.Generator.Tests;

public sealed class CommandCatalogValidatorTests
{
    [Fact]
    public void CommittedCatalogPassesReleaseValidation()
    {
        var result = CommandCatalogValidator.ValidateDirectory(FindCatalogRoot());

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.Equal(1, result.CatalogCount);
        Assert.Equal(6, result.OperationCount);
    }

    [Fact]
    public void InitialWaveOneMembershipIsAnExactSupportedCatalogSubset()
    {
        var releasePath = Path.Combine(
            FindCatalogRoot(),
            "sa",
            "2026.1.0529.7",
            "release-memberships",
            "v0.2-wave1-initial.json");
        var membership = JsonNode.Parse(File.ReadAllText(releasePath))!.AsObject();

        Assert.Equal("v0.2-wave1-initial", membership["membership_id"]!.GetValue<string>());
        Assert.Equal("v0.2", membership["release_line"]!.GetValue<string>());
        Assert.Equal("wave_1", membership["delivery_wave"]!.GetValue<string>());
        Assert.Equal(5, membership["operation_ids"]!.AsArray().Count);
        Assert.DoesNotContain(
            membership["operation_ids"]!.AsArray(),
            value => value!.GetValue<string>() == "file_operations.get_working_directory");
    }

    [Fact]
    public void ReleaseMembershipCannotReferenceAnUnsupportedOperation()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditReleaseMembership(release =>
            release["operation_ids"]!.AsArray().Add("collection_operations.unsupported"));

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "operation_id 'collection_operations.unsupported' is not present",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ReleaseMembershipRejectsDuplicateOperationIds()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditReleaseMembership(release =>
            release["operation_ids"]!.AsArray().Add(
                "collection_operations.list_points_in_group"));

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "duplicate operation_id 'collection_operations.list_points_in_group'",
                StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("catalog_id", "briosa.sa.2025.9", "catalog_id")]
    [InlineData("spatial_analyzer_target", "2025.9", "spatial_analyzer_target")]
    [InlineData("catalog_revision", "99", "catalog_revision")]
    public void ReleaseMembershipCoordinatesMustMatchTheOwningCatalog(
        string property,
        string value,
        string expectedError)
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditReleaseMembership(release => release[property] = value);

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains(expectedError, StringComparison.Ordinal) &&
                error.Contains("must be", StringComparison.Ordinal));
    }

    [Fact]
    public void UnlistedReleaseMembershipFileFailsClosed()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditManifest(manifest =>
            manifest["release_membership_files"]!.AsArray().Clear());

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "must list every release-memberships/*.json file exactly once",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ListedButMissingReleaseMembershipFileFailsClosed()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditManifest(manifest =>
            manifest["release_membership_files"] = new JsonArray(
                "release-memberships/v0.2-wave1-missing.json"));

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "release membership file 'release-memberships/v0.2-wave1-missing.json' does not exist",
                StringComparison.Ordinal));
    }

    [Fact]
    public void GetWorkingDirectoryRetainsItsResultOnlyGetter()
    {
        var operationPath = Path.Combine(
            FindCatalogRoot(),
            "sa",
            "2026.1.0529.7",
            "operations",
            "file_operations.get_working_directory.json");
        var operation = JsonNode.Parse(File.ReadAllText(operationPath))!.AsObject();
        var argument = operation["arguments"]!.AsArray().Single()!.AsObject();

        Assert.Equal("Get Working Directory", operation["mp_step"]!.GetValue<string>());
        Assert.Equal(
            "documentation:FileOperations/GetWorkingDirectory.htm",
            operation["inventory_key"]!.GetValue<string>());
        Assert.Equal("global_state_read", operation["execution_scope"]!.GetValue<string>());
        Assert.Equal("safe", operation["risk"]!["replay_safety"]!.GetValue<string>());
        Assert.Equal("Directory", argument["mp_name"]!.GetValue<string>());
        Assert.Equal(0, argument["sdk_order"]!.GetValue<int>());
        Assert.Null(argument["field_numbers"]!["request"]);
        Assert.Equal(1, argument["field_numbers"]!["result"]!.GetValue<int>());
        Assert.Equal("output", argument["direction"]!.GetValue<string>());
        Assert.Equal("yes", argument["result_only"]!.GetValue<string>());
        Assert.Equal("string", argument["semantic_type"]!.GetValue<string>());
        Assert.Equal("path", argument["data_classification"]!.GetValue<string>());
        Assert.Null(argument["sdk_binding"]!["setter"]);
        Assert.Equal(
            "GetStringArg",
            argument["sdk_binding"]!["getter"]!.GetValue<string>());
    }

    [Fact]
    public void UnknownExecutionScopeFailsReleaseValidation()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation => operation["execution_scope"] = "unknown");

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("unknown execution_scope", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingIsolationReviewFailsReleaseValidation()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation =>
            operation["documentation"]!.AsObject().Remove("isolation"));

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("isolation", StringComparison.Ordinal));
    }

    [Fact]
    public void MutatingOperationRequiresReviewedReplaySafety()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation =>
        {
            operation["risk"]!["effect"] = "mutating";
            operation["risk"]!.AsObject().Remove("replay_safety");
        });

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("replay_safety", StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownArgumentDataClassificationFailsReleaseValidation()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation =>
            operation["arguments"]![0]!["data_classification"] = "unknown");

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("unknown data_classification", StringComparison.Ordinal));
    }

    [Fact]
    public void RiskFlagsMustUseDeterministicOrder()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation =>
            operation["risk"]!["flags"] = new JsonArray("network", "filesystem_metadata"));

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("risk.flags must use ordinal sort order", StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownArgumentDirectionFailsReleaseValidation()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation =>
            operation["arguments"]![0]!["direction"] = "unknown");

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("unknown direction", StringComparison.Ordinal));
    }

    [Fact]
    public void RequiredInputWithoutSetterFailsReleaseValidation()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation =>
        {
            var argument = operation["arguments"]![0]!.AsObject();
            argument["direction"] = "input";
            argument["result_only"] = "no";
            argument["sdk_binding"] = new JsonObject
            {
                ["status"] = "unavailable",
                ["setter"] = null,
                ["getter"] = null
            };
            argument["input"] = new JsonObject
            {
                ["presence"] = "required",
                ["omission_behavior"] = "reject_request",
                ["default"] = new JsonObject
                {
                    ["status"] = "none"
                }
            };
        });

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("available SDK binding", StringComparison.Ordinal));
        Assert.Contains(
            result.Errors,
            error => error.Contains("requires an SDK setter", StringComparison.Ordinal));
    }

    [Fact]
    public void GeneratedSampleCannotBecomeAnImplicitCatalogDefault()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation =>
        {
            var argument = operation["arguments"]![0]!.AsObject();
            argument["direction"] = "input";
            argument["result_only"] = "no";
            argument["sdk_binding"] = new JsonObject
            {
                ["status"] = "available",
                ["setter"] = "SetStringArg",
                ["getter"] = null
            };
            argument["input"] = new JsonObject
            {
                ["presence"] = "optional",
                ["omission_behavior"] = "set_catalog_default",
                ["default"] = new JsonObject
                {
                    ["status"] = "generated_sample",
                    ["value"] = string.Empty
                }
            };
        });

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("only when that default is reviewed", StringComparison.Ordinal));
    }

    [Fact]
    public void DeprecatedOperationRequiresDetails()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation => operation["stability"] = "deprecated");

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "deprecated operations require deprecation details",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ActiveOperationCannotCarryDeprecationDetails()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation =>
            operation["deprecation"] = new JsonObject
            {
                ["reason"] = "Use a replacement."
            });

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "only deprecated operations may include deprecation details",
                StringComparison.Ordinal));
    }

    [Fact]
    public void DeprecatedOperationAcceptsReasonAndReplacement()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation =>
        {
            operation["stability"] = "deprecated";
            operation["deprecation"] = new JsonObject
            {
                ["reason"] = "Use the replacement operation.",
                ["replacement_operation_id"] = "file_operations.replacement"
            };
        });

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
    }

    [Fact]
    public void DuplicateMpStepNamesAreAllowedForDistinctOperations()
    {
        using var fixture = CatalogFixture.Create();
        fixture.AddOperation(
            "file_operations.read_working_directory",
            "FileOperations",
            "ReadWorkingDirectory");

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.Equal(7, result.OperationCount);
    }

    [Fact]
    public void PackageWideRpcAndMessageCollisionsFailClosed()
    {
        using var fixture = CatalogFixture.Create();
        fixture.AddProtocolPartition("Other", "other", "Other", "other.proto");
        fixture.AddOperation(
            "other.get_working_directory",
            "Other",
            "GetWorkingDirectory",
            "Other");

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("package-wide RPC name", StringComparison.Ordinal));
        Assert.Contains(
            result.Errors,
            error => error.Contains("package symbol 'GetWorkingDirectoryRequest'", StringComparison.Ordinal));
        Assert.Contains(
            result.Errors,
            error => error.Contains("package symbol 'GetWorkingDirectoryResult'", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => CommandCatalogGenerator.Generate(
            fixture.Root,
            Path.Combine(fixture.Root, "generated-collision-output")));
    }

    [Fact]
    public void CategoryAliasCollisionsFailClosedInsteadOfReceivingSuffixes()
    {
        using var fixture = CatalogFixture.Create();
        fixture.AddProtocolPartition(
            "Other File Operations",
            "file_operations",
            "FileOperations",
            "file_operations.proto");

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("duplicates a category alias", StringComparison.Ordinal));
        Assert.Contains(
            result.Errors,
            error => error.Contains("collides on service", StringComparison.Ordinal));
        Assert.Contains(
            result.Errors,
            error => error.Contains("collides on proto_file", StringComparison.Ordinal));
    }

    [Fact]
    public void ServicesCannotCollideWithGeneratedMessageTypes()
    {
        using var fixture = CatalogFixture.Create();
        fixture.AddProtocolPartition(
            "Request Collision",
            "get_working_directory_request",
            "GetWorkingDirectoryRequest",
            "get_working_directory_request.proto");

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "package symbol 'GetWorkingDirectoryRequest'",
                StringComparison.Ordinal));
    }

    [Fact]
    public void CategoryFilesCannotReplaceFixedExactTargetProtocolFiles()
    {
        using var fixture = CatalogFixture.Create();
        fixture.AddProtocolPartition(
            "Values",
            "values",
            "Values",
            "values.proto");

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "proto_file 'values.proto' collides with an existing non-catalog exact-target protocol file",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ServicesCannotReplaceFixedExactTargetPackageSymbols()
    {
        using var fixture = CatalogFixture.Create();
        fixture.AddProtocolPartition(
            "Point Name",
            "point_name",
            "PointName",
            "point_name.proto");

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("package symbol 'PointName'", StringComparison.Ordinal) &&
                error.Contains("fixed protocol file 'values.proto'", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingFixedProtocolContextFailsClosed()
    {
        using var fixture = CatalogFixture.Create();
        fixture.RemoveProtocolContext();

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "fixed package filenames and symbols cannot be validated",
                StringComparison.Ordinal));
    }

    [Fact]
    public void FieldNumbersAreExplicitAndIndependentFromMpOrdinals()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation => operation["arguments"]![0]!["ordinal"] = 42);

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
    }

    [Fact]
    public void ReservedFieldNumbersFailClosed()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation =>
        {
            var first = operation["arguments"]![0]!.AsObject();
            first["field_numbers"]!["result"] = 1000;
        });

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("reserved result field number 1000", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateFieldNumbersFailClosed()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation =>
        {
            var first = operation["arguments"]![0]!.AsObject();
            var second = first.DeepClone().AsObject();
            second["argument_id"] = "other_directory";
            second["ordinal"] = 1;
            second["sdk_order"] = 1;
            operation["arguments"]!.AsArray().Add(second);
        });

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("duplicates result field number 1", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateSdkOrderFailsIndependentlyFromDocumentedOrdinal()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditOperation(operation =>
        {
            var first = operation["arguments"]![0]!.AsObject();
            var second = first.DeepClone().AsObject();
            second["argument_id"] = "other_directory";
            second["ordinal"] = 1;
            second["field_numbers"]!["result"] = 2;
            operation["arguments"]!.AsArray().Add(second);
        });

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("duplicate argument sdk_order 0", StringComparison.Ordinal));
    }

    [Fact]
    public void TargetDirectoryAndManifestIdentityMustMatch()
    {
        using var fixture = CatalogFixture.Create();
        fixture.EditManifest(manifest =>
            manifest["spatial_analyzer_target"] = "2026.1.0529.8");

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.Contains(
            result.Errors,
            error => error.Contains("spatial_analyzer_target", StringComparison.Ordinal));
    }

    private static string FindCatalogRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(
            directory?.FullName ??
                throw new DirectoryNotFoundException("Could not locate the Briosa repository root."),
            "catalog");
    }

    private sealed class CatalogFixture : IDisposable
    {
        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true
        };

        private CatalogFixture(string workspaceRoot, string root)
        {
            WorkspaceRoot = workspaceRoot;
            Root = root;
        }

        private string WorkspaceRoot { get; }

        public string Root { get; }

        private string TargetDirectory => Path.Combine(Root, "sa", "2026.1.0529.7");

        private string ManifestPath => Path.Combine(TargetDirectory, "catalog.json");

        private string OperationPath => Path.Combine(
            TargetDirectory,
            "operations",
            "file_operations.get_working_directory.json");

        private string ReleaseMembershipPath => Path.Combine(
            TargetDirectory,
            "release-memberships",
            "v0.2-wave1-initial.json");

        public static CatalogFixture Create()
        {
            var workspaceRoot = Path.Combine(
                Path.GetTempPath(),
                $"briosa-catalog-tests-{Guid.NewGuid():N}");
            var catalogRoot = Path.Combine(workspaceRoot, "catalog");
            var repositoryRoot = Directory.GetParent(FindCatalogRoot())!.FullName;
            CopyDirectory(FindCatalogRoot(), catalogRoot);
            CopyDirectory(
                Path.Combine(repositoryRoot, "proto"),
                Path.Combine(workspaceRoot, "proto"));
            return new CatalogFixture(workspaceRoot, catalogRoot);
        }

        public void EditManifest(Action<JsonObject> edit) => EditJson(ManifestPath, edit);

        public void EditOperation(Action<JsonObject> edit) => EditJson(OperationPath, edit);

        public void EditReleaseMembership(Action<JsonObject> edit) =>
            EditJson(ReleaseMembershipPath, edit);

        public void RemoveProtocolContext()
        {
            var protocolRoot = Path.Combine(WorkspaceRoot, "proto");
            if (Directory.Exists(protocolRoot))
            {
                Directory.Delete(protocolRoot, recursive: true);
            }
        }

        public void AddOperation(
            string operationId,
            string service,
            string rpc,
            string category = "File Operations")
        {
            var operation = JsonNode.Parse(File.ReadAllText(OperationPath))!.AsObject();
            operation["operation_id"] = operationId;
            operation["inventory_key"] = $"synthetic:{operationId}";
            operation["category"] = category;
            operation["protocol"] = new JsonObject
            {
                ["service"] = service,
                ["rpc"] = rpc,
                ["request"] = $"{rpc}Request",
                ["result"] = $"{rpc}Result"
            };

            var relativePath = $"operations/{operationId}.json";
            WriteJson(Path.Combine(TargetDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)), operation);
            EditManifest(manifest =>
            {
                var files = manifest["operation_files"]!.AsArray();
                files.Add(relativePath);
                var sorted = files
                    .Select(node => node!.GetValue<string>())
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(path => JsonValue.Create(path))
                    .ToArray();
                files.Clear();
                foreach (var path in sorted)
                {
                    files.Add(path);
                }
            });
        }

        public void AddProtocolPartition(
            string category,
            string alias,
            string service,
            string protoFile)
        {
            EditManifest(manifest =>
            {
                var partitions = manifest["protocol_partitions"]!.AsArray();
                partitions.Add(new JsonObject
                {
                    ["category"] = category,
                    ["alias"] = alias,
                    ["service"] = service,
                    ["proto_file"] = protoFile
                });
                var sorted = partitions
                    .Select(node => node!.DeepClone())
                    .OrderBy(node => node!["alias"]!.GetValue<string>(), StringComparer.Ordinal)
                    .ToArray();
                partitions.Clear();
                foreach (var partition in sorted)
                {
                    partitions.Add(partition);
                }
            });
        }

        public void Dispose()
        {
            if (Directory.Exists(WorkspaceRoot))
            {
                Directory.Delete(WorkspaceRoot, recursive: true);
            }
        }

        private static void EditJson(string path, Action<JsonObject> edit)
        {
            var document = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            edit(document);
            WriteJson(path, document);
        }

        private static void WriteJson(string path, JsonObject document)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(
                path,
                document.ToJsonString(WriteOptions) + Environment.NewLine);
        }

        private static void CopyDirectory(string source, string destination)
        {
            foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
            }

            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                var destinationPath = Path.Combine(destination, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Copy(file, destinationPath);
            }
        }
    }
}
