using System.Collections;
using System.Reflection;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Grpc.Net.Client;

namespace Briosa.Cli;

internal sealed class DescriptorProof
{
    private const string WorkingFrame = "/briosa.UtilityOperations/GetWorkingFrameProperties";
    private static readonly JsonFormatter Formatter = new(
        JsonFormatter.Settings.Default.WithPreserveProtoFieldNames(true));
    private readonly Dictionary<string, FileDescriptor> files = typeof(OperationError).Assembly.GetTypes()
        .Select(type => type.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static))
        .Where(property => property?.PropertyType == typeof(FileDescriptor))
        .Select(property => (FileDescriptor)property!.GetValue(null)!)
        .ToDictionary(file => file.Name, StringComparer.Ordinal);

    private Dictionary<string, MethodDescriptor> Methods => files.Values.SelectMany(file => file.Services)
        .SelectMany(service => service.Methods)
        .ToDictionary(method => $"/{method.Service.FullName}/{method.Name}", StringComparer.Ordinal);

    public async Task VerifyAsync(string descriptorPath, string address)
    {
        CompareDescriptorSet(descriptorPath);
        VerifyExamples();

        using var channel = GrpcChannel.ForAddress(address);
        var invoker = channel.CreateCallInvoker();
        var methods = Methods;
        var discovery = (ListCapabilitiesResponse)await CallAsync(
            invoker, methods["/briosa.DiscoveryService/ListCapabilities"],
            """{"include_denied":true}""").ConfigureAwait(false);

        var failures = new List<string>();
        var registered = new HashSet<string>(StringComparer.Ordinal);
        var messages = new HashSet<string>(StringComparer.Ordinal);
        var cases = 0;
        foreach (var operation in discovery.Operations)
        {
            Require(registered.Add(operation.FullyQualifiedMethod), operation.FullyQualifiedMethod);
            var method = methods[operation.FullyQualifiedMethod];
            foreach (var message in new[] { method.InputType, method.OutputType })
            {
                messages.Add(message.FullName);
                foreach (var populated in new[] { false, true })
                {
                    cases++;
                    try
                    {
                        var original = populated ? Populate(message, 0) : message.Parser.ParseFrom(ByteString.Empty);
                        var json = Formatter.Format(original);
                        var restored = JsonParser.Default.Parse(json, message);
                        Require(original.ToByteString().Equals(restored.ToByteString()), json);
                    }
                    catch (Exception exception) when (exception is InvalidOperationException or
                        InvalidJsonException or InvalidProtocolBufferException or ArgumentException)
                    {
                        failures.Add($"{method.FullName} {message.FullName} populated={populated}: {exception.Message}");
                    }
                }
            }
        }

        Console.WriteLine($"Registered operations={registered.Count}; distinct messages={messages.Count}; cases={cases}; failures={failures.Count}");
        foreach (var failure in failures)
        {
            await Console.Error.WriteLineAsync(failure).ConfigureAwait(false);
        }

        Require(failures.Count == 0, "One or more round trips failed.");
        var start = (StartSpatialAnalyzerSdkResponse)await CallAsync(invoker,
            methods["/briosa.SpatialAnalyzerSdkLifecycle/StartSpatialAnalyzerSdk"], "{}").ConfigureAwait(false);
        Require(start.State.ReadyForMp, $"Fake worker did not become ready: {Formatter.Format(start)}");
        var target = methods[WorkingFrame];
        var result = await CallAsync(invoker, target, "{}").ConfigureAwait(false);
        Console.WriteLine($"OK {target.FullName} {Formatter.Format(result)}");

        // The fake-server harness configures an explicit deny for this operation.
        var invalid = methods["/briosa.FileOperations/GetWorkingDirectory"];
        try
        {
            _ = await CallAsync(invoker, invalid, "{}").ConfigureAwait(false);
            throw new InvalidOperationException("Expected a typed operation failure.");
        }
        catch (RpcException exception)
        {
            var trailer = exception.Trailers.Single(entry =>
                entry.IsBinary && entry.Key == "briosa-operation-error-bin");
            var error = OperationError.Parser.ParseFrom(trailer.ValueBytes);
            Require(exception.StatusCode == StatusCode.PermissionDenied && error.DiagnosticCode.Length > 0,
                "Expected a typed policy denial.");
            Console.WriteLine($"{exception.StatusCode} {invalid.FullName} {Formatter.Format(error)}");
        }
    }

    private void CompareDescriptorSet(string descriptorPath)
    {
        var bytes = File.ReadAllBytes(descriptorPath);
        var set = FileDescriptorSet.Parser.ParseFrom(bytes);
        var runtime = FileDescriptor.BuildFromByteStrings(set.File.Select(file => file.ToByteString()));
        Require(files.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(runtime.Select(file => file.Name)),
            "File names differ.");
        foreach (var file in runtime)
        {
            var artifact = file.ToProto();
            var compiled = files[file.Name].ToProto();
            artifact.SourceCodeInfo = null;
            compiled.SourceCodeInfo = null;
            NormalizeJsonNames(artifact.MessageType, file.MessageTypes);
            NormalizeJsonNames(compiled.MessageType, files[file.Name].MessageTypes);
            Require(artifact.Equals(compiled), $"Descriptor differs: {file.Name}");
        }

        Console.WriteLine($"Descriptor: {bytes.Length} bytes; {runtime.Count} files; " +
            $"{runtime.Sum(file => file.Services.Count)} services; {Methods.Count} RPCs; " +
            $"{runtime.Sum(file => CountMessages(file.MessageTypes))} messages; " +
            $"{set.File.Sum(file => file.SourceCodeInfo?.Location.Count ?? 0)} source locations; " +
            $"{set.File.Sum(file => file.SourceCodeInfo?.Location.Count(HasComments) ?? 0)} with comments; " +
            $"{set.File.Sum(file => file.SourceCodeInfo?.Location.Count(location => HasComments(location) &&
                location.Path.Count == 4 && location.Path[0] == 6 && location.Path[2] == 2) ?? 0)} method comments");
    }

    private static bool HasComments(SourceCodeInfo.Types.Location location) =>
        location.HasLeadingComments || location.HasTrailingComments || location.LeadingDetachedComments.Count > 0;

    private static void NormalizeJsonNames(
        RepeatedField<DescriptorProto> definitions, IList<MessageDescriptor> descriptors)
    {
        for (var index = 0; index < definitions.Count; index++)
        {
            var definition = definitions[index];
            var descriptor = descriptors[index];
            foreach (var field in definition.Field)
            {
                field.JsonName = descriptor.FindFieldByNumber(field.Number).JsonName;
            }

            NormalizeJsonNames(definition.NestedType, descriptor.NestedTypes);
        }
    }

    private static int CountMessages(IList<MessageDescriptor> messages) =>
        messages.Count + messages.Sum(message => CountMessages(message.NestedTypes));

    private void VerifyExamples()
    {
        var request = FindMessage("ExportEventRefListRequest");
        var json = Formatter.Format(JsonParser.Default.Parse(
            """{"event_list":[{},{}],"decimal_precision":0,"overwrite_existing_file":false}""", request));
        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement;
        Require(properties.GetProperty("decimal_precision").GetInt32() == 0 &&
            !properties.GetProperty("overwrite_existing_file").GetBoolean() &&
            properties.GetProperty("event_list").GetArrayLength() == 2, json);
        var parsed = JsonParser.Default.Parse(json, request);
        var omitted = JsonParser.Default.Parse("{}", request);
        foreach (var name in new[] { "decimal_precision", "overwrite_existing_file" })
        {
            var field = request.FindFieldByName(name);
            Require(field.ContainingOneof.IsSynthetic && field.Accessor.HasValue(parsed) &&
                !field.Accessor.HasValue(omitted), name);
        }

        Console.WriteLine($"Optional, nested, and repeated: {json}");
        RejectJson(request, """{"unknown_field":true}""");
        var duration = FindMessage("GracefulShutdownBound");
        var oneof = duration.FindFieldByName("finite_milliseconds").ContainingOneof;
        var max = JsonParser.Default.Parse("""{"finite_milliseconds":"18446744073709551615"}""", duration);
        Require(!oneof.IsSynthetic && oneof.Accessor.GetCaseFieldDescriptor(max).Name == "finite_milliseconds",
            "Expected a real oneof selection.");
        Console.WriteLine($"Real oneof and uint64: {Formatter.Format(max)}");
        var result = FindMessage("GetCalibrationApplianceDataResult");
        var values = JsonParser.Default.Parse("""{"real_values":["NaN","Infinity","-Infinity"]}""", result);
        var restored = JsonParser.Default.Parse(Formatter.Format(values), result);
        Require(values.ToByteString().Equals(restored.ToByteString()), "Nonfinite values changed.");
        Console.WriteLine($"Nonfinite: {Formatter.Format(restored)}");

        var details = FindMessage("MpExecutionDetails");
        var state = details.Fields.InFieldNumberOrder().First(field => field.FieldType == FieldType.Enum);
        var known = state.EnumType.Values.First(value => value.Number != 0);
        foreach (var token in new[] { JsonSerializer.Serialize(known.Name), known.Number.ToString(
            System.Globalization.CultureInfo.InvariantCulture), "99999" })
        {
            var enumJson = $$"""{"{{state.Name}}":{{token}}}""";
            var canonical = Formatter.Format(JsonParser.Default.Parse(enumJson, details));
            Console.WriteLine($"Enum {enumJson} => {canonical}");
        }

        RejectJson(details, $$"""{"{{state.Name}}":"UNKNOWN_SPIKE_LITERAL"}""");
    }

    private MessageDescriptor FindMessage(string name) =>
        files.Values.SelectMany(file => file.MessageTypes).Single(message => message.Name == name);

    private static void RejectJson(MessageDescriptor descriptor, string json)
    {
        try
        {
            _ = JsonParser.Default.Parse(json, descriptor);
        }
        catch (InvalidProtocolBufferException exception)
        {
            Console.WriteLine($"Rejected {json}: {exception.Message}");
            return;
        }

        throw new InvalidOperationException($"Unexpectedly accepted {json}.");
    }

    private static async Task<IMessage> CallAsync(CallInvoker invoker, MethodDescriptor descriptor, string json)
    {
        Require(!descriptor.IsClientStreaming && !descriptor.IsServerStreaming, descriptor.FullName);
        var method = new Method<IMessage, IMessage>(MethodType.Unary, descriptor.Service.FullName,
            descriptor.Name, Marshal(descriptor.InputType), Marshal(descriptor.OutputType));
        using var call = invoker.AsyncUnaryCall(method, null,
            new CallOptions(deadline: DateTime.UtcNow.AddSeconds(15)),
            JsonParser.Default.Parse(json, descriptor.InputType));
        return await call.ResponseAsync.ConfigureAwait(false);
    }

    private static Marshaller<IMessage> Marshal(MessageDescriptor descriptor) =>
        Marshallers.Create<IMessage>(message => message.ToByteArray(), bytes => descriptor.Parser.ParseFrom(bytes));

    private static IMessage Populate(MessageDescriptor message, int depth)
    {
        Require(depth <= 12, $"Unexpected recursive schema at {message.FullName}");
        var value = message.Parser.ParseFrom(ByteString.Empty);
        var selected = new HashSet<OneofDescriptor>();
        foreach (var field in message.Fields.InFieldNumberOrder())
        {
            if (field.ContainingOneof is { IsSynthetic: false } oneof && !selected.Add(oneof))
            {
                continue;
            }

            Require(!field.IsMap, $"Map needs a fixture at {field.FullName}");
            if (field.IsRepeated)
            {
                var list = (IList)field.Accessor.GetValue(value);
                _ = list.Add(Sample(field, depth, 1));
                _ = list.Add(Sample(field, depth, 2));
            }
            else
            {
                field.Accessor.SetValue(value, Sample(field, depth, 1));
            }
        }

        return value;
    }

    private static object Sample(FieldDescriptor field, int depth, int seed) => field.FieldType switch
    {
        FieldType.Message or FieldType.Group => Populate(field.MessageType, depth + 1),
        FieldType.Bool => seed == 1,
        FieldType.String => $"spike-{seed}",
        FieldType.Bytes => ByteString.CopyFromUtf8($"spike-{seed}"),
        FieldType.Double => seed + 0.25,
        FieldType.Float => seed + 0.25f,
        FieldType.Int32 or FieldType.SInt32 or FieldType.SFixed32 => -seed,
        FieldType.UInt32 or FieldType.Fixed32 => (uint)seed,
        FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64 => long.MinValue + seed,
        FieldType.UInt64 or FieldType.Fixed64 => ulong.MaxValue - (ulong)seed,
        FieldType.Enum => Enum.ToObject(field.EnumType.ClrType,
            field.EnumType.Values.FirstOrDefault(value => value.Number != 0)?.Number ?? 0),
        _ => throw new InvalidOperationException($"No sample for {field.FullName}: {field.FieldType}")
    };

    private static void Require(bool condition, string error)
    {
        if (!condition)
        {
            throw new InvalidOperationException(error);
        }
    }
}
