using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Briosa.Generator;

public static class InteropApiManifest
{
    public static string Create(string assemblyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        if (!peReader.HasMetadata)
        {
            throw new InvalidDataException($"'{assemblyPath}' is not a managed assembly.");
        }

        var reader = peReader.GetMetadataReader();
        if (!reader.IsAssembly)
        {
            throw new InvalidDataException($"'{assemblyPath}' does not contain an assembly manifest.");
        }

        var methodOwners = BuildMethodOwnerMap(reader);
        var lines = new List<string>
        {
            "# Briosa SpatialAnalyzer interop public API",
            "# Volatile PE headers and the module MVID are intentionally excluded."
        };

        AddAssembly(reader, methodOwners, lines);
        AddReferences(reader, lines);

        var types = reader.TypeDefinitions
            .Select(handle => (Handle: handle, Name: GetTypeName(reader, handle)))
            .Where(item => item.Name != "<Module>")
            .OrderBy(item => item.Name, StringComparer.Ordinal);

        foreach (var item in types)
        {
            AddType(reader, item.Handle, item.Name, methodOwners, lines);
        }

        return string.Join('\n', lines) + "\n";
    }

    private static void AddAssembly(
        MetadataReader reader,
        Dictionary<MethodDefinitionHandle, string> methodOwners,
        List<string> lines)
    {
        var assembly = reader.GetAssemblyDefinition();
        var culture = assembly.Culture.IsNil ? "neutral" : reader.GetString(assembly.Culture);
        lines.Add($"assembly {reader.GetString(assembly.Name)} version={assembly.Version} culture={culture} flags=0x{(int)assembly.Flags:X8} public-key={Blob(reader, assembly.PublicKey)}");

        AddCustomAttributes(reader, assembly.GetCustomAttributes(), methodOwners, "  ", lines);
    }

    private static void AddReferences(MetadataReader reader, List<string> lines)
    {
        var references = reader.AssemblyReferences
            .Select(handle => reader.GetAssemblyReference(handle))
            .Select(reference =>
            {
                var culture = reference.Culture.IsNil ? "neutral" : reader.GetString(reference.Culture);
                return $"reference {reader.GetString(reference.Name)} version={reference.Version} culture={culture} flags=0x{(int)reference.Flags:X8} key={Blob(reader, reference.PublicKeyOrToken)} hash={Blob(reader, reference.HashValue)}";
            })
            .OrderBy(value => value, StringComparer.Ordinal);

        foreach (var reference in references)
        {
            lines.Add(reference);
        }
    }

    private static void AddType(
        MetadataReader reader,
        TypeDefinitionHandle handle,
        string typeName,
        Dictionary<MethodDefinitionHandle, string> methodOwners,
        List<string> lines)
    {
        var type = reader.GetTypeDefinition(handle);
        lines.Add($"type {typeName} attributes=0x{(int)type.Attributes:X8} base={EntityName(reader, type.BaseType)}");
        AddCustomAttributes(reader, type.GetCustomAttributes(), methodOwners, "  ", lines);

        var interfaces = type.GetInterfaceImplementations()
            .Select(interfaceHandle => reader.GetInterfaceImplementation(interfaceHandle))
            .Select(implementation => $"  interface {EntityName(reader, implementation.Interface)}")
            .OrderBy(value => value, StringComparer.Ordinal);
        AddAll(interfaces, lines);

        var fields = type.GetFields()
            .Select(fieldHandle => reader.GetFieldDefinition(fieldHandle))
            .Select(field => $"  field {reader.GetString(field.Name)} attributes=0x{(int)field.Attributes:X8} signature={Blob(reader, field.Signature)}")
            .OrderBy(value => value, StringComparer.Ordinal);
        AddAll(fields, lines);

        var methods = type.GetMethods()
            .Select(methodHandle => (Handle: methodHandle, Definition: reader.GetMethodDefinition(methodHandle)))
            .OrderBy(item => reader.GetString(item.Definition.Name), StringComparer.Ordinal)
            .ThenBy(item => Blob(reader, item.Definition.Signature), StringComparer.Ordinal);

        foreach (var item in methods)
        {
            var method = item.Definition;
            lines.Add($"  method {reader.GetString(method.Name)} attributes=0x{(int)method.Attributes:X8} impl=0x{(int)method.ImplAttributes:X4} signature={Blob(reader, method.Signature)}");
            AddCustomAttributes(reader, method.GetCustomAttributes(), methodOwners, "    ", lines);

            var parameters = method.GetParameters()
                .Select(parameterHandle => reader.GetParameter(parameterHandle))
                .OrderBy(parameter => parameter.SequenceNumber);
            foreach (var parameter in parameters)
            {
                lines.Add($"    parameter sequence={parameter.SequenceNumber} name={reader.GetString(parameter.Name)} attributes=0x{(int)parameter.Attributes:X4}");
                AddCustomAttributes(reader, parameter.GetCustomAttributes(), methodOwners, "      ", lines);
            }
        }

        var properties = type.GetProperties()
            .Select(propertyHandle => reader.GetPropertyDefinition(propertyHandle))
            .Select(property => $"  property {reader.GetString(property.Name)} attributes=0x{(int)property.Attributes:X4} signature={Blob(reader, property.Signature)}")
            .OrderBy(value => value, StringComparer.Ordinal);
        AddAll(properties, lines);

        var events = type.GetEvents()
            .Select(eventHandle => reader.GetEventDefinition(eventHandle))
            .Select(eventDefinition => $"  event {reader.GetString(eventDefinition.Name)} attributes=0x{(int)eventDefinition.Attributes:X4} type={EntityName(reader, eventDefinition.Type)}")
            .OrderBy(value => value, StringComparer.Ordinal);
        AddAll(events, lines);
    }

