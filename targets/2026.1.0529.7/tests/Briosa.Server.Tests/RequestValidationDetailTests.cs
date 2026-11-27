using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Briosa.Worker.Control;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Xunit.Abstractions;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

/// <summary>
/// #442: drives request validation failures through the real request mapping of every
/// registered operation. Each reported path must resolve, segment by segment, to a field
/// of that operation's request message; its constraint must fit the field and be one the
/// applied change can cause; it must name the field the change touched (or, for an unset
/// oneof, the message holding it); the server must publish it; and neither it nor the
/// exception may carry a request value. Any other exception from a mapping fails.
/// </summary>
public sealed class RequestValidationDetailTests(ITestOutputHelper output)
{
    internal const string TextSentinel = "zq7-sentinel-text";
    internal const int NumberSentinel = 424242;
    private const int MaximumDepth = 6;
    private const int RepairAttempts = 96;

    // The string fields that name an exact-target choice, by operation and field path, each
    // with a name the target accepts. The sweep cannot guess such a name, so it repairs the
    // field with this one. The table is written by hand and never learned from a mapping: a
    // mapping that rejects free-form text as an unsupported choice fails the sweep, and so
    // does an entry no mapping enforces. This target has no such field.
    private static readonly Dictionary<(string OperationId, string FieldPath), string> TextChoices = new();

    [Fact]
    public void EveryRegisteredOperationHasOneRequestMapping()
    {
        var operations = ImplementedOperations();
        Assert.Equal(SpatialAnalyzerApi.Operations.Count, operations.Length);
        Assert.Equal(
            SpatialAnalyzerApi.Operations.Select(operation => operation.OperationId),
            operations.Select(operation => operation.Descriptor.OperationId));
    }

    [Fact]
    public void ThePathCheckRejectsAPathThatIsNotARequestField()
    {
        var request = Api.ScaleObjectsRequest.Descriptor;
        Assert.Equal("scale_factor", Resolve(request, "scale_factor", out _)?.Name);
        Assert.Equal("target_name", Resolve(Api.CollimationRequest.Descriptor, "collimation_point.target_name", out _)?.Name);
        Assert.Equal("object_type", Resolve(request, "objects.object_type", out _)?.Name);
        Assert.Null(Resolve(request, "scale", out var unknown));
        Assert.Contains("'scale'", unknown, StringComparison.Ordinal);
        Assert.Null(Resolve(request, "objects.object_name.length", out var scalar));
        Assert.Contains("'length'", scalar, StringComparison.Ordinal);
        Assert.Null(Resolve(request, "objects..object_name", out _));
        Assert.Null(Resolve(request, "Objects", out _));
        Assert.NotNull(Misfit(request.FindFieldByName("objects"), Api.RequestFieldConstraint.NotBlank));
        Assert.Null(Misfit(request.FindFieldByName("objects"), Api.RequestFieldConstraint.NotEmpty));

        // The checks the registry sweep applies to each reported violation.
        var scaleAbsent = Change(request, "scale_factor", "absent");
        Assert.Empty(Problems(request, RequestFieldViolation.Required("scale_factor"), scaleAbsent));
        Assert.Contains(Problems(request, RequestFieldViolation.Required("objects"), scaleAbsent),
            problem => problem.Contains("for a change to 'scale_factor'", StringComparison.Ordinal));
        Assert.Contains(Problems(request, RequestFieldViolation.Range("scale_factor"), scaleAbsent),
            problem => problem.Contains("absent cannot cause Range", StringComparison.Ordinal));
        Assert.Contains(Problems(request, RequestFieldViolation.NotBlank("scale_factor"), null),
            problem => problem.Contains("not a string", StringComparison.Ordinal));
        Assert.Contains(Problems(request, RequestFieldViolation.Required(TextSentinel), null),
            problem => problem.Contains("does not resolve", StringComparison.Ordinal));
        Assert.Contains(Problems(request, RequestFieldViolation.Required("scale_factor.value"), null),
            problem => problem.Contains("does not resolve", StringComparison.Ordinal));

        // A blank name inside a present message is the name's failure, not the message's.
        var notes = Api.GetCollectionNotesRequest.Descriptor;
        var blankName = Change(notes, "collection.name", "empty");
        Assert.Empty(Problems(notes, RequestFieldViolation.NotBlank("collection.name"), blankName));
        Assert.Contains(Problems(notes, RequestFieldViolation.Required("collection"), blankName),
            problem => problem.Contains("for a change to 'collection.name'", StringComparison.Ordinal));
        Assert.Contains(Problems(notes, RequestFieldViolation.Required("collection.name"), blankName),
            problem => problem.Contains("empty cannot cause Required", StringComparison.Ordinal));

        // Only an unset oneof may be reported on the message above the changed field.
        var report = Api.DefineReportTemplateRequest.Descriptor;
        var destinationCleared = Change(report, "report_output_options.embedded_file", "absent");
        Assert.Empty(Problems(report, RequestFieldViolation.OneofRequired("report_output_options"), destinationCleared));
        Assert.Contains(Problems(report, RequestFieldViolation.Required("report_output_options"), destinationCleared),
            problem => problem.Contains("for a change to", StringComparison.Ordinal));
        var outputTypeCleared = Change(report, "report_output_options.output_type", "absent");
        Assert.Contains(Problems(report, RequestFieldViolation.OneofRequired("report_output_options"), outputTypeCleared),
            problem => problem.Contains("for a change to", StringComparison.Ordinal));
        // A oneof member set explicitly, even to an empty string, leaves the oneof set.
        var pathSelected = Change(report, "report_output_options.external_path", "empty", oneofUnset: false);
        Assert.Contains(Problems(report, RequestFieldViolation.OneofRequired("report_output_options"), pathSelected),
            problem => problem.Contains("for a change to", StringComparison.Ordinal));

        // A free-form string is not a named choice; a string the operation only accepts
        // from a target's list is.
        Assert.Contains(Problems(notes, RequestFieldViolation.SupportedChoice("collection.name"), blankName),
            problem => problem.Contains("is not a named choice", StringComparison.Ordinal));
        Assert.Empty(Problems(notes, RequestFieldViolation.SupportedChoice("collection.name"), blankName,
            new HashSet<string>(StringComparer.Ordinal) { "collection.name" }));
    }

