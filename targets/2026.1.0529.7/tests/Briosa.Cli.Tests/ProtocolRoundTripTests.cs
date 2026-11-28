using System.Collections;
using System.Text;
using System.Text.Json.Nodes;
using Briosa.Cli.Engine;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Briosa.Cli.Tests;

/// <summary>
/// Pins the strict JSON path for every request and result message of every operation
/// this target registers. The covered method set is compared with the handwritten
/// registry, so a shrinking discovery cannot shrink coverage silently.
/// </summary>
public sealed class ProtocolRoundTripTests
{
    private const int RegisteredOperationCount = 1018;
    private static readonly JsonParser ResultParser = new(JsonParser.Settings.Default.WithIgnoreUnknownFields(false));

    [Fact]
    public void TheRegisteredOperationSetIsPinnedAndMatchesDiscoveryAndTheCompiledSchema()
    {
        var registry = ServerProjection.Registry;
        Assert.Equal(RegisteredOperationCount, registry.Count);
        var methods = registry.Select(operation => operation.FullyQualifiedMethod).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(registry.Count, methods.Count);
        Assert.True(methods.SetEquals(ServerProjection.AllCapabilities.Operations.Select(operation =>
            operation.FullyQualifiedMethod)));
        foreach (var operation in registry)
        {
            var method = ProtocolCatalog.Instance.FindMethod(operation.FullyQualifiedMethod);
            Assert.NotNull(method);
            Assert.False(method.IsClientStreaming || method.IsServerStreaming);
            Assert.NotNull(ProtocolCatalog.FindExecutionField(method.OutputType));
        }

        Assert.Equal(25, ProtocolCatalog.Instance.Files.Count);
    }

    [Fact]
    public void EveryRegisteredRequestAndResultRoundTripsThroughTheStrictPath()
    {
        var registry = ServerProjection.Registry;
        var covered = new HashSet<string>(StringComparer.Ordinal);
        var exercisedArms = new HashSet<string>(StringComparer.Ordinal);
        var allArms = new HashSet<string>(StringComparer.Ordinal);
        var failures = new List<string>();
        var cases = 0;
        foreach (var operation in registry)
        {
            var method = ProtocolCatalog.Instance.FindMethod(operation.FullyQualifiedMethod)!;
            covered.Add(operation.FullyQualifiedMethod);
            foreach (var (descriptor, isRequest) in new[] { (method.InputType, true), (method.OutputType, false) })
            {
                foreach (var oneof in Reachable(descriptor).SelectMany(message => message.Oneofs)
                             .Where(oneof => !oneof.IsSynthetic))
                {
                    allArms.UnionWith(oneof.Fields.Select(field => field.FullName));
                }

                foreach (var (label, instance) in Instances(descriptor, exercisedArms))
                {
                    cases++;
                    var failure = isRequest ? RoundTripRequest(instance) : RoundTripResult(instance);
                    if (failure is not null)
                    {
                        failures.Add($"{operation.OperationId} {descriptor.FullName} {label}: {failure}");
                    }
                }
            }
        }

        Assert.Empty(failures);
        Assert.True(covered.SetEquals(registry.Select(operation => operation.FullyQualifiedMethod)));
        Assert.NotEmpty(allArms);
        Assert.True(allArms.SetEquals(exercisedArms), "Every arm of every real oneof must be exercised.");
        Assert.True(cases >= RegisteredOperationCount * 2 * 4, $"Only {cases} cases ran.");
    }

    [Fact]
    public void EveryFieldOfEveryRegisteredMessageIsAKindTheStrictPathSupports()
    {
        foreach (var message in RegisteredMessages())
        {
            foreach (var field in message.Fields.InDeclarationOrder())
            {
                Assert.True(StrictJsonInput.IsSupported(field), $"{field.FullName} needs a reviewed input rule.");
            }
        }
    }

    [Fact]
    public void NoRegisteredRequestNestsDeeperThanTheInputLimit()
    {
        foreach (var operation in ServerProjection.Registry)
        {
            var method = ProtocolCatalog.Instance.FindMethod(operation.FullyQualifiedMethod)!;
            Assert.True(Depth(method.InputType, []) < Wire.MaximumDepth, method.InputType.FullName);
        }
    }