    private static void AddCustomAttributes(
        MetadataReader reader,
        CustomAttributeHandleCollection handles,
        Dictionary<MethodDefinitionHandle, string> methodOwners,
        string indent,
        List<string> lines)
    {
        var attributes = handles
            .Select(handle => reader.GetCustomAttribute(handle))
            .Select(attribute => $"{indent}attribute {ConstructorName(reader, attribute.Constructor, methodOwners)} value={Blob(reader, attribute.Value)}")
            .OrderBy(value => value, StringComparer.Ordinal);
        AddAll(attributes, lines);
    }

    private static string ConstructorName(
        MetadataReader reader,
        EntityHandle constructor,
        Dictionary<MethodDefinitionHandle, string> methodOwners)
    {
        return constructor.Kind switch
        {
            HandleKind.MemberReference => MemberReferenceName(reader, (MemberReferenceHandle)constructor),
            HandleKind.MethodDefinition => MethodDefinitionName(reader, (MethodDefinitionHandle)constructor, methodOwners),
            _ => constructor.Kind.ToString()
        };
    }

    private static string MemberReferenceName(MetadataReader reader, MemberReferenceHandle handle)
    {
        var member = reader.GetMemberReference(handle);
        return $"{EntityName(reader, member.Parent)}::{reader.GetString(member.Name)} signature={Blob(reader, member.Signature)}";
    }

    private static string MethodDefinitionName(
        MetadataReader reader,
        MethodDefinitionHandle handle,
        Dictionary<MethodDefinitionHandle, string> methodOwners)
    {
        var method = reader.GetMethodDefinition(handle);
        var owner = methodOwners.TryGetValue(handle, out var typeName) ? typeName : "<unknown>";
        return $"{owner}::{reader.GetString(method.Name)} signature={Blob(reader, method.Signature)}";
    }

    private static Dictionary<MethodDefinitionHandle, string> BuildMethodOwnerMap(MetadataReader reader)
    {
        var owners = new Dictionary<MethodDefinitionHandle, string>();
        foreach (var typeHandle in reader.TypeDefinitions)
        {
            var name = GetTypeName(reader, typeHandle);
            foreach (var methodHandle in reader.GetTypeDefinition(typeHandle).GetMethods())
            {
                owners[methodHandle] = name;
            }
        }

        return owners;
    }

    private static string EntityName(MetadataReader reader, EntityHandle handle)
    {
        if (handle.IsNil)
        {
            return "<nil>";
        }

        return handle.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeName(reader, (TypeDefinitionHandle)handle),
            HandleKind.TypeReference => GetTypeReferenceName(reader, (TypeReferenceHandle)handle),
            HandleKind.TypeSpecification => $"typespec:{Blob(reader, reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature)}",
            HandleKind.AssemblyReference => reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)handle).Name),
            HandleKind.ModuleReference => reader.GetString(reader.GetModuleReference((ModuleReferenceHandle)handle).Name),
            HandleKind.MemberReference => MemberReferenceName(reader, (MemberReferenceHandle)handle),
            HandleKind.MethodDefinition => reader.GetString(reader.GetMethodDefinition((MethodDefinitionHandle)handle).Name),
            _ => handle.Kind.ToString()
        };
    }

    private static string GetTypeReferenceName(MetadataReader reader, TypeReferenceHandle handle)
    {
        var type = reader.GetTypeReference(handle);
        var namespaceName = reader.GetString(type.Namespace);
        var typeName = reader.GetString(type.Name);
        var fullName = string.IsNullOrEmpty(namespaceName) ? typeName : $"{namespaceName}.{typeName}";
        return $"[{EntityName(reader, type.ResolutionScope)}]{fullName}";
    }

    private static string GetTypeName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var type = reader.GetTypeDefinition(handle);
        var typeName = reader.GetString(type.Name);
        var declaringType = type.GetDeclaringType();
        if (!declaringType.IsNil)
        {
            return $"{GetTypeName(reader, declaringType)}+{typeName}";
        }

        var namespaceName = reader.GetString(type.Namespace);
        return string.IsNullOrEmpty(namespaceName) ? typeName : $"{namespaceName}.{typeName}";
    }

    private static string Blob(MetadataReader reader, BlobHandle handle) =>
        handle.IsNil ? "" : Convert.ToHexString(reader.GetBlobBytes(handle));

    private static void AddAll(IEnumerable<string> values, List<string> lines)
    {
        foreach (var value in values)
        {
            lines.Add(value);
        }
    }
}