    // The one deliberately generic argument failure: a null request names no field.
    [Fact]
    public void ANullRequestFailsWithAnUntypedArgumentNullException()
    {
        foreach (var operation in ImplementedOperations())
        {
            var exception = Build(operation, null).Exception;
            Assert.True(exception?.GetType() == typeof(ArgumentNullException),
                $"{operation.Descriptor.OperationId}: {exception?.GetType().Name ?? "accepted"}");
        }
    }

    [Fact]
    public void EveryReportedViolationNamesARealRequestFieldWithAFittingConstraintAndNoValue()
    {
        var problems = new List<string>();
        var unexpected = new List<string>();
        var unbuilt = new List<string>();
        var reported = new SortedSet<string>(StringComparer.Ordinal);
        var constraints = new Dictionary<Api.RequestFieldConstraint, int>();
        var mutations = 0;

        foreach (var operation in ImplementedOperations())
        {
            var id = operation.Descriptor.OperationId;
            // The string fields this operation accepts only from the target's list of names.
            var choicePaths = TextChoices.Keys
                .Where(key => key.OperationId == id)
                .Select(key => key.FieldPath)
                .ToHashSet(StringComparer.Ordinal);
            void Check(Outcome outcome, FieldChange? change, string mutation)
            {
                switch (outcome.Exception)
                {
                    case null:
                        return;
                    case RequestFieldViolation violation:
                        reported.Add($"{id} {violation.FieldPath} {violation.Constraint}");
                        constraints[violation.Constraint] = constraints.GetValueOrDefault(violation.Constraint) + 1;
                        foreach (var problem in Problems(operation.Request, violation, change, choicePaths))
                        {
                            problems.Add($"{id}: {mutation}: {problem}");
                        }

                        return;
                    default:
                        // Every check a mapping makes on a request reports a typed violation; a
                        // non-null request reaches no deliberately generic failure.
                        unexpected.Add($"{id}: {mutation}: {outcome.Exception.GetType().Name}");
                        return;
                }
            }

            var baseline = Baseline(operation, out var failure);

            var empty = (IMessage)Activator.CreateInstance(operation.Request.ClrType)!;
            Check(Build(operation, empty), null, "empty request");

            if (baseline is null)
            {
                unbuilt.Add($"{id}: {failure}");
                continue;
            }

            foreach (var path in Paths(baseline, ImmutableList<FieldDescriptor>.Empty, 0))
            {
                var field = path[^1];
                var text = string.Join('.', path.Select(segment => segment.Name));
                foreach (var mutation in Mutations(field))
                {
                    var request = Copy(baseline);
                    var parent = Parent(request, path);
                    if (parent is null)
                    {
                        continue;
                    }

                    mutation.Apply(parent, field);
                    mutations++;
                    var oneofUnset = field.RealContainingOneof is { } oneof &&
                        oneof.Accessor.GetCaseFieldDescriptor(parent) is null;
                    Check(Build(operation, request), new(text, field, mutation, oneofUnset), $"{text} {mutation.Name}");
                }
            }
        }

        output.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{ImplementedOperations().Length} operations, {mutations} mutated requests, {reported.Count} distinct operation paths."));
        foreach (var (constraint, count) in constraints.OrderBy(pair => pair.Key))
        {
            output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  {constraint}: {count}"));
        }

        foreach (var line in unbuilt.Concat(unexpected).Concat(problems))
        {
            output.WriteLine(line);
        }

        Assert.True(unbuilt.Count == 0, "No accepted request could be built for:\n" + string.Join('\n', unbuilt.Take(50)));
        Assert.True(unexpected.Count == 0,
            "A request mapping threw something other than a typed violation:\n" + string.Join('\n', unexpected.Take(50)));
        Assert.True(problems.Count == 0, string.Join('\n', problems.Take(80)));
        // Every hand-written named choice is one its mapping really enforces.
        foreach (var (operationId, fieldPath) in TextChoices.Keys)
        {
            Assert.Contains(
                $"{operationId} {fieldPath} {Api.RequestFieldConstraint.SupportedChoice}", reported);
        }

        // Every constraint is one that some operation enforces.
        Assert.Equal(
            Enum.GetValues<Api.RequestFieldConstraint>().Where(value => value != Api.RequestFieldConstraint.Unspecified),
            constraints.Keys.Order());
    }

