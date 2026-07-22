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
        Assert.Equal(1, result.OperationCount);
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
        Assert.Equal("Directory", argument["mp_name"]!.GetValue<string>());
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
    public void DuplicateMpStepNamesAreAllowedForDistinctOperations()
    {
        using var fixture = CatalogFixture.Create();
        fixture.AddOperation(
            "file_operations.read_working_directory",
            "FileOperations",
            "ReadWorkingDirectory");

        var result = CommandCatalogValidator.ValidateDirectory(fixture.Root);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.Equal(2, result.OperationCount);
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

        private CatalogFixture(string root)
        {
            Root = root;
        }

        public string Root { get; }

        private string TargetDirectory => Path.Combine(Root, "sa", "2026.1.0529.7");

        private string ManifestPath => Path.Combine(TargetDirectory, "catalog.json");

        private string OperationPath => Path.Combine(
            TargetDirectory,
            "operations",
            "file_operations.get_working_directory.json");

        public static CatalogFixture Create()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                $"briosa-catalog-tests-{Guid.NewGuid():N}");
            CopyDirectory(FindCatalogRoot(), root);
            return new CatalogFixture(root);
        }

        public void EditManifest(Action<JsonObject> edit) => EditJson(ManifestPath, edit);

        public void EditOperation(Action<JsonObject> edit) => EditJson(OperationPath, edit);

        public void AddOperation(string operationId, string service, string rpc)
        {
            var operation = JsonNode.Parse(File.ReadAllText(OperationPath))!.AsObject();
            operation["operation_id"] = operationId;
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

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
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