    [Fact]
    public void TwoArmsOfOneOneofAreRejectedForEveryRealOneof()
    {
        var messages = RegisteredMessages().Append(GracefulShutdownBound.Descriptor)
            .Where(message => message.Oneofs.Any(oneof => !oneof.IsSynthetic && oneof.Fields.Count > 1))
            .ToArray();
        Assert.NotEmpty(messages);
        foreach (var message in messages)
        {
            foreach (var oneof in message.Oneofs.Where(oneof => !oneof.IsSynthetic && oneof.Fields.Count > 1))
            {
                var combined = new JsonObject();
                foreach (var arm in oneof.Fields.Take(2))
                {
                    var instance = message.Parser.ParseFrom(ByteString.Empty);
                    arm.Accessor.SetValue(instance, Sample(arm, 0, 0, SampleMode.Populated, 1, []));
                    combined[arm.Name] = JsonNode.Parse(ProtocolJson.Format(instance))![arm.Name]!.DeepClone();
                }

                var parsed = StrictJsonInput.Parse(Encoding.UTF8.GetBytes(combined.ToJsonString()), message);
                Assert.NotNull(parsed.Rejection);
                Assert.Equal(Codes.OneofConflict, parsed.Rejection.Code);
                Assert.Equal(oneof.Name, parsed.Rejection.FieldPath);
            }
        }
    }

    [Fact]
    public void ExplicitZeroAndFalseKeepTheirPresence()
    {
        var descriptor = ExportEventRefListRequest.Descriptor;
        var parsed = StrictJsonInput.Parse("""{"decimal_precision":0,"overwrite_existing_file":false}"""u8, descriptor);
        Assert.Null(parsed.Rejection);
        var omitted = StrictJsonInput.Parse("{}"u8, descriptor);
        foreach (var name in new[] { "decimal_precision", "overwrite_existing_file" })
        {
            var field = descriptor.FindFieldByName(name);
            Assert.True(field.HasPresence);
            Assert.True(field.Accessor.HasValue(parsed.Message!));
            Assert.False(field.Accessor.HasValue(omitted.Message!));
        }

        Assert.NotEqual(parsed.Message!.ToByteString(), omitted.Message!.ToByteString());
        // The formatter writes the explicit values back, so the CLI's own output is valid input.
        Assert.True(JsonNode.DeepEquals(
            JsonNode.Parse("""{"event_list":[],"decimal_precision":0,"overwrite_existing_file":false}"""),
            JsonNode.Parse(ProtocolJson.Format(parsed.Message!))));
    }

    [Fact]
    public void EnumsAreReadByNameOnlyAndUnknownNumbersInResultsArePreserved()
    {
        var descriptor = GetIthPointFromGroupRequest.Descriptor;
        Assert.Null(StrictJsonInput.Parse("""{"group_name":{"object_type":"OBJECT_TYPE_POINT_GROUP"}}"""u8,
            descriptor).Rejection);
        Assert.Equal(Codes.EnumNameRequired,
            StrictJsonInput.Parse("""{"group_name":{"object_type":1}}"""u8, descriptor).Rejection?.Code);
        Assert.Equal(Codes.UnknownEnumName,
            StrictJsonInput.Parse("""{"group_name":{"object_type":"POINT_GROUP"}}"""u8, descriptor).Rejection?.Code);

        // A result carries whatever the server sends; an unknown number survives intact.
        var result = new GetIthPointFromGroupResult
        {
            Execution = new MpExecutionDetails { State = (MpExecutionState)99, MpResultCode = 2 }
        };
        var json = ProtocolJson.Format(result);
        Assert.Equal(99, JsonNode.Parse(json)!["execution"]!["state"]!.GetValue<int>());
        Assert.Equal(result.ToByteString(), ResultParser.Parse(json, GetIthPointFromGroupResult.Descriptor).ToByteString());
        var known = new MpExecutionDetails { State = MpExecutionState.PartialSuccess, MpResultCode = 4 };
        Assert.Equal("MP_EXECUTION_STATE_PARTIAL_SUCCESS",
            JsonNode.Parse(ProtocolJson.Format(known))!["state"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("""{"finite_milliseconds":"18446744073709551615"}""", null)]
    [InlineData("""{"finite_milliseconds":"0"}""", null)]
    [InlineData("""{"finite_milliseconds":18446744073709551615}""", Codes.NumberForm)]
    [InlineData("""{"finite_milliseconds":"18446744073709551616"}""", Codes.OutOfRange)]
    [InlineData("""{"finite_milliseconds":"-1"}""", Codes.NumberForm)]
    [InlineData("""{"finite_milliseconds":"01"}""", Codes.NumberForm)]
    [InlineData("""{"finite_milliseconds":"1e3"}""", Codes.NumberForm)]
    [InlineData("""{"finite_milliseconds":" 1"}""", Codes.NumberForm)]
    public void SixtyFourBitIntegersAreDecimalStrings(string json, string? rejection)
    {
        var parsed = StrictJsonInput.Parse(Encoding.UTF8.GetBytes(json), GracefulShutdownBound.Descriptor);
        Assert.Equal(rejection, parsed.Rejection?.Code);
        if (rejection is null)
        {
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(ProtocolJson.Format(parsed.Message!))));
        }
    }

