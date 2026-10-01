using System.Reflection;
using System.Runtime.CompilerServices;
using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Operations.VectorOperations;
using Briosa.Server.Services;
using Briosa.Worker.Control;

namespace Briosa.Server.Tests;

// Every declared output kind must have a production SDK getter; otherwise MP success
// would be followed by an unavailable output and a data-loss outcome.
public sealed class OperationOutputRetrievabilityTests
{
    [Fact]
    public void EveryDeclaredOperationOutputKindIsRetrievableByTheWorker()
    {
        var violations = DeclaredOutputs()
            .SelectMany(item => item.Contracts.Select(contract => (item.Declaration, Contract: contract)))
            .Where(item => !WorkerRetrievableOutputKinds.Contains(item.Contract.Kind))
            .Select(item => $"{item.Declaration}: {item.Contract.FieldName} ({item.Contract.Kind})")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void OutputDeclarationScanFindsStaticOperationContracts()
    {
        var declarations = DeclaredOutputs();

        Assert.Contains(declarations, item =>
            item.Declaration == $"{typeof(MakeCollectionVectorGroupNameRefListRuntimeSelectOperation).FullName}.OutputContracts" &&
            item.Contracts.Any(contract => contract.Kind == WorkerMpValueKind.CollectionVectorGroupNameList));
        Assert.Contains(declarations, item =>
            item.Declaration == $"{typeof(GetVectorGroupPropertiesOperation).FullName}.OutputContracts" &&
            item.Contracts.Any(contract => contract.ArgumentName == "% Vectors In Tolerance"));
        Assert.True(declarations.Length >= SpatialAnalyzerApi.Operations.Count / 2);
    }

    private static (string Declaration, OperationOutputContract[] Contracts)[] DeclaredOutputs()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.DeclaredOnly;
        return typeof(SpatialAnalyzerApi).Assembly.GetTypes()
            .Where(type => !type.ContainsGenericParameters)
            .SelectMany(type => type.GetProperties(flags)
                .Where(property => property.GetIndexParameters().Length == 0 &&
                    typeof(IEnumerable<OperationOutputContract>).IsAssignableFrom(property.PropertyType))
                .Select(property => (Name: $"{type.FullName}.{property.Name}", Value: property.GetValue(null)))
                .Concat(type.GetFields(flags)
                    .Where(field => !field.IsDefined(typeof(CompilerGeneratedAttribute)) &&
                        typeof(IEnumerable<OperationOutputContract>).IsAssignableFrom(field.FieldType))
                    .Select(field => (Name: $"{type.FullName}.{field.Name}", Value: field.GetValue(null)))))
            .Select(member => (member.Name,
                ((IEnumerable<OperationOutputContract>?)member.Value ?? []).ToArray()))
            .ToArray();
    }
}
