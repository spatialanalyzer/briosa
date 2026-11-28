using System.Collections.Frozen;
using System.Reflection;
using Google.Protobuf.Reflection;

namespace Briosa.Cli.Engine;

/// <summary>
/// The compiled <c>Briosa.Protocol</c> descriptors. The bridge is a generic protocol
/// adapter: it reads request and result shapes from these standard descriptors and
/// never carries a per-operation table of its own.
/// </summary>
internal sealed class ProtocolCatalog
{
    /// <summary>The field number of <c>MpExecutionDetails execution</c> on every result.</summary>
    public const int ExecutionFieldNumber = 1000;

    private readonly FrozenDictionary<string, MethodDescriptor> _methods;

    private ProtocolCatalog(IReadOnlyList<FileDescriptor> files)
    {
        Files = files;
        _methods = files
            .SelectMany(file => file.Services)
            .SelectMany(service => service.Methods)
            .ToFrozenDictionary(method => PathOf(method), StringComparer.Ordinal);
    }

    public static ProtocolCatalog Instance { get; } = Create();

    /// <summary>Every compiled protocol file, ordered by name.</summary>
    public IReadOnlyList<FileDescriptor> Files { get; }

    /// <summary>Every compiled RPC, keyed by its fully qualified gRPC path.</summary>
    public IReadOnlyDictionary<string, MethodDescriptor> Methods => _methods;

    /// <summary>The gRPC path, for example <c>/briosa.FileOperations/GetWorkingDirectory</c>.</summary>
    public static string PathOf(MethodDescriptor method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return "/" + method.Service.FullName + "/" + method.Name;
    }

    public MethodDescriptor? FindMethod(string fullyQualifiedMethod) =>
        _methods.GetValueOrDefault(fullyQualifiedMethod);

    /// <summary>
    /// The <c>execution</c> field of a result message, or null when the message does
    /// not carry the protocol's MP execution details.
    /// </summary>
    public static FieldDescriptor? FindExecutionField(MessageDescriptor result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var field = result.FindFieldByNumber(ExecutionFieldNumber);
        return field is { FieldType: FieldType.Message, IsRepeated: false } &&
            field.MessageType.FullName == MpExecutionDetails.Descriptor.FullName
                ? field
                : null;
    }

    private static ProtocolCatalog Create()
    {
        // The generated "<File>Reflection" classes each expose the file descriptor.
        var files = typeof(OperationError).Assembly.GetTypes()
            .Where(type => type.IsClass && type.IsAbstract && type.IsSealed && type.IsPublic)
            .Select(type => type.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static))
            .Where(property => property?.PropertyType == typeof(FileDescriptor))
            .Select(property => (FileDescriptor)property!.GetValue(null)!)
            .DistinctBy(file => file.Name, StringComparer.Ordinal)
            .OrderBy(file => file.Name, StringComparer.Ordinal)
            .ToArray();
        return new ProtocolCatalog(files);
    }
}
