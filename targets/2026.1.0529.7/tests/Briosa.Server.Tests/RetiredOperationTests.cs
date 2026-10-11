using System.Reflection;
using System.Text.Json;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Google.Protobuf.Reflection;

namespace Briosa.Server.Tests;

/// <summary>
/// Operations retired by a reviewed decision (D2 point 5 on #243, decided on #308 and #310)
/// are removed as complete vertical slices. The reviewed ledger in
/// docs/development/retired-operations.json is also what eng/Verify-Protocol.ps1 uses to
/// accept exactly these protobuf removals, so these tests keep the ledger and the code in step.
/// </summary>
public sealed class RetiredOperationTests
{
    public static TheoryData<string> RetiredOperationIds()
    {
        var data = new TheoryData<string>();
        foreach (var operation in LoadLedger().Operations)
        {
            data.Add(operation.OperationId);
        }

        return data;
    }

    [Fact]
    public void LedgerIsReviewedForThisTargetAndThePendingContractMajor()
    {
        var ledger = LoadLedger();
        Assert.Equal(1, ledger.SchemaVersion);
        Assert.Equal(SpatialAnalyzerApi.TargetVersion, ledger.Target);
        Assert.True(File.Exists(Path.Combine(TargetRoot(), ledger.Evidence)), ledger.Evidence);

        // A retirement is accepted only into the next contract major, or the current one
        // once the freeze has bumped it. Older entries must be removed with that major.
        var major = CompatibilityMajor();
        Assert.InRange(ledger.RetiredInContractMajor, major, major + 1);

        Assert.NotEmpty(ledger.Operations);
        AssertDistinct(ledger.Operations.Select(operation => operation.OperationId));
        AssertDistinct(ledger.Operations.Select(operation => $"{operation.Service}/{operation.Rpc}"));
        AssertDistinct(ledger.Operations.SelectMany(operation =>
            new[] { operation.Request, operation.Result }.Concat(operation.Enums)));
        Assert.All(ledger.Operations, operation =>
        {
            Assert.StartsWith("briosa.", operation.Service, StringComparison.Ordinal);
            Assert.Equal($"briosa.{operation.Rpc}Request", operation.Request);
            Assert.Equal($"briosa.{operation.Rpc}Result", operation.Result);
            Assert.All(operation.Enums, name => Assert.StartsWith("briosa.", name, StringComparison.Ordinal));
            Assert.False(string.IsNullOrWhiteSpace(operation.MpStep));
            Assert.False(string.IsNullOrWhiteSpace(operation.Reason));
        });
    }