    internal static IEnumerable<string> Problems(
        MessageDescriptor request, RequestFieldViolation violation, FieldChange? change,
        IReadOnlySet<string>? choicePaths = null)
    {
        var path = violation.FieldPath;
        var field = Resolve(request, path, out var unresolved);
        if (field is null)
        {
            yield return $"'{path}' does not resolve: {unresolved}";
            yield break;
        }

        if (Misfit(field, violation.Constraint) is { } misfit)
        {
            yield return $"'{path}' {violation.Constraint}: {misfit}";
        }

        if (violation.Constraint == Api.RequestFieldConstraint.SupportedChoice && field.FieldType == FieldType.String &&
            choicePaths?.Contains(path) != true)
        {
            yield return $"'{path}' is not a named choice of this operation";
        }

        // The server's own check must publish every violation a mapping really reports.
        if (violation.ToDetails(request) is not { } details || details.FieldPath != path ||
            details.Constraint != violation.Constraint)
        {
            yield return $"'{path}' has no public detail";
        }
        else if (Leaks(details.ToByteArray()) || Leaks(Encoding.UTF8.GetBytes(violation.Message)))
        {
            yield return $"'{path}' carries a request value";
        }

        if (change is null)
        {
            yield break;
        }

        if (path == change.Path)
        {
            if (!change.Mutation.Causes.Contains(violation.Constraint))
            {
                yield return $"'{path}' {change.Mutation.Name} cannot cause {violation.Constraint}";
            }
        }
        else if (!(violation.Constraint == Api.RequestFieldConstraint.OneofRequired &&
                   change.Field.RealContainingOneof is not null && change.OneofUnset &&
                   change.Path == $"{path}.{change.Field.Name}"))
        {
            yield return $"reported '{path}' for a change to '{change.Path}'";
        }
    }

    // The change the sweep would make to one field, for the self-test.
    private static FieldChange Change(MessageDescriptor request, string path, string mutation, bool oneofUnset = true)
    {
        var field = Resolve(request, path, out _)!;
        return new(path, field, Mutations(field).Single(candidate => candidate.Name == mutation),
            field.RealContainingOneof is not null && oneofUnset);
    }