    [Theory]
    [InlineData("\"NaN\"", null)]
    [InlineData("\"Infinity\"", null)]
    [InlineData("\"-Infinity\"", null)]
    [InlineData("1.5e-310", null)]
    [InlineData("-0", null)]
    [InlineData("\"nan\"", Codes.NumberForm)]
    [InlineData("\"1.5\"", Codes.NumberForm)]
    [InlineData("1e999", Codes.OutOfRange)]
    public void DoublesAreNumbersOrTheThreeNonFiniteNames(string value, string? rejection)
    {
        var request = $$$"""{"transform_in_working_coordinates":{"values":[{{{value}}}]}}""";
        var parsed = StrictJsonInput.Parse(Encoding.UTF8.GetBytes(request), ConstructFrameRequest.Descriptor);
        Assert.Equal(rejection, parsed.Rejection?.Code);
        if (rejection is null)
        {
            var again = StrictJsonInput.Parse(Encoding.UTF8.GetBytes(ProtocolJson.Format(parsed.Message!)),
                ConstructFrameRequest.Descriptor);
            Assert.Equal(parsed.Message!.ToByteString(), again.Message!.ToByteString());
        }
    }

    [Fact]
    public void ResultNumbersKeepTheirExactTextInTheEnvelope()
    {
        var result = new GetPointCoordinateResult
        {
            XValue = 0.1,
            YValue = 5e-324,
            ZValue = double.MaxValue,
            VectorRepresentation = new Vector { X = 123456789.12345678, Y = -0.0, Z = 1.0 / 3.0 },
            Execution = new MpExecutionDetails { State = MpExecutionState.Succeeded, MpResultCode = 2 }
        };
        var json = ProtocolJson.Format(result);
        var envelope = new BridgeEnvelope("call", BridgeOutcome.Succeeded) { Data = JsonNode.Parse(json) };
        var written = Encoding.UTF8.GetString(envelope.ToUtf8(0));
        foreach (var token in new[] { "0.1", "5e-324", "1.7976931348623157e+308", "123456789.12345678", "-0",
                     "0.3333333333333333" })
        {
            Assert.True(json.Contains(token, StringComparison.Ordinal), $"formatter lacks {token}: {json}");
            Assert.True(written.Contains(token, StringComparison.Ordinal), $"envelope lacks {token}: {written}");
        }

        var data = JsonNode.Parse(written)!["data"]!.ToJsonString();
        Assert.Equal(result.ToByteString(), ResultParser.Parse(data, GetPointCoordinateResult.Descriptor).ToByteString());
    }

    private static string? RoundTripRequest(IMessage instance)
    {
        var json = ProtocolJson.Format(instance);
        var parsed = StrictJsonInput.Parse(Encoding.UTF8.GetBytes(json), instance.Descriptor);
        if (parsed.Rejection is { } rejection)
        {
            return $"rejected {rejection.Code} at {rejection.FieldPath}";
        }

        return parsed.Message!.ToByteString().Equals(instance.ToByteString()) ? null : "bytes differ";
    }

    private static string? RoundTripResult(IMessage instance)
    {
        var json = ProtocolJson.Format(instance);
        var parsed = ResultParser.Parse(json, instance.Descriptor);
        if (!parsed.ToByteString().Equals(instance.ToByteString()))
        {
            return "bytes differ";
        }

        // The envelope embeds the JSON as a node; that must be lossless too.
        var embedded = ResultParser.Parse(JsonNode.Parse(json)!.ToJsonString(), instance.Descriptor);
        return embedded.ToByteString().Equals(instance.ToByteString()) ? null : "envelope embedding differs";
    }

    private static IEnumerable<(string Label, IMessage Instance)> Instances(MessageDescriptor descriptor,
        HashSet<string> exercisedArms)
    {
        yield return ("default", descriptor.Parser.ParseFrom(ByteString.Empty));
        var variants = Math.Max(1, Reachable(descriptor).SelectMany(message => message.Oneofs)
            .Where(oneof => !oneof.IsSynthetic).Select(oneof => oneof.Fields.Count).DefaultIfEmpty(1).Max());
        for (var variant = 0; variant < variants; variant++)
        {
            yield return ($"populated/{variant}", Populate(descriptor, 0, variant, SampleMode.Populated, exercisedArms));
        }

        yield return ("explicit-defaults", Populate(descriptor, 0, 0, SampleMode.Defaults, exercisedArms));
        yield return ("non-finite", Populate(descriptor, 0, 0, SampleMode.NonFinite, exercisedArms));
    }