    [Theory]
    [MemberData(nameof(RetiredOperationIds))]
    public void RetiredOperationIsNotRegisteredRoutedOrClassified(string operationId)
    {
        var retired = LoadLedger().Operations.Single(operation => operation.OperationId == operationId);
        var method = $"/{retired.Service}/{retired.Rpc}";

        Assert.DoesNotContain(SpatialAnalyzerApi.Operations, operation =>
            operation.OperationId == retired.OperationId ||
            operation.FullyQualifiedMethod == method ||
            string.Equals(operation.MpStep, retired.MpStep, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(OperationClassification.Rows, row => row.OperationId == retired.OperationId);
        Assert.False(OperationClassification.Find(retired.OperationId).IsReviewed);
    }

    [Theory]
    [MemberData(nameof(RetiredOperationIds))]
    public void RetiredOperationHasNoGeneratedRpcMessageOrEnum(string operationId)
    {
        var retired = LoadLedger().Operations.Single(operation => operation.OperationId == operationId);
        var protocol = GeneratedProtocol();

        Assert.DoesNotContain($"{retired.Service}.{retired.Rpc}", protocol.Methods);
        Assert.DoesNotContain(retired.Request, protocol.Types);
        Assert.DoesNotContain(retired.Result, protocol.Types);
        Assert.All(retired.Enums, name => Assert.DoesNotContain(name, protocol.Types));
    }

    [Fact]
    public void RetiredOperationsHaveNoOperationClassOrServiceOverride()
    {
        var retired = LoadLedger().Operations.Select(operation => operation.Rpc).ToHashSet(StringComparer.Ordinal);
        var serverTypes = typeof(SpatialAnalyzerApi).Assembly.GetTypes();

        Assert.DoesNotContain(serverTypes, type => type.Name.EndsWith("Operation", StringComparison.Ordinal) &&
            retired.Contains(type.Name[..^"Operation".Length]));
        Assert.DoesNotContain(
            serverTypes.SelectMany(type => type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)),
            method => method.DeclaringType!.Name.EndsWith("Service", StringComparison.Ordinal) &&
                retired.Contains(method.Name));
    }

    internal static RetiredOperationLedger LoadLedger()
    {
        var path = Path.Combine(TargetRoot(), "docs", "development", "retired-operations.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var operations = root.GetProperty("operations").EnumerateArray()
            .Select(operation => new RetiredOperation(
                operation.GetProperty("operation_id").GetString()!,
                operation.GetProperty("mp_step").GetString()!,
                operation.GetProperty("service").GetString()!,
                operation.GetProperty("rpc").GetString()!,
                operation.GetProperty("request").GetString()!,
                operation.GetProperty("result").GetString()!,
                [.. operation.GetProperty("enums").EnumerateArray().Select(name => name.GetString()!)],
                operation.GetProperty("reason").GetString()!))
            .ToArray();
        return new RetiredOperationLedger(
            root.GetProperty("schema_version").GetInt32(),
            root.GetProperty("spatial_analyzer_target").GetString()!,
            root.GetProperty("retired_in_contract_major").GetInt32(),
            root.GetProperty("evidence").GetString()!,
            operations);
    }

    private static (HashSet<string> Methods, HashSet<string> Types) GeneratedProtocol()
    {
        var methods = new HashSet<string>(StringComparer.Ordinal);
        var types = new HashSet<string>(StringComparer.Ordinal);
        var files = typeof(global::Briosa.ConstructionOperations).Assembly.GetTypes()
            .Select(type => type.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static))
            .Where(property => property?.PropertyType == typeof(FileDescriptor))
            .Select(property => (FileDescriptor)property!.GetValue(null)!)
            .ToList();
        Assert.Contains(files, file => file.Name == "briosa/construction_operations.proto");

        void AddMessage(MessageDescriptor message)
        {
            types.Add(message.FullName);
            foreach (var nested in message.NestedTypes)
            {
                AddMessage(nested);
            }

            foreach (var nested in message.EnumTypes)
            {
                types.Add(nested.FullName);
            }
        }

        foreach (var file in files)
        {
            foreach (var message in file.MessageTypes)
            {
                AddMessage(message);
            }

            foreach (var enumeration in file.EnumTypes)
            {
                types.Add(enumeration.FullName);
            }

            foreach (var service in file.Services)
            {
                foreach (var method in service.Methods)
                {
                    methods.Add($"{service.FullName}.{method.Name}");
                }
            }
        }

        return (methods, types);
    }

    private static int CompatibilityMajor()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(TargetRoot(), "compatibility.json")));
        return document.RootElement.GetProperty("major").GetInt32();
    }

    private static void AssertDistinct(IEnumerable<string> values)
    {
        var list = values.ToList();
        Assert.Equal(list.Count, list.Distinct(StringComparer.Ordinal).Count());
    }

    private static string TargetRoot()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Briosa.slnx")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        return root.FullName;
    }

    internal sealed record RetiredOperationLedger(
        int SchemaVersion,
        string Target,
        int RetiredInContractMajor,
        string Evidence,
        IReadOnlyList<RetiredOperation> Operations);

    internal sealed record RetiredOperation(
        string OperationId,
        string MpStep,
        string Service,
        string Rpc,
        string Request,
        string Result,
        IReadOnlyList<string> Enums,
        string Reason);
}