    internal static bool Leaks(byte[] bytes)
    {
        var text = Encoding.UTF8.GetString(bytes);
        return text.Contains(TextSentinel, StringComparison.Ordinal) ||
            text.Contains(NumberSentinel.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    // A path names a field of the request: each segment is a field of the message the
    // previous segment's field holds, through repeated message fields without an index.
    internal static FieldDescriptor? Resolve(MessageDescriptor request, string path, out string? problem)
    {
        problem = null;
        MessageDescriptor? message = request;
        FieldDescriptor? field = null;
        foreach (var segment in path.Split('.'))
        {
            if (message is null)
            {
                problem = $"'{segment}' follows a field that does not hold a message";
                return null;
            }

            field = message.Fields.InDeclarationOrder().FirstOrDefault(candidate =>
                string.Equals(candidate.Name, segment, StringComparison.Ordinal));
            if (field is null)
            {
                problem = $"'{segment}' is not a field of {message.FullName}";
                return null;
            }

            message = field.FieldType == FieldType.Message ? field.MessageType : null;
        }

        return field;
    }

    internal static string? Misfit(FieldDescriptor field, Api.RequestFieldConstraint constraint) => constraint switch
    {
        Api.RequestFieldConstraint.Required when field.IsRepeated || !field.HasPresence =>
            "the field has no presence",
        Api.RequestFieldConstraint.NotBlank when field.IsRepeated || field.FieldType != FieldType.String =>
            "the field is not a string",
        Api.RequestFieldConstraint.NotEmpty or Api.RequestFieldConstraint.ElementCount when !field.IsRepeated =>
            "the field is not repeated",
        Api.RequestFieldConstraint.Range when field.IsRepeated || field.FieldType is not (FieldType.Int32 or
            FieldType.UInt32 or FieldType.Int64 or FieldType.UInt64 or FieldType.SInt32 or FieldType.SInt64 or
            FieldType.Fixed32 or FieldType.Fixed64 or FieldType.SFixed32 or FieldType.SFixed64 or FieldType.Double or
            FieldType.Float) => "the field is not a number",
        Api.RequestFieldConstraint.SupportedChoice when field.IsRepeated ||
            field.FieldType is not (FieldType.Enum or FieldType.String) => "the field is not an enum or a named choice",
        Api.RequestFieldConstraint.OneofRequired when field.IsRepeated || field.FieldType != FieldType.Message ||
            field.MessageType.Oneofs.All(oneof => oneof.IsSynthetic) => "the field does not hold a oneof",
        Api.RequestFieldConstraint.Unspecified => "the constraint is unspecified",
        _ when !Enum.IsDefined(constraint) => "the constraint is not defined",
        _ => null
    };

    internal static ImplementedOperation[] ImplementedOperations()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var byId = new Dictionary<string, ImplementedOperation>(StringComparer.Ordinal);
        foreach (var type in typeof(SpatialAnalyzerApi).Assembly.GetTypes())
        {
            var descriptor = type.GetProperty("Descriptor", flags);
            var createCommand = type.GetMethods(flags)
                .Where(method => method.Name == "CreateCommand" && method.ReturnType == typeof(WorkerMpCommand))
                .ToArray();
            if (descriptor?.PropertyType != typeof(OperationDescriptor) || createCommand.Length != 1)
            {
                continue;
            }

            var requestType = Assert.Single(createCommand[0].GetParameters()).ParameterType;
            var request = (MessageDescriptor)requestType.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)!;
            var operation = (OperationDescriptor)descriptor.GetValue(null)!;
            byId.Add(operation.OperationId, new(operation, createCommand[0], request));
        }

        return SpatialAnalyzerApi.Operations
            .Select(operation => byId.TryGetValue(operation.OperationId, out var implemented)
                ? implemented
                : throw new InvalidOperationException($"{operation.OperationId} has no request mapping."))
            .ToArray();
    }

    internal static Outcome Build(ImplementedOperation operation, IMessage? request)
    {
        try
        {
            operation.CreateCommand.Invoke(null, [request]);
            return new(null);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            return new(exception.InnerException);
        }
    }