    private enum SampleMode
    {
        Populated,
        Defaults,
        NonFinite
    }

    private static IMessage Populate(MessageDescriptor descriptor, int depth, int variant, SampleMode mode,
        HashSet<string> exercisedArms)
    {
        Assert.True(depth <= 12, $"Unexpected recursive schema at {descriptor.FullName}.");
        var message = descriptor.Parser.ParseFrom(ByteString.Empty);
        var chosen = descriptor.Oneofs.Where(oneof => !oneof.IsSynthetic)
            .ToDictionary(oneof => oneof, oneof => oneof.Fields[variant % oneof.Fields.Count]);
        foreach (var field in descriptor.Fields.InFieldNumberOrder())
        {
            if (field.RealContainingOneof is { } oneof)
            {
                if (chosen[oneof] != field)
                {
                    continue;
                }

                exercisedArms.Add(field.FullName);
            }

            if (field.IsRepeated)
            {
                var list = (IList)field.Accessor.GetValue(message);
                list.Add(Sample(field, depth, variant, mode, 1, exercisedArms));
                list.Add(Sample(field, depth, variant, mode, 2, exercisedArms));
            }
            else
            {
                field.Accessor.SetValue(message, Sample(field, depth, variant, mode, 1, exercisedArms));
            }
        }

        return message;
    }

    private static object Sample(FieldDescriptor field, int depth, int variant, SampleMode mode, int seed,
        HashSet<string> exercisedArms)
    {
        var defaults = mode == SampleMode.Defaults;
        return field.FieldType switch
        {
            FieldType.Message => Populate(field.MessageType, depth + 1, variant, mode, exercisedArms),
            FieldType.Bool => !defaults && seed == 1,
            FieldType.String => defaults ? string.Empty : $"value-{seed} \"quoted\" \\ / \u0001 é \U0001F600",
            FieldType.Double => mode switch
            {
                SampleMode.Defaults => 0d,
                SampleMode.NonFinite => seed == 1 ? double.NaN : (variant % 2 == 0 ? double.PositiveInfinity : double.NegativeInfinity),
                _ => seed == 1 ? 0.1 + depth : -1.7976931348623157e308
            },
            FieldType.Int32 or FieldType.SInt32 or FieldType.SFixed32 => defaults ? 0 : (seed == 1 ? int.MinValue : int.MaxValue),
            FieldType.UInt32 or FieldType.Fixed32 => defaults ? 0u : uint.MaxValue - (uint)seed,
            FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64 => defaults ? 0L : long.MinValue + seed,
            FieldType.UInt64 or FieldType.Fixed64 => defaults ? 0UL : ulong.MaxValue - (ulong)seed,
            FieldType.Enum => Enum.ToObject(field.EnumType.ClrType, defaults
                ? 0
                : field.EnumType.Values[(seed + variant) % field.EnumType.Values.Count].Number),
            _ => throw new InvalidOperationException($"No sample for {field.FullName}: {field.FieldType}")
        };
    }

    private static IEnumerable<MessageDescriptor> RegisteredMessages()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var operation in ServerProjection.Registry)
        {
            var method = ProtocolCatalog.Instance.FindMethod(operation.FullyQualifiedMethod)!;
            foreach (var message in Reachable(method.InputType).Concat(Reachable(method.OutputType)))
            {
                if (seen.Add(message.FullName))
                {
                    yield return message;
                }
            }
        }
    }

    private static IEnumerable<MessageDescriptor> Reachable(MessageDescriptor root)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<MessageDescriptor>([root]);
        while (pending.Count > 0)
        {
            var message = pending.Pop();
            if (!seen.Add(message.FullName))
            {
                continue;
            }

            yield return message;
            foreach (var field in message.Fields.InDeclarationOrder().Where(field => field.FieldType == FieldType.Message))
            {
                pending.Push(field.MessageType);
            }
        }
    }

    // JSON nesting levels: one per object, plus one per array.
    private static int Depth(MessageDescriptor message, HashSet<string> path)
    {
        Assert.True(path.Add(message.FullName), $"Recursive request schema at {message.FullName}.");
        var deepest = message.Fields.InDeclarationOrder()
            .Select(field => (field.IsRepeated ? 1 : 0) +
                (field.FieldType == FieldType.Message ? Depth(field.MessageType, path) : 0))
            .DefaultIfEmpty(0).Max();
        path.Remove(message.FullName);
        return 1 + deepest;
    }
}
