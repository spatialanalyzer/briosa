using System.Globalization;
using System.Reflection;
using Briosa.Server.Operations;
using Briosa.Server.Operations.UtilityOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

/// <summary>
/// The D2 guard (#243): for every row of <see cref="ArgumentDefaultsTable"/>, builds the
/// real MP command with the field absent, explicitly empty, and set, and requires the
/// reviewed major-3 outcome. A rejection is the <see cref="ArgumentException"/> that
/// <c>OperationExecutor</c> maps to InvalidArgument and NotStarted
/// (<c>request-validation-failed</c>) before anything is dispatched.
/// </summary>
public sealed class ArgumentDefaultsTests
{
    private const string RejectedUnlessPrefix = "rejected_unless:";
    private const string SendsDefaultPrefix = "sends_default:";

    private static readonly string[] AbsentOutcomes = ["omitted", "rejected", "sends_blank", "removed"];
    private static readonly string[] EmptyOutcomes =
        ["rejected", "sends_blank", "sends_all_limits_disabled", "not_applicable", "removed"];
    private static readonly string[] ValueOutcomes = ["sent", "removed"];

    public static TheoryData<string> RowKeys()
    {
        var keys = new TheoryData<string>();
        foreach (var row in ArgumentDefaultsTable.Rows)
        {
            keys.Add(Key(row));
        }

        return keys;
    }

    [Fact]
    public void TableUsesTheReviewedVocabularyAndListsEachArgumentOnce()
    {
        Assert.Equal(
            ArgumentDefaultsTable.Rows.Count,
            ArgumentDefaultsTable.Rows.Select(Key).Distinct(StringComparer.Ordinal).Count());
        foreach (var row in ArgumentDefaultsTable.Rows)
        {
            Assert.True(
                AbsentOutcomes.Contains(row.Absent, StringComparer.Ordinal) ||
                row.Absent.StartsWith(RejectedUnlessPrefix, StringComparison.Ordinal) ||
                row.Absent.StartsWith(SendsDefaultPrefix, StringComparison.Ordinal),
                $"{Key(row)} has an unknown absent outcome '{row.Absent}'.");
            Assert.Contains(row.ExplicitEmpty, EmptyOutcomes);
            Assert.Contains(row.Value, ValueOutcomes);
            var removed = new[] { row.Absent, row.ExplicitEmpty, row.Value }
                .Count(value => value == "removed");
            Assert.True(removed is 0 or 3, $"{Key(row)} must be removed in all three columns or none.");
        }
    }

    [Theory]
    [MemberData(nameof(RowKeys))]
    public void MappingFollowsTheReviewedRow(string key)
    {
        var row = Assert.Single(ArgumentDefaultsTable.Rows, candidate => Key(candidate) == key);
        if (row.Value == "removed")
        {
            Assert.DoesNotContain(SpatialAnalyzerApi.Operations, operation => operation.OperationId == row.OperationId);
            return;
        }

        var operation = Assert.Single(SpatialAnalyzerApi.Operations, candidate => candidate.OperationId == row.OperationId);
        var separator = row.ProtoField.IndexOf('.', StringComparison.Ordinal);
        var messageName = row.ProtoField[..separator];
        Assert.Equal(operation.Rpc + "Request", messageName);
        var requestType = typeof(Api.ScaleObjectsRequest).Assembly.GetType("Briosa." + messageName, throwOnError: true)!;
        var create = typeof(SpatialAnalyzerApi).Assembly.GetTypes()
            .Select(type => type.GetMethod("CreateCommand", BindingFlags.Public | BindingFlags.Static, [requestType]))
            .Single(method => method is not null)!;
        var field = Descriptor(requestType).FindFieldByName(row.ProtoField[(separator + 1)..]);
        Assert.NotNull(field);

        var baseline = Filled(requestType);

        // Set: the argument is sent once, with the reviewed label and SDK binding.
        Assert.NotNull(Argument(Build(create, baseline), row));

        // Absent.
        var absent = Copy(baseline);
        field.Accessor.Clear(absent);
        if (row.Absent.StartsWith(RejectedUnlessPrefix, StringComparison.Ordinal))
        {
            var guard = Descriptor(requestType).FindFieldByName(row.Absent[RejectedUnlessPrefix.Length..]);
            Assert.NotNull(guard);
            guard.Accessor.SetValue(absent, false);
            Assert.Null(Build(create, absent));
            guard.Accessor.SetValue(absent, true);
            Assert.True(IsZero(Argument(Build(create, absent), row)!.Value), $"{key}: expected the zero value.");
        }
        else if (row.Absent.StartsWith(SendsDefaultPrefix, StringComparison.Ordinal))
        {
            var value = Argument(Build(create, absent), row)!.Value;
            Assert.Equal(row.Absent[SendsDefaultPrefix.Length..], Render(value));
        }
        else
        {
            AssertOutcome(row.Absent, create, absent, row);
        }

        // Explicitly empty.
        if (row.ExplicitEmpty == "not_applicable")
        {
            Assert.True(
                field.FieldType is FieldType.Bool or FieldType.Double or FieldType.Float or FieldType.Int32 or
                    FieldType.Int64 or FieldType.UInt32 or FieldType.Enum && !field.IsRepeated,
                $"{key}: only scalar and enum fields have no empty form.");
            return;
        }

        foreach (var empty in EmptyVariants(baseline, field))
        {
            AssertOutcome(row.ExplicitEmpty, create, empty, row);
        }
    }