    // A request the operation accepts: every field set, sentinel text and numbers where
    // the mapping takes them, and each reported violation repaired until none remains.
    internal static IMessage? Baseline(ImplementedOperation operation, out string? failure)
    {
        var request = Filled(operation.Request, 0);
        var attempts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var attempt = 0; attempt < RepairAttempts; attempt++)
        {
            var outcome = Build(operation, request);
            if (outcome.Exception is null)
            {
                failure = null;
                return request;
            }

            if (outcome.Exception is not RequestFieldViolation violation)
            {
                failure = $"{outcome.Exception.GetType().Name}: {outcome.Exception.Message}";
                return null;
            }

            var count = attempts[violation.FieldPath] = attempts.GetValueOrDefault(violation.FieldPath) + 1;
            if (!Repair(request, operation, violation, count))
            {
                failure = $"'{violation.FieldPath}' {violation.Constraint} could not be repaired";
                return null;
            }
        }

        failure = "too many repairs";
        return null;
    }

    private static bool Repair(IMessage request, ImplementedOperation operation, RequestFieldViolation violation, int attempt)
    {
        var descriptor = operation.Request;
        if (Resolve(descriptor, violation.FieldPath, out _) is null)
        {
            return false;
        }

        var textChoice = TextChoices.GetValueOrDefault((operation.Descriptor.OperationId, violation.FieldPath));
        var segments = violation.FieldPath.Split('.');
        var repaired = false;
        foreach (var parent in Parents(request, segments))
        {
            var field = parent.Descriptor.FindFieldByName(segments[^1]);
            repaired |= RepairField(parent, field, violation.Constraint, attempt, textChoice);
        }

        return repaired;
    }

    // Every message on the path's parent level, through all elements of repeated fields.
    private static IEnumerable<IMessage> Parents(IMessage root, string[] segments)
    {
        IEnumerable<IMessage> level = [root];
        foreach (var segment in segments[..^1])
        {
            level = level.SelectMany(message =>
            {
                var field = message.Descriptor.FindFieldByName(segment);
                var value = field.Accessor.GetValue(message);
                if (field.IsRepeated)
                {
                    return ((IList)value).Cast<IMessage>();
                }

                if (value is null)
                {
                    var created = Filled(field.MessageType, 1);
                    field.Accessor.SetValue(message, created);
                    return [created];
                }

                return [(IMessage)value];
            }).ToArray();
        }

        return level;
    }

    private static bool RepairField(
        IMessage parent, FieldDescriptor field, Api.RequestFieldConstraint constraint, int attempt, string? textChoice)
    {
        switch (constraint)
        {
            case Api.RequestFieldConstraint.Required when field.FieldType == FieldType.Message:
                field.Accessor.SetValue(parent, Filled(field.MessageType, 1));
                return attempt == 1;
            case Api.RequestFieldConstraint.Required or Api.RequestFieldConstraint.NotBlank when !field.IsRepeated:
                if (field.FieldType == FieldType.String)
                {
                    field.Accessor.SetValue(parent, TextSentinel);
                    return attempt == 1;
                }

                return SetChoiceOrNumber(parent, field, attempt);
            case Api.RequestFieldConstraint.NotEmpty or Api.RequestFieldConstraint.ElementCount when field.IsRepeated:
                int[] counts = [1, 16, 6, 3, 4, 2];
                if (attempt > counts.Length)
                {
                    return false;
                }

                var list = (IList)field.Accessor.GetValue(parent);
                list.Clear();
                for (var index = 0; index < counts[attempt - 1]; index++)
                {
                    list.Add(ElementValue(field));
                }

                return true;
            case Api.RequestFieldConstraint.SupportedChoice when field.FieldType == FieldType.String:
                if (attempt != 1 || textChoice is null)
                {
                    return false;
                }

                field.Accessor.SetValue(parent, textChoice);
                return true;
            case Api.RequestFieldConstraint.Range or Api.RequestFieldConstraint.SupportedChoice when !field.IsRepeated:
                return SetChoiceOrNumber(parent, field, attempt);
            case Api.RequestFieldConstraint.OneofRequired when field.FieldType == FieldType.Message:
                var holder = (IMessage?)field.Accessor.GetValue(parent) ?? Filled(field.MessageType, 1);
                field.Accessor.SetValue(parent, holder);
                var member = holder.Descriptor.Oneofs.First(oneof => !oneof.IsSynthetic).Fields[0];
                member.Accessor.SetValue(holder, ScalarOrMessage(member, 1));
                return attempt == 1;
            default:
                return false;
        }
    }

    // Successive attempts walk the enum's defined values, or a few small numbers.
    private static bool SetChoiceOrNumber(IMessage parent, FieldDescriptor field, int attempt)
    {
        if (field.FieldType == FieldType.Enum)
        {
            var choices = field.EnumType.Values.Where(value => value.Number != 0).ToArray();
            if (attempt > choices.Length)
            {
                return false;
            }

            field.Accessor.SetValue(parent, Enum.ToObject(field.EnumType.ClrType, choices[attempt - 1].Number));
            return true;
        }

        object?[] numbers = field.FieldType switch
        {
            FieldType.Double => [1d, 0d, 2d],
            FieldType.Float => [1f, 0f, 2f],
            FieldType.UInt32 or FieldType.Fixed32 => [1u, 0u, 2u],
            FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64 => [1L, 0L, 2L],
            FieldType.UInt64 or FieldType.Fixed64 => [1UL, 0UL, 2UL],
            FieldType.Bool => [true, false],
            _ => [1, 0, 2]
        };
        if (attempt > numbers.Length)
        {
            return false;
        }

        field.Accessor.SetValue(parent, numbers[attempt - 1]);
        return true;
    }

    internal static IMessage Filled(MessageDescriptor descriptor, int depth)
    {
        var message = (IMessage)Activator.CreateInstance(descriptor.ClrType)!;
        foreach (var field in descriptor.Fields.InDeclarationOrder())
        {
            if (field.IsMap || (field.RealContainingOneof is { } oneof && oneof.Fields[0] != field))
            {
                continue;
            }

            if (field.FieldType == FieldType.Message && depth >= MaximumDepth)
            {
                continue;
            }

            if (field.IsRepeated)
            {
                var list = (IList)field.Accessor.GetValue(message);
                var count = field.FieldType == FieldType.Double ? 16 : 1;
                for (var index = 0; index < count; index++)
                {
                    list.Add(ScalarOrMessage(field, depth + 1));
                }

                continue;
            }

            field.Accessor.SetValue(message, ScalarOrMessage(field, depth + 1));
        }

        return message;
    }

    private static object ElementValue(FieldDescriptor field) => ScalarOrMessage(field, 1);

    private static object ScalarOrMessage(FieldDescriptor field, int depth) => field.FieldType switch
    {
        FieldType.Message => Filled(field.MessageType, depth),
        FieldType.String => TextSentinel,
        FieldType.Bytes => ByteString.CopyFromUtf8(TextSentinel),
        FieldType.Bool => true,
        FieldType.Double => NumberSentinel + 0.25,
        FieldType.Float => NumberSentinel + 0.25f,
        FieldType.UInt32 or FieldType.Fixed32 => (uint)NumberSentinel,
        FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64 => (long)NumberSentinel,
        FieldType.UInt64 or FieldType.Fixed64 => (ulong)NumberSentinel,
        FieldType.Enum => Enum.ToObject(field.EnumType.ClrType,
            field.EnumType.Values.FirstOrDefault(value => value.Number != 0)?.Number ?? 0),
        _ => NumberSentinel
    };

    internal static IEnumerable<ImmutableList<FieldDescriptor>> Paths(IMessage message, ImmutableList<FieldDescriptor> prefix, int depth)
    {
        foreach (var field in message.Descriptor.Fields.InDeclarationOrder())
        {
            if (field.IsMap)
            {
                continue;
            }

            var path = prefix.Add(field);
            yield return path;
            if (field.FieldType != FieldType.Message || depth >= MaximumDepth)
            {
                continue;
            }

            var value = field.Accessor.GetValue(message);
            var child = field.IsRepeated ? ((IList)value).Cast<IMessage>().FirstOrDefault() : value as IMessage;
            if (child is not null)
            {
                foreach (var nested in Paths(child, path, depth + 1))
                {
                    yield return nested;
                }
            }
        }
    }

    // The message holding the path's last field, through the first element of repeated fields.
    internal static IMessage? Parent(IMessage root, ImmutableList<FieldDescriptor> path)
    {
        var current = root;
        foreach (var field in path.Take(path.Count - 1))
        {
            var value = field.Accessor.GetValue(current);
            if (field.IsRepeated)
            {
                var list = (IList)value;
                value = list.Count == 0 ? null : list[0];
            }

            if (value is not IMessage next)
            {
                return null;
            }

            current = next;
        }

        return current;
    }

    // Each mutation lists the constraints it can cause on the field it changes: an absent
    // field can be required, or read as a blank string or an unspecified choice; a blank
    // string can be blank or not a named choice; a number can be out of range; an enum
    // value can be unsupported; a list can be empty or have the wrong length.
    internal static IEnumerable<Mutation> Mutations(FieldDescriptor field)
    {
        if (field.IsRepeated)
        {
            yield return new("cleared", (parent, target) => ((IList)target.Accessor.GetValue(parent)).Clear(),
                [Api.RequestFieldConstraint.NotEmpty, Api.RequestFieldConstraint.ElementCount]);
            if (field.FieldType != FieldType.Message)
            {
                foreach (var count in new[] { 1, 15, 17 })
                {
                    yield return new($"with {count} elements", (parent, target) =>
                    {
                        var list = (IList)target.Accessor.GetValue(parent);
                        list.Clear();
                        for (var index = 0; index < count; index++)
                        {
                            list.Add(ElementValue(target));
                        }
                    }, [Api.RequestFieldConstraint.ElementCount]);
                }
            }

            yield break;
        }

        if (field.HasPresence)
        {
            Api.RequestFieldConstraint[] causes = field.FieldType switch
            {
                FieldType.String => [Api.RequestFieldConstraint.Required, Api.RequestFieldConstraint.NotBlank,
                    Api.RequestFieldConstraint.SupportedChoice],
                FieldType.Enum => [Api.RequestFieldConstraint.Required, Api.RequestFieldConstraint.SupportedChoice],
                _ => [Api.RequestFieldConstraint.Required]
            };
            yield return new("absent", (parent, target) => target.Accessor.Clear(parent), causes);
        }

        if (field.FieldType == FieldType.Message)
        {
            yield break;
        }

        IEnumerable<(string Name, object Value)> values = field.FieldType switch
        {
            FieldType.String => [("empty", string.Empty), ("whitespace", "   ")],
            FieldType.Bool => [("false", false), ("true", true)],
            FieldType.Enum => field.EnumType.Values
                .Select(value => ($"= {value.Name}", Enum.ToObject(field.EnumType.ClrType, value.Number)))
                .Append(("= 9999", Enum.ToObject(field.EnumType.ClrType, 9999))),
            FieldType.UInt32 or FieldType.Fixed32 => [("= 256", 256u), ("= max", uint.MaxValue)],
            FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64 => [("= -1", -1L)],
            FieldType.UInt64 or FieldType.Fixed64 => [("= 256", 256UL)],
            FieldType.Double => [("= -1", -1d), ("= 0", 0d)],
            FieldType.Float => [("= -1", -1f)],
            _ => [("= -1", -1), ("= 256", 256), ("= max", int.MaxValue)]
        };
        Api.RequestFieldConstraint[] valueCauses = field.FieldType switch
        {
            FieldType.String => [Api.RequestFieldConstraint.NotBlank, Api.RequestFieldConstraint.SupportedChoice],
            FieldType.Bool => [],
            FieldType.Enum => [Api.RequestFieldConstraint.SupportedChoice],
            _ => [Api.RequestFieldConstraint.Range]
        };
        foreach (var (name, value) in values)
        {
            yield return new(name, (parent, target) => target.Accessor.SetValue(parent, value), valueCauses);
        }
    }

    internal static IMessage Copy(IMessage source)
    {
        var copy = (IMessage)Activator.CreateInstance(source.GetType())!;
        copy.MergeFrom(source.ToByteString());
        return copy;
    }

    internal sealed record ImplementedOperation(OperationDescriptor Descriptor, MethodInfo CreateCommand, MessageDescriptor Request);

    internal sealed record Outcome(Exception? Exception);

    internal sealed record Mutation(
        string Name, Action<IMessage, FieldDescriptor> Apply, IReadOnlyList<Api.RequestFieldConstraint> Causes);

    // OneofUnset: the change left the oneof that holds Field with no field set.
    internal sealed record FieldChange(string Path, FieldDescriptor Field, Mutation Mutation, bool OneofUnset);
}