    [Fact]
    public async Task RejectedArgumentFailsValidationAtThePublicSeamBeforeDispatch()
    {
        var worker = new RecordingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<UtilityOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var client = new Api.UtilityOperations.UtilityOperationsClient(grpcHost.Channel);

        // R3: a ScaleObjects request without a scale factor is no longer sent as 0.
        var exception = await Assert.ThrowsAsync<RpcException>(async () => await client.ScaleObjectsAsync(new()
        {
            Objects = { new Api.CollectionObjectName { CollectionName = "A", ObjectName = "Plane" } }
        }).ConfigureAwait(true)).ConfigureAwait(true);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        var error = Api.OperationError.Parser.ParseFrom(Assert.Single(
            exception.Trailers, entry => entry.Key == GrpcOperationOutcomeMapper.ErrorTrailerName).ValueBytes);
        Assert.Equal("utility_operations.scale_objects", error.OperationId);
        Assert.Equal(Api.OperationFailureKind.Validation, error.Kind);
        Assert.Equal("request-validation-failed", error.DiagnosticCode);
        Assert.Equal(Api.ExecutionDisposition.NotStarted, error.ExecutionDisposition);
        Assert.Empty(worker.Commands);
    }

    private static string Key(ArgumentDefaultRow row) => $"{row.OperationId} {row.ProtoField} {row.MpArgument}";

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Completed, WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }

    private static void AssertOutcome(string outcome, MethodInfo create, IMessage request, ArgumentDefaultRow row)
    {
        var command = Build(create, request);
        switch (outcome)
        {
            case "rejected":
                Assert.True(command is null, $"{Key(row)}: expected a request validation failure.");
                break;
            case "omitted":
                Assert.True(command is not null, $"{Key(row)}: expected the request to be accepted.");
                Assert.DoesNotContain(command.InputArguments, argument => argument.Name == row.MpArgument);
                break;
            case "sends_blank":
                Assert.True(IsBlank(Argument(command, row)!.Value), $"{Key(row)}: expected a blank value.");
                break;
            case "sends_all_limits_disabled":
                var limits = Assert.IsType<WorkerToleranceVectorOptionsValue>(Argument(command, row)!.Value);
                Assert.All(
                    typeof(WorkerToleranceVectorOptionsValue).GetProperties()
                        .Where(property => property.PropertyType == typeof(WorkerToleranceLimit))
                        .Select(property => (WorkerToleranceLimit)property.GetValue(limits)!),
                    limit => Assert.False(limit.Enabled));
                break;
            default:
                Assert.Fail($"{Key(row)}: unknown outcome '{outcome}'.");
                break;
        }
    }

    private static WorkerMpInputArgument? Argument(WorkerMpCommand? command, ArgumentDefaultRow row)
    {
        Assert.True(command is not null, $"{Key(row)}: expected the request to be accepted.");
        var matches = command.InputArguments
            .Where(argument => argument.Name == row.MpArgument && argument.SdkBinding == row.SdkBinding)
            .ToList();
        Assert.True(matches.Count == 1, $"{Key(row)}: expected one '{row.MpArgument}' argument, found {matches.Count}.");
        return matches[0];
    }

    // Null means the mapping rejected the request before dispatch.
    private static WorkerMpCommand? Build(MethodInfo create, IMessage request)
    {
        try
        {
            return (WorkerMpCommand)create.Invoke(null, [request])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is ArgumentException)
        {
            return null;
        }
    }

    private static bool IsBlank(WorkerMpValue value) => value switch
    {
        WorkerTextValue text => text.Value.Length == 0,
        WorkerCollectionObjectNameValue name => name.CollectionName.Length == 0 && name.ObjectName.Length == 0,
        WorkerStringListValue list => list.Values.Count == 0,
        _ => false
    };

    private static bool IsZero(WorkerMpValue value) => value switch
    {
        WorkerDoubleValue number => number.Value == 0,
        WorkerIntegerValue number => number.Value == 0,
        WorkerBooleanValue flag => !flag.Value,
        _ => false
    };

    private static string Render(WorkerMpValue value) => value switch
    {
        WorkerBooleanValue flag => flag.Value ? "true" : "false",
        WorkerDoubleValue number => number.Value.ToString(CultureInfo.InvariantCulture),
        WorkerIntegerValue number => number.Value.ToString(CultureInfo.InvariantCulture),
        WorkerTextValue text => text.Value,
        _ => value.ToString() ?? string.Empty
    };

    // Explicit empty, per field type: an empty or whitespace string; a name message
    // whose name is empty or whitespace while its collection is set; a message with
    // no limits; an empty list.
    private static IEnumerable<IMessage> EmptyVariants(IMessage baseline, FieldDescriptor field)
    {
        if (field.IsRepeated)
        {
            var empty = Copy(baseline);
            field.Accessor.Clear(empty);
            yield return empty;
            yield break;
        }

        foreach (var blank in new[] { string.Empty, "   " })
        {
            var request = Copy(baseline);
            if (field.FieldType == FieldType.String)
            {
                field.Accessor.SetValue(request, blank);
            }
            else
            {
                Assert.Equal(FieldType.Message, field.FieldType);
                var value = (IMessage)Activator.CreateInstance(field.MessageType.ClrType)!;
                foreach (var inner in field.MessageType.Fields.InDeclarationOrder())
                {
                    if (inner.FieldType == FieldType.String && !inner.IsRepeated)
                    {
                        inner.Accessor.SetValue(value, inner.Name is "collection_name" or "group_name" ? "C" : blank);
                    }
                }

                field.Accessor.SetValue(request, value);
            }

            yield return request;
        }
    }

    private static MessageDescriptor Descriptor(Type requestType) =>
        (MessageDescriptor)requestType.GetProperty("Descriptor")!.GetValue(null)!;

    private static IMessage Copy(IMessage source)
    {
        var copy = (IMessage)Activator.CreateInstance(source.GetType())!;
        copy.MergeFrom(source.ToByteString());
        return copy;
    }

    // A request with every field set to a distinct valid value.
    private static IMessage Filled(Type type)
    {
        var message = (IMessage)Activator.CreateInstance(type)!;
        foreach (var field in message.Descriptor.Fields.InDeclarationOrder())
        {
            if (field.IsRepeated)
            {
                var list = (System.Collections.IList)field.Accessor.GetValue(message);
                if (field.FieldType == FieldType.Message)
                {
                    list.Add(Filled(field.MessageType.ClrType));
                }
                else if (field.FieldType == FieldType.String)
                {
                    list.Add("Item");
                }

                continue;
            }

            object? value = field.FieldType switch
            {
                FieldType.Message => Filled(field.MessageType.ClrType),
                FieldType.String => "Name",
                FieldType.Bool => true,
                FieldType.Double => 1.5,
                FieldType.Float => 1.5f,
                FieldType.Int32 => 5,
                FieldType.UInt32 => 5u,
                FieldType.Int64 => 5L,
                FieldType.Enum => Enum.ToObject(
                    field.EnumType.ClrType, field.EnumType.Values.First(value => value.Number != 0).Number),
                _ => null
            };
            if (value is not null)
            {
                field.Accessor.SetValue(message, value);
            }
        }

        return message;
    }
}
