using System.Collections;
using System.Globalization;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Api = global::Briosa;

namespace Briosa.Server.Operations.WaveA;

internal static class MpOperationValueMapper
{
    public static int GetRequiredPositiveInt32(IMessage request, string fieldName)
    {
        var field = request.Descriptor.FindFieldByName(fieldName) ??
            throw new InvalidOperationException(
                $"Request field '{fieldName}' does not exist.");
        var (provided, value) = ReadValue(request, field);
        if (!provided || value is not int result || result <= 0)
        {
            throw new ArgumentException(
                $"Request field '{fieldName}' must be greater than zero.",
                nameof(request));
        }

        return result;
    }

    public static WorkerMpInputArgument? ToInput(
        IMessage request,
        MpArgumentContract contract)
    {
        var outerField = request.Descriptor.FindFieldByName(contract.FieldName) ??
            throw new InvalidOperationException(
                $"Request field '{contract.FieldName}' does not exist.");
        var field = outerField;
        var source = request;
        var (provided, value) = ReadValue(request, outerField);
        if (contract.NestedFieldName is not null)
        {
            source = provided && value is IMessage outerNested
                ? outerNested
                : (IMessage?)Activator.CreateInstance(outerField.MessageType.ClrType) ??
                    throw new InvalidOperationException(
                        $"Cannot create request field '{contract.FieldName}'.");
            var segments = contract.NestedFieldName.Split('.');
            for (var index = 0; index < segments.Length; index++)
            {
                var segment = segments[index];
                field = source.Descriptor.FindFieldByName(segment) ??
                    throw new InvalidOperationException(
                        $"Request field '{contract.FieldName}.{contract.NestedFieldName}' does not exist.");
                if (provided)
                {
                    (provided, value) = ReadValue(source, field);
                }
                else
                {
                    value = null;
                }

                if (index < segments.Length - 1)
                {
                    if (provided && value is IMessage nested)
                    {
                        source = nested;
                    }
                    else
                    {
                        source = (IMessage?)Activator.CreateInstance(field.MessageType.ClrType) ??
                            throw new InvalidOperationException(
                                $"Cannot create request field '{contract.FieldName}.{segment}'.");
                        provided = false;
                        value = null;
                    }
                }
            }
        }
        if (!provided)
        {
            if (contract.OmitWhenAbsent)
            {
                return null;
            }

            if (contract.Required)
            {
                throw new ArgumentException(
                    $"Request field '{contract.FieldName}' is required.",
                    nameof(request));
            }

            value = CreateDefault(field, contract);
        }

        ValidateRequiredValue(value, field, contract, source);
        if (field.FieldType == FieldType.Enum &&
            field.EnumType.FindValueByNumber(Convert.ToInt32(value, CultureInfo.InvariantCulture)) is null)
        {
            throw new ArgumentException($"Request field '{contract.FieldName}' has an unsupported exact-target enum value.", nameof(request));
        }

        if (contract.Kind == WorkerMpValueKind.InstrumentTypeName &&
            (value is not Api.InstrumentTypeName instrumentType ||
             !WorkerInstrumentTypeNames.IsSupported(instrumentType.Value)))
        {
            throw new ArgumentException("Instrument type must be an exact SA 2024.1.0508.5 choice.", nameof(request));
        }

        return CreateInput(contract, NormalizeInputValue(value, field, contract));
    }

    private static object? NormalizeInputValue(
        object? value,
        FieldDescriptor field,
        MpArgumentContract contract)
    {
        if (IsListKind(contract.Kind) &&
            value is IMessage wrapper &&
            wrapper.Descriptor.FindFieldByName("values") is
                { IsRepeated: true } valuesField)
        {
            return ((IEnumerable)valuesField.Accessor.GetValue(wrapper))
                .Cast<object>()
                .ToArray();
        }

        if (field.FieldType != FieldType.Enum || contract.EnumTextValues is null)
        {
            return value;
        }

        var number = Convert.ToInt32(value, CultureInfo.InvariantCulture);
        if (number <= 0 ||
            number > contract.EnumTextValues.Count ||
            string.IsNullOrWhiteSpace(contract.EnumTextValues[number - 1]))
        {
            throw new ArgumentException(
                $"Request field '{contract.FieldName}' must specify a supported value.");
        }

        return contract.EnumTextValues[number - 1];
    }

    private static bool IsListKind(WorkerMpValueKind kind) =>
        kind is WorkerMpValueKind.DoubleArray or
            WorkerMpValueKind.CollectionInstrumentIdList or
            WorkerMpValueKind.CollectionItemNameList or
            WorkerMpValueKind.CollectionObjectNameList or
            WorkerMpValueKind.CollectionGroupNameList or
            WorkerMpValueKind.CollectionVectorGroupNameList or
            WorkerMpValueKind.PointNameList or
            WorkerMpValueKind.StringList or
            WorkerMpValueKind.VectorNameList;

    public static TResponse ToResult<TResponse>(
        SuccessfulOperationExecution completed,
        IReadOnlyList<MpArgumentContract> outputs)
        where TResponse : class, IMessage<TResponse>, new()
    {
        ArgumentNullException.ThrowIfNull(completed);
        var result = new TResponse();
        if (completed.Execution.OutputValues.Count != outputs.Count)
        {
            throw new InvalidOperationException(
                "Worker output count does not match the operation contract.");
        }

        for (var outputIndex = 0; outputIndex < outputs.Count; outputIndex++)
        {
            var contract = outputs[outputIndex];
            var outerField = result.Descriptor.FindFieldByName(contract.FieldName) ??
                throw new InvalidOperationException(
                    $"Result field '{contract.FieldName}' does not exist.");
            var target = (IMessage)result;
            var field = outerField;
            if (contract.NestedFieldName is not null)
            {
                var outerNested = outerField.Accessor.GetValue(result) as IMessage;
                if (outerNested is null)
                {
                    outerNested = (IMessage?)Activator.CreateInstance(outerField.MessageType.ClrType) ??
                        throw new InvalidOperationException(
                            $"Cannot create result field '{contract.FieldName}'.");
                    outerField.Accessor.SetValue(result, outerNested);
                }

                target = outerNested;
                var segments = contract.NestedFieldName.Split('.');
                for (var index = 0; index < segments.Length; index++)
                {
                    var segment = segments[index];
                    field = target.Descriptor.FindFieldByName(segment) ??
                        throw new InvalidOperationException(
                            $"Result field '{contract.FieldName}.{contract.NestedFieldName}' does not exist.");
                    if (index == segments.Length - 1)
                    {
                        break;
                    }

                    var nested = field.Accessor.GetValue(target) as IMessage;
                    if (nested is null)
                    {
                        nested = (IMessage?)Activator.CreateInstance(field.MessageType.ClrType) ??
                            throw new InvalidOperationException(
                                $"Cannot create result field '{contract.FieldName}.{segment}'.");
                        field.Accessor.SetValue(target, nested);
                    }

                    target = nested;
                }
            }
            var output = completed.Execution.OutputValues[outputIndex];
            if (output.Name != contract.MpName || output.Kind != contract.Kind)
            {
                throw new InvalidOperationException(
                    $"Worker output at index {outputIndex} does not match " +
                    $"'{contract.MpName}' ({contract.Kind}).");
            }

            SetResultField(target, field, ToProtocolValue(output, field, contract));
        }

        var execution = result.Descriptor.FindFieldByName("execution") ??
            throw new InvalidOperationException("Result message has no execution field.");
        execution.Accessor.SetValue(result, completed.Details);
        return result;
    }

    private static (bool Provided, object? Value) ReadValue(
        IMessage request,
        FieldDescriptor field)
    {
        var value = field.Accessor.GetValue(request);
        if (field.IsRepeated)
        {
            var values = ((IEnumerable)value).Cast<object>().ToArray();
            return (values.Length > 0, values);
        }

        return field.HasPresence
            ? (field.Accessor.HasValue(request), value)
            : (true, value);
    }

    private static object? CreateDefault(
        FieldDescriptor field,
        MpArgumentContract contract)
    {
        if (field.IsRepeated)
        {
            return Array.Empty<object>();
        }

        var value = contract.DefaultValue;
        return field.FieldType switch
        {
            FieldType.Bool => bool.Parse(value),
            FieldType.Int32 => int.Parse(value, CultureInfo.InvariantCulture),
            FieldType.Double => double.Parse(value, CultureInfo.InvariantCulture),
            FieldType.String => string.Equals(value, "Empty", StringComparison.Ordinal)
                ? string.Empty
                : value,
            FieldType.Enum => ResolveEnumDefault(field.EnumType, value),
            FieldType.Message when contract.Kind is
                WorkerMpValueKind.Text or WorkerMpValueKind.InstrumentTypeName =>
                CreateStringWrapperDefault(field.MessageType, value),
            FieldType.Message => CreateMessageDefault(contract.Kind, value),
            _ => throw new InvalidOperationException(
                $"Unsupported default for field '{field.Name}'.")
        };
    }

    private static int ResolveEnumDefault(EnumDescriptor descriptor, string value)
    {
        var normalized = Normalize(value);
        if (descriptor.FullName == "briosa.RelWeightingMode" &&
            normalized == "NORMALIZEONEQUATIONCOUNT")
        {
            return descriptor.Values[1].Number;
        }

        var match = descriptor.Values.FirstOrDefault(candidate =>
            Normalize(candidate.Name).EndsWith(normalized, StringComparison.Ordinal));
        return match?.Number ??
            throw new InvalidOperationException(
                $"Default '{value}' is not valid for {descriptor.FullName}.");
    }

    private static object CreateMessageDefault(
        WorkerMpValueKind kind,
        string value) =>
        kind switch
        {
            WorkerMpValueKind.RgbColor => DefaultColor(value),
            WorkerMpValueKind.Font => new Api.Font
            {
                FontName = "MS Shell Dlg",
                Size = 8,
                Color = new Api.Color()
            },
            WorkerMpValueKind.ColorizationOptions => DefaultColorization(),
            WorkerMpValueKind.CloudThinningOptions => new Api.CloudThinningOptions
            {
                Mode = Api.CloudThinningMode.None,
                PointIncrement = 1,
                MinimumNumberOfPoints = 0,
                MaximumNumberOfPoints = 0
            },
            WorkerMpValueKind.BSplineFitOptions => DefaultBSplineFitOptions(),
            WorkerMpValueKind.FitConstraintScalarOptions =>
                new Api.FitConstraintScalarOptions(),
            WorkerMpValueKind.ToleranceScalarOptions =>
                new Api.ToleranceScalarOptions(),
            WorkerMpValueKind.ProjectionOptions => new Api.ProjectionOptions
            {
                ProjectionType = "Object To Probe Vectors"
            },
            WorkerMpValueKind.PointDeltaReportOptions =>
                DefaultPointDeltaReportOptions(),
            WorkerMpValueKind.UdpTransmitSettings => new Api.RelationshipWatchWindowUdpSettings
            {
                Enabled = false,
                Broadcast = true,
                IpAddress = string.Empty,
                Port = 10000
            },
            WorkerMpValueKind.ReportOutputOptions =>
                DefaultReportOutput(value),
            _ => throw new InvalidOperationException(
                $"No message default exists for {kind}.")
        };

    private static IMessage CreateStringWrapperDefault(
        MessageDescriptor descriptor,
        string value)
    {
        var message = descriptor.Parser.ParseFrom(Array.Empty<byte>());
        var field = descriptor.FindFieldByName("value");
        if (field is null || field.FieldType != FieldType.String)
        {
            throw new InvalidOperationException(
                $"Message '{descriptor.FullName}' is not a string wrapper.");
        }

        field.Accessor.SetValue(
            message,
            string.Equals(value, "Empty", StringComparison.Ordinal) ? string.Empty : value);
        return message;
    }

    private static Api.Color DefaultColor(string value)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3 && parts.All(part => uint.TryParse(part, out _)))
        {
            return new Api.Color
            {
                Red = uint.Parse(parts[0], CultureInfo.InvariantCulture),
                Green = uint.Parse(parts[1], CultureInfo.InvariantCulture),
                Blue = uint.Parse(parts[2], CultureInfo.InvariantCulture)
            };
        }

        return new Api.Color { Red = 255 };
    }

    private static Api.BSplineFitOptions DefaultBSplineFitOptions() => new()
    {
        OpenCurve = true,
        UseInterpolationForFit = true,
        NumberOfControlPoints = 8,
        DegreeOfCurve = 3,
        SortMethod = Api.BSplinePointSortMode.UseSelectionOrder,
        SpanAnyGap = true,
        TerminationGapLength = 0,
        IgnoreProximatePoints = false,
        ProximatePointThreshold = 0,
        UseGlobalTessellationOptions = true,
        MaximumChordalDeviation = 0.05,
        MaximumTrimEdgeAngle = 15,
        TerminationAverageMultiplier = 10,
        Extension = 0
    };

    private static Api.ColorizationOptions DefaultColorization() => new()
    {
        ColorRangeMethod = Api.ColorRangeMethod.Continuous,
        BaseHighColor = Api.BaseColorType.Blue,
        BaseMidColor = Api.BaseMidColorType.Green,
        BaseLowColor = Api.BaseColorType.Red,
        DrawTubes = false,
        DrawArrowheads = true,
        IndicateValues = false,
        VectorMagnification = 100,
        VectorWidth = 1,
        DrawBlotches = false,
        BlotchSize = 0.1,
        ShowOutOfToleranceOnly = false,
        ShowColorBarInView = false,
        ShowColorBarPercentages = true,
        ShowColorBarFractions = false,
        HighSaturationLimit = 0.5,
        LowSaturationLimit = -0.5,
        HighTolerance = 0.03,
        LowTolerance = -0.03
    };

    private static Api.PointDeltaReportOptions DefaultPointDeltaReportOptions() => new()
    {
        CoordinateSystem = Api.CoordinateSystemType.Cartesian,
        DetailsFormat = "Single",
        ShowPointA = true,
        ShowPointB = true,
        ShowDelta = true,
        ShowMagnitude = true,
        ShowComponent1 = true,
        ShowComponent2 = true,
        ShowComponent3 = true,
        SortPointNames = false,
        ShowToleranceFields = true,
        ColorizeInToleranceFields = true
    };

    private static Api.ReportOutputOptions DefaultReportOutput(string value)
    {
        var separator = value.IndexOf("::", StringComparison.Ordinal);
        return new Api.ReportOutputOptions
        {
            OutputType = Api.ReportOutputType.SaReport,
            EmbeddedFile = new Api.EmbeddedReportFile
            {
                CollectionName = separator <= 0 ? string.Empty : value[..separator],
                FileName = separator < 0 ? value : value[(separator + 2)..]
            }
        };
    }

    private static void ValidateRequiredValue(
        object? value,
        FieldDescriptor field,
        MpArgumentContract contract,
        IMessage request)
    {
        if (!contract.Required)
        {
            return;
        }

        var invalid = value switch
        {
            null => true,
            string text => string.IsNullOrWhiteSpace(text),
            object[] values => values.Length == 0,
            Api.FileReference file => string.IsNullOrWhiteSpace(file.Path),
            Api.PointName point => string.IsNullOrWhiteSpace(point.TargetName),
            Api.CollectionInstrumentId instrument =>
                string.IsNullOrWhiteSpace(instrument.CollectionName),
            Api.CollectionMachineId machine =>
                string.IsNullOrWhiteSpace(machine.CollectionName),
            Api.CollectionName collection => string.IsNullOrWhiteSpace(collection.Name),
            Api.CollectionItemName item => string.IsNullOrWhiteSpace(item.ItemName),
            Api.CollectionObjectName item => string.IsNullOrWhiteSpace(item.ObjectName),
            Api.CollectionGroupName group => string.IsNullOrWhiteSpace(group.GroupName),
            Api.CollectionVectorGroupName group =>
                string.IsNullOrWhiteSpace(group.VectorGroupName),
            Api.VectorName vector => string.IsNullOrWhiteSpace(vector.Name),
            Api.ChartName chart => string.IsNullOrWhiteSpace(chart.Name),
            Api.FrameName frame => string.IsNullOrWhiteSpace(frame.Name),
            Api.ViewName view => string.IsNullOrWhiteSpace(view.Name),
            int enumValue when field.FieldType == FieldType.Enum => enumValue == 0,
            _ => false
        };
        if (invalid)
        {
            throw new ArgumentException(
                $"Request field '{contract.FieldName}' is required.",
                nameof(request));
        }
    }

    private static WorkerMpInputArgument CreateInput(
        MpArgumentContract contract,
        object? value)
    {
        var kind = contract.Kind;
        var values = value as object[];
        return new WorkerMpInputArgument(
            contract.MpName,
            kind,
            BooleanValue: kind == WorkerMpValueKind.Logical ? Convert.ToBoolean(value, CultureInfo.InvariantCulture) : null,
            IntegerValue: kind == WorkerMpValueKind.WholeNumber ? Convert.ToInt32(value, CultureInfo.InvariantCulture) : null,
            DoubleValue: kind == WorkerMpValueKind.FloatingPoint ? Convert.ToDouble(value, CultureInfo.InvariantCulture) : null,
            StringValue: ToStringValue(kind, value),
            PointNameValue: value is Api.PointName point ? Point(point) : null,
            VectorValue: value is Api.Vector vector ? new(vector.X, vector.Y, vector.Z) : null,
            ToleranceVectorOptionsValue: value is Api.ToleranceVectorOptions toleranceVector
                ? ToleranceVector(toleranceVector)
                : null,
            CollectionInstrumentIdValue: value is Api.CollectionInstrumentId instrument
                ? new(instrument.CollectionName, instrument.InstrumentId)
                : null,
            CollectionInstrumentIdListValue: kind == WorkerMpValueKind.CollectionInstrumentIdList
                ? new(values!.Cast<Api.CollectionInstrumentId>()
                    .Select(item => new WorkerCollectionInstrumentIdValue(
                        item.CollectionName,
                        item.InstrumentId)).ToArray())
                : null,
            CollectionMachineIdValue: value is Api.CollectionMachineId machine
                ? new(machine.CollectionName, machine.MachineId)
                : null,
            CollectionObjectNameValue: value switch
            {
                Api.CollectionObjectName item => ObjectName(item, contract.ObjectTypeWhenOmitted),
                Api.CollectionItemName item => ObjectName(item, contract.ObjectTypeWhenOmitted),
                _ => null
            },
            CollectionObjectNameListValue: kind == WorkerMpValueKind.CollectionObjectNameList
                ? new(values!.Select(item => item switch
                    {
                        Api.CollectionObjectName objectName => ObjectName(objectName, contract.ObjectTypeWhenOmitted),
                        Api.CollectionItemName itemName => ObjectName(itemName, contract.ObjectTypeWhenOmitted),
                        _ => throw new ArgumentException($"Field '{contract.FieldName}' contains an unsupported object identity.")
                    }).ToArray())
                : null,
            CollectionItemNameValue: value is Api.CollectionItemName collectionItem
                ? ItemName(collectionItem, contract.ItemTypeWhenOmitted)
                : null,
            CollectionItemNameListValue: kind == WorkerMpValueKind.CollectionItemNameList
                ? new(values!.Cast<Api.CollectionItemName>()
                    .Select(item => ItemName(item, contract.ItemTypeWhenOmitted)).ToArray())
                : null,
            CollectionGroupNameListValue: kind == WorkerMpValueKind.CollectionGroupNameList
                ? new(values!.Cast<Api.CollectionGroupName>()
                    .Select(item => new WorkerCollectionGroupNameValue(
                        item.CollectionName,
                        item.GroupName)).ToArray())
                : null,
            CollectionVectorGroupNameValue: value is Api.CollectionVectorGroupName vectorGroup
                ? new(vectorGroup.CollectionName, vectorGroup.VectorGroupName)
                : null,
            CollectionVectorGroupNameListValue: kind == WorkerMpValueKind.CollectionVectorGroupNameList
                ? new(values!.Cast<Api.CollectionVectorGroupName>()
                    .Select(item => new WorkerCollectionVectorGroupNameValue(
                        item.CollectionName,
                        item.VectorGroupName)).ToArray())
                : null,
            PointNameListValue: kind == WorkerMpValueKind.PointNameList
                ? new(values!.Cast<Api.PointName>().Select(Point).ToArray())
                : null,
            StringListValue: kind is WorkerMpValueKind.StringList or WorkerMpValueKind.EditText
                ? new(values!.Select(Convert.ToString).Select(item => item!).ToArray())
                : null,
            VectorNameListValue: kind == WorkerMpValueKind.VectorNameList
                ? new(values!.Cast<Api.VectorName>()
                    .Select(item => new WorkerVectorNameValue(
                        item.CollectionName,
                        item.GroupName,
                        item.Name)).ToArray())
                : null,
            DoubleArrayValue: kind == WorkerMpValueKind.DoubleArray
                ? new(values!.Select(item => Convert.ToDouble(item, CultureInfo.InvariantCulture)).ToArray())
                : null,
            TransformValue: kind == WorkerMpValueKind.Transform && value is Api.Transform transform
                ? Transform(transform)
                : null,
            WorldTransformValue: kind == WorkerMpValueKind.WorldTransform && value is Api.WorldTransform world
                ? WorldTransform(world)
                : null,
            RgbColorValue: kind == WorkerMpValueKind.RgbColor && value is Api.Color color
                ? Color(color)
                : null,
            FileReferenceValue: value is Api.FileReference file
                ? new(file.Path, file.EmbeddedFile)
                : null,
            AngularUnitValue: kind == WorkerMpValueKind.AngularUnit
                ? (WorkerAngularUnitValue)Convert.ToInt32(value, CultureInfo.InvariantCulture)
                : null,
            DistanceUnitValue: kind == WorkerMpValueKind.DistanceUnit
                ? (WorkerDistanceUnitValue)Convert.ToInt32(value, CultureInfo.InvariantCulture)
                : null,
            TemperatureUnitValue: kind == WorkerMpValueKind.TemperatureUnit
                ? (WorkerTemperatureUnitValue)Convert.ToInt32(value, CultureInfo.InvariantCulture)
                : null,
            FontValue: value is Api.Font font ? Font(font) : null,
            SpecializedEnumValue: IsSpecializedEnum(kind)
                ? new WorkerSpecializedEnumValue(
                    Convert.ToInt32(value, CultureInfo.InvariantCulture) - 1)
                : null,
            ColorizationOptionsValue: value is Api.ColorizationOptions colorization
                ? Colorization(colorization)
                : null,
            CloudThinningOptionsValue: value is Api.CloudThinningOptions cloudThinning
                ? CloudThinning(cloudThinning)
                : null,
            BSplineFitOptionsValue: value is Api.BSplineFitOptions bSplineFit
                ? BSplineFit(bSplineFit)
                : null,
            FitConstraintScalarOptionsValue: value is Api.FitConstraintScalarOptions fit
                ? new(Tolerance(fit.High), Tolerance(fit.Low))
                : null,
            ReportOutputOptionsValue: value is Api.ReportOutputOptions reportOutput
                ? ReportOutput(reportOutput)
                : null,
            ReportViewOptionsValue: value is Api.ReportViewOptions reportView
                ? new(
                    (int)reportView.ViewType - 1,
                    reportView.CollectionName,
                    reportView.CalloutName)
                : null,
            ToleranceScalarOptionsValue: value is Api.ToleranceScalarOptions scalarTolerance
                ? new(Tolerance(scalarTolerance.High), Tolerance(scalarTolerance.Low))
                : null,
            ProjectionOptionsValue: value is Api.ProjectionOptions projection
                ? Projection(projection)
                : null,
            PointDeltaReportOptionsValue: value is Api.PointDeltaReportOptions pointDelta
                ? PointDelta(pointDelta)
                : null,
            UdpTransmitSettingsValue: value is Api.RelationshipWatchWindowUdpSettings udp
                ? new WorkerUdpTransmitSettingsValue(
                    udp.HasEnabled && udp.Enabled,
                    !udp.HasBroadcast || udp.Broadcast,
                    udp.HasIpAddress ? udp.IpAddress : string.Empty,
                    udp.HasPort ? udp.Port : 10000)
                : null,
            SdkBinding: contract.SdkBinding);
    }

    private static string? ToStringValue(WorkerMpValueKind kind, object? value) =>
        kind is WorkerMpValueKind.Text or WorkerMpValueKind.InstrumentTypeName or WorkerMpValueKind.ChartName or
            WorkerMpValueKind.CollectionName or WorkerMpValueKind.FrameName or
            WorkerMpValueKind.ViewName
            ? value switch
            {
                Api.ChartName item => item.Name,
                Api.CollectionName item => item.Name,
                Api.FrameName item => item.Name,
                Api.ViewName item => item.Name,
                Api.InstrumentTypeName item => item.Value,
                Api.FileReference file => file.Path,
                IMessage wrapper when wrapper.Descriptor.FindFieldByName("value") is { FieldType: FieldType.String } wrapperField =>
                    Convert.ToString(wrapperField.Accessor.GetValue(wrapper), CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture)
            }
            : null;

    private static WorkerPointNameValue Point(Api.PointName value) =>
        new(value.CollectionName, value.GroupName, value.TargetName);

    private static WorkerCollectionObjectNameValue ObjectName(
        Api.CollectionObjectName value,
        WorkerObjectTypeValue? fallback)
    {
        if (!Enum.IsDefined(value.ObjectType))
        {
            throw new ArgumentException("Object type is not supported by this SA target.", nameof(value));
        }

        var type = (WorkerObjectTypeValue)(int)value.ObjectType;
        if (type == WorkerObjectTypeValue.Unspecified)
        {
            type = fallback ?? WorkerObjectTypeValue.Any;
        }

        return new(value.CollectionName, value.ObjectName, type);
    }

    private static WorkerCollectionItemNameValue ItemName(
        Api.CollectionItemName value,
        WorkerItemTypeValue? fallback = null)
    {
        ValidateItemType(value);
        return new(
            value.CollectionName,
            value.ItemName,
            value.HasItemType
                ? (WorkerItemTypeValue)(int)value.ItemType
                : fallback ?? WorkerItemTypeValue.Any);
    }

    private static WorkerCollectionObjectNameValue ObjectName(
        Api.CollectionItemName value,
        WorkerObjectTypeValue? fallback)
    {
        ValidateItemType(value);
        return new(
            value.CollectionName,
            value.ItemName,
            fallback ?? WorkerObjectTypeValue.Any);
    }

    private static void ValidateItemType(Api.CollectionItemName value)
    {
        if (value.HasItemType && !Enum.IsDefined(value.ItemType))
        {
            throw new ArgumentException("Item type is not supported by this SA target.", nameof(value));
        }
    }

    private static WorkerTransformValue Transform(Api.Transform value)
    {
        if (value.Values.Count != 16)
        {
            throw new ArgumentException("Transforms must contain exactly 16 values.");
        }

        return new([.. value.Values]);
    }

    private static WorkerWorldTransformValue WorldTransform(Api.WorldTransform value) =>
        new(
            Transform(value.Transform ??
                throw new ArgumentException("A world transform requires a transform.")),
            value.HasScaleFactor ? value.ScaleFactor : 1d);

    private static WorkerRgbColorValue Color(Api.Color value)
    {
        if (value.Red > byte.MaxValue || value.Green > byte.MaxValue || value.Blue > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Color channels must be in 0..255.");
        }

        return new((byte)value.Red, (byte)value.Green, (byte)value.Blue);
    }

    private static WorkerFontValue Font(Api.Font value)
    {
        if (value.Size > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Font size must be in 0..255.");
        }

        return new(
            value.HasFontName ? value.FontName : "MS Shell Dlg",
            (byte)(value.HasSize ? value.Size : 8),
            value.Color is null ? new(0, 0, 0) : Color(value.Color));
    }

    private static WorkerCloudThinningOptionsValue CloudThinning(Api.CloudThinningOptions value)
    {
        var defaults = (Api.CloudThinningOptions)CreateMessageDefault(
            WorkerMpValueKind.CloudThinningOptions,
            string.Empty);
        return new(
            (int)(value.HasMode ? value.Mode : defaults.Mode) - 1,
            value.HasPointIncrement ? value.PointIncrement : defaults.PointIncrement,
            value.HasMinimumNumberOfPoints ? value.MinimumNumberOfPoints : defaults.MinimumNumberOfPoints,
            value.HasMaximumNumberOfPoints ? value.MaximumNumberOfPoints : defaults.MaximumNumberOfPoints);
    }

    private static WorkerBSplineFitOptionsValue BSplineFit(Api.BSplineFitOptions value)
    {
        var defaults = DefaultBSplineFitOptions();
        return new(
            value.HasUseInterpolationForFit ? value.UseInterpolationForFit : defaults.UseInterpolationForFit,
            value.HasOpenCurve ? value.OpenCurve : defaults.OpenCurve,
            (int)(value.HasSortMethod ? value.SortMethod : defaults.SortMethod) - 1,
            value.HasSpanAnyGap ? (value.SpanAnyGap ? 0 : 1) : 0,
            value.HasDegreeOfCurve ? value.DegreeOfCurve : defaults.DegreeOfCurve,
            value.HasTerminationGapLength ? value.TerminationGapLength : defaults.TerminationGapLength,
            value.HasTerminationAverageMultiplier ? value.TerminationAverageMultiplier : defaults.TerminationAverageMultiplier,
            value.HasNumberOfControlPoints ? value.NumberOfControlPoints : defaults.NumberOfControlPoints,
            value.HasIgnoreProximatePoints ? value.IgnoreProximatePoints : defaults.IgnoreProximatePoints,
            value.HasProximatePointThreshold ? value.ProximatePointThreshold : defaults.ProximatePointThreshold,
            value.HasExtension ? value.Extension : defaults.Extension,
            value.HasUseGlobalTessellationOptions ? value.UseGlobalTessellationOptions : defaults.UseGlobalTessellationOptions,
            value.HasMaximumChordalDeviation ? value.MaximumChordalDeviation : defaults.MaximumChordalDeviation,
            value.HasMaximumTrimEdgeAngle ? value.MaximumTrimEdgeAngle : defaults.MaximumTrimEdgeAngle);
    }

    private static WorkerToleranceVectorOptionsValue ToleranceVector(
        Api.ToleranceVectorOptions value) =>
        new(
            VectorTolerance(value.HighX),
            VectorTolerance(value.HighY),
            VectorTolerance(value.HighZ),
            VectorTolerance(value.HighMagnitude),
            VectorTolerance(value.LowX),
            VectorTolerance(value.LowY),
            VectorTolerance(value.LowZ),
            VectorTolerance(value.LowMagnitude));

    private static WorkerToleranceLimit VectorTolerance(Api.ToleranceLimit? value) =>
        value is null
            ? new(false, 0)
            : new(value.HasEnabled && value.Enabled, value.HasValue ? value.Value : 0);

    private static WorkerScalarToleranceLimit Tolerance(Api.ScalarToleranceLimit? value) =>
        value is null
            ? new(false, 0)
            : new(value.HasEnabled && value.Enabled, value.HasValue ? value.Value : 0);

    private static WorkerColorizationOptionsValue Colorization(Api.ColorizationOptions value)
    {
        var defaults = DefaultColorization();
        return new(
            (int)(value.HasColorRangeMethod ? value.ColorRangeMethod : defaults.ColorRangeMethod) - 1,
            (int)(value.HasBaseHighColor ? value.BaseHighColor : defaults.BaseHighColor) - 1,
            (int)(value.HasBaseMidColor ? value.BaseMidColor : defaults.BaseMidColor) - 1,
            (int)(value.HasBaseLowColor ? value.BaseLowColor : defaults.BaseLowColor) - 1,
            value.HasDrawTubes ? value.DrawTubes : defaults.DrawTubes,
            value.HasDrawArrowheads ? value.DrawArrowheads : defaults.DrawArrowheads,
            value.HasIndicateValues ? value.IndicateValues : defaults.IndicateValues,
            value.HasVectorMagnification ? value.VectorMagnification : defaults.VectorMagnification,
            value.HasVectorWidth ? value.VectorWidth : defaults.VectorWidth,
            value.HasDrawBlotches ? value.DrawBlotches : defaults.DrawBlotches,
            value.HasBlotchSize ? value.BlotchSize : defaults.BlotchSize,
            value.HasShowOutOfToleranceOnly ? value.ShowOutOfToleranceOnly : defaults.ShowOutOfToleranceOnly,
            value.HasShowColorBarInView ? value.ShowColorBarInView : defaults.ShowColorBarInView,
            value.HasShowColorBarPercentages ? value.ShowColorBarPercentages : defaults.ShowColorBarPercentages,
            value.HasShowColorBarFractions ? value.ShowColorBarFractions : defaults.ShowColorBarFractions,
            value.HasHighSaturationLimit ? value.HighSaturationLimit : defaults.HighSaturationLimit,
            value.HasLowSaturationLimit ? value.LowSaturationLimit : defaults.LowSaturationLimit,
            value.HasHighTolerance ? value.HighTolerance : defaults.HighTolerance,
            value.HasLowTolerance ? value.LowTolerance : defaults.LowTolerance);
    }

    private static WorkerReportOutputOptionsValue ReportOutput(Api.ReportOutputOptions value) =>
        new(
            (int)value.OutputType - 1,
            value.DestinationCase == Api.ReportOutputOptions.DestinationOneofCase.ExternalPath
                ? value.ExternalPath
                : null,
            value.DestinationCase == Api.ReportOutputOptions.DestinationOneofCase.EmbeddedFile
                ? new WorkerEmbeddedReportFileValue(
                    value.EmbeddedFile.CollectionName,
                    value.EmbeddedFile.FileName)
                : null);

    private static WorkerProjectionOptionsValue Projection(Api.ProjectionOptions value) =>
        new(
            value.HasProjectionType ? value.ProjectionType : "Object To Probe Vectors",
            value.HasIgnoreEdgeProjections && value.IgnoreEdgeProjections,
            value.HasOverrideTargetOffsets && value.OverrideTargetOffsets,
            value.HasOverrideTargetOffsetsValue ? value.OverrideTargetOffsetsValue : 0,
            value.HasAddExtraMaterialThickness && value.AddExtraMaterialThickness,
            value.HasExtraMaterialThicknessValue ? value.ExtraMaterialThicknessValue : 0);

    private static WorkerPointDeltaReportOptionsValue PointDelta(
        Api.PointDeltaReportOptions value)
    {
        var defaults = DefaultPointDeltaReportOptions();
        return new(
            (int)(value.HasCoordinateSystem ? value.CoordinateSystem : defaults.CoordinateSystem) - 1,
            value.HasDetailsFormat ? value.DetailsFormat : defaults.DetailsFormat,
            value.HasShowPointA ? value.ShowPointA : defaults.ShowPointA,
            value.HasShowPointB ? value.ShowPointB : defaults.ShowPointB,
            value.HasShowDelta ? value.ShowDelta : defaults.ShowDelta,
            value.HasShowMagnitude ? value.ShowMagnitude : defaults.ShowMagnitude,
            value.HasShowComponent1 ? value.ShowComponent1 : defaults.ShowComponent1,
            value.HasShowComponent2 ? value.ShowComponent2 : defaults.ShowComponent2,
            value.HasShowComponent3 ? value.ShowComponent3 : defaults.ShowComponent3,
            value.HasSortPointNames ? value.SortPointNames : defaults.SortPointNames,
            value.HasShowToleranceFields ? value.ShowToleranceFields : defaults.ShowToleranceFields,
            value.HasColorizeInToleranceFields
                ? value.ColorizeInToleranceFields
                : defaults.ColorizeInToleranceFields);
    }

    private static bool IsSpecializedEnum(WorkerMpValueKind kind) =>
        kind is WorkerMpValueKind.AsciiImportFileFormat or
            WorkerMpValueKind.AsciiFrameSetFormat or
            WorkerMpValueKind.AxisIdentifier or
            WorkerMpValueKind.WcfAxisIdentifier or
            WorkerMpValueKind.BaseColorType or
            WorkerMpValueKind.BaseMidColorType or
            WorkerMpValueKind.ChartType or
            WorkerMpValueKind.CollimationBaselineType or
            WorkerMpValueKind.CollimationType or
            WorkerMpValueKind.ColorRangeMethod or
            WorkerMpValueKind.CoordinateSystemType or
            WorkerMpValueKind.VectorComponent or
            WorkerMpValueKind.DynamicCircleMode or
            WorkerMpValueKind.DynamicEllipseMode or
            WorkerMpValueKind.DynamicLineMode or
            WorkerMpValueKind.DynamicPlaneMode or
            WorkerMpValueKind.DynamicPointMode or
            WorkerMpValueKind.EdgeMode or
            WorkerMpValueKind.ExportDataDelimiterType or
            WorkerMpValueKind.ExportTargetNameFormat or
            WorkerMpValueKind.ExportVectorNameFormat or
            WorkerMpValueKind.GeometryType or
            WorkerMpValueKind.GdtDistanceBetweenMode or
            WorkerMpValueKind.GdtEvaluationMethod or
            WorkerMpValueKind.InstrumentType or
            WorkerMpValueKind.ObjectType or
            WorkerMpValueKind.OffsetDirectionType or
            WorkerMpValueKind.PointFilterInputType or
            WorkerMpValueKind.RelationshipWeightingMode or
            WorkerMpValueKind.RenderModeType or
            WorkerMpValueKind.ReportPageOrientation or
            WorkerMpValueKind.SaturationLimitType or
            WorkerMpValueKind.ShowUsmnDialogType or
            WorkerMpValueKind.SurfaceAnalysisMode or
            WorkerMpValueKind.SurfaceDissectionModeType or
            WorkerMpValueKind.TargetComputationMethod or
            WorkerMpValueKind.TranslucencyType or
            WorkerMpValueKind.CompTechnique or
            WorkerMpValueKind.DegreeOfFreedom or
            WorkerMpValueKind.FitMethod or
            WorkerMpValueKind.MeasuredSideForPlanarOffset or
            WorkerMpValueKind.MeasuredSideForRadialOffset or
            WorkerMpValueKind.MpDialogInteractionMode or
            WorkerMpValueKind.MpInteractionMode or
            WorkerMpValueKind.NormalDirection or
            WorkerMpValueKind.SaInteractionMode or
            WorkerMpValueKind.SlotType or
            WorkerMpValueKind.SphereFitComputationMode or
            WorkerMpValueKind.WindowState or
            WorkerMpValueKind.SystemString;

    private static object ToProtocolValue(
        WorkerMpOutputValue output,
        FieldDescriptor field,
        MpArgumentContract contract) =>
        output.Kind switch
        {
            WorkerMpValueKind.Logical => output.BooleanValue!.Value,
            WorkerMpValueKind.WholeNumber when field.FieldType == FieldType.Double =>
                Convert.ToDouble(output.IntegerValue!.Value, CultureInfo.InvariantCulture),
            WorkerMpValueKind.WholeNumber => output.IntegerValue!.Value,
            WorkerMpValueKind.FloatingPoint => output.DoubleValue!.Value,
            WorkerMpValueKind.Text when field.FieldType == FieldType.Enum =>
                ProtocolEnumValue(field.EnumType, output.StringValue!, contract.EnumTextValues),
            WorkerMpValueKind.Text when field.FieldType == FieldType.Message =>
                ProtocolStringMessage(field.MessageType, output.StringValue!),
            WorkerMpValueKind.CollectionName when field.FieldType == FieldType.Message =>
                new Api.CollectionName { Name = output.StringValue! },
            WorkerMpValueKind.Text or WorkerMpValueKind.CollectionName =>
                output.StringValue!,
            WorkerMpValueKind.DoubleArray when field.FieldType == FieldType.Message =>
                ProtocolDoubleArrayMessage(field.MessageType, output.DoubleArrayValue!.Values),
            WorkerMpValueKind.DoubleArray => output.DoubleArrayValue!.Values.Cast<object>().ToArray(),
            WorkerMpValueKind.EditText or WorkerMpValueKind.StringList =>
                output.StringListValue!.Values.Cast<object>().ToArray(),
            WorkerMpValueKind.PointName => ProtocolPoint(output.PointNameValue!),
            WorkerMpValueKind.PointNameList =>
                output.PointNameListValue!.Values.Select(ProtocolPoint).Cast<object>().ToArray(),
            WorkerMpValueKind.Vector when field.FieldType == FieldType.Message &&
                field.MessageType.FullName != "briosa.Vector" =>
                ProtocolVectorMessage(field.MessageType, output.VectorValue!),
            WorkerMpValueKind.Vector => new Api.Vector
            {
                X = output.VectorValue!.X,
                Y = output.VectorValue.Y,
                Z = output.VectorValue.Z
            },
            WorkerMpValueKind.ToleranceVectorOptions =>
                ProtocolToleranceVector(output.ToleranceVectorOptionsValue!),
            WorkerMpValueKind.Transform => new Api.Transform
            {
                Values = { output.TransformValue!.Values }
            },
            WorkerMpValueKind.WorldTransform => new Api.WorldTransform
            {
                Transform = new Api.Transform
                {
                    Values = { output.WorldTransformValue!.Transform.Values }
                },
                ScaleFactor = output.WorldTransformValue.ScaleFactor
            },
            WorkerMpValueKind.FileReference => new Api.FileReference
            {
                Path = output.FileReferenceValue!.Path,
                EmbeddedFile = output.FileReferenceValue.EmbeddedFile
            },
            WorkerMpValueKind.CollectionObjectName =>
                ProtocolObject(output.CollectionObjectNameValue!),
            WorkerMpValueKind.CollectionObjectNameList =>
                output.CollectionObjectNameListValue!.Values
                    .Select(ProtocolObject).Cast<object>().ToArray(),
            WorkerMpValueKind.CollectionInstrumentId =>
                new Api.CollectionInstrumentId
                {
                    CollectionName = output.CollectionInstrumentIdValue!.CollectionName,
                    InstrumentId = output.CollectionInstrumentIdValue.InstrumentId
                },
            WorkerMpValueKind.CollectionInstrumentIdList =>
                output.CollectionInstrumentIdListValue!.Values.Select(item =>
                    new Api.CollectionInstrumentId
                    {
                        CollectionName = item.CollectionName,
                        InstrumentId = item.InstrumentId
                    }).Cast<object>().ToArray(),
            WorkerMpValueKind.CollectionMachineId =>
                new Api.CollectionMachineId
                {
                    CollectionName = output.CollectionMachineIdValue!.CollectionName,
                    MachineId = output.CollectionMachineIdValue.MachineId
                },
            WorkerMpValueKind.CollectionItemName =>
                ProtocolItem(output.CollectionItemNameValue!),
            WorkerMpValueKind.CollectionItemNameList =>
                output.CollectionItemNameListValue!.Values
                    .Select(ProtocolItem).Cast<object>().ToArray(),
            WorkerMpValueKind.CollectionGroupNameList =>
                output.CollectionGroupNameListValue!.Values.Select(item =>
                    new Api.CollectionGroupName
                    {
                        CollectionName = item.CollectionName,
                        GroupName = item.GroupName
                    }).Cast<object>().ToArray(),
            WorkerMpValueKind.CollectionVectorGroupName =>
                new Api.CollectionVectorGroupName
                {
                    CollectionName = output.CollectionVectorGroupNameValue!.CollectionName,
                    VectorGroupName = output.CollectionVectorGroupNameValue.VectorGroupName
                },
            WorkerMpValueKind.CollectionVectorGroupNameList =>
                output.CollectionVectorGroupNameListValue!.Values.Select(item =>
                    new Api.CollectionVectorGroupName
                    {
                        CollectionName = item.CollectionName,
                        VectorGroupName = item.VectorGroupName
                    }).Cast<object>().ToArray(),
            WorkerMpValueKind.VectorNameList =>
                output.VectorNameListValue!.Values.Select(item => new Api.VectorName
                {
                    CollectionName = item.CollectionName,
                    GroupName = item.GroupName,
                    Name = item.VectorName
                }).Cast<object>().ToArray(),
            WorkerMpValueKind.FitConstraintScalarOptions =>
                ProtocolFitConstraint(output.FitConstraintScalarOptionsValue!),
            WorkerMpValueKind.ToleranceScalarOptions =>
                ProtocolToleranceScalar(output.ToleranceScalarOptionsValue!),
            _ => throw new InvalidOperationException(
                $"No result mapper exists for {output.Kind}.")
        };

    private static int ProtocolEnumValue(
        EnumDescriptor descriptor,
        string value,
        IReadOnlyList<string>? sdkValues)
    {
        if (sdkValues is null)
        {
            throw new InvalidOperationException(
                $"No SDK text mapping exists for {descriptor.FullName}.");
        }

        var index = -1;
        for (var candidate = 0; candidate < sdkValues.Count; candidate++)
        {
            if (string.Equals(sdkValues[candidate], value, StringComparison.Ordinal))
            {
                index = candidate;
                break;
            }
        }
        if (index < 0 || index + 1 >= descriptor.Values.Count)
        {
            throw new InvalidOperationException(
                $"SDK returned unknown {descriptor.FullName} value '{value}'.");
        }

        return descriptor.Values[index + 1].Number;
    }

    private static IMessage ProtocolStringMessage(
        MessageDescriptor descriptor,
        string value)
    {
        var message = descriptor.Parser.ParseFrom(Array.Empty<byte>());
        var field = descriptor.Fields.InFieldNumberOrder().Single(candidate =>
            candidate.FieldType == FieldType.String);
        field.Accessor.SetValue(message, value);
        return message;
    }

    private static IMessage ProtocolDoubleArrayMessage(
        MessageDescriptor descriptor,
        IReadOnlyList<double> values)
    {
        var message = (IMessage?)Activator.CreateInstance(descriptor.ClrType) ??
            throw new InvalidOperationException($"Cannot create {descriptor.FullName}.");
        var field = descriptor.Fields.InFieldNumberOrder().Single(candidate =>
            candidate.IsRepeated && candidate.FieldType == FieldType.Double);
        SetResultField(message, field, values.Cast<object>().ToArray());
        return message;
    }

    private static IMessage ProtocolVectorMessage(
        MessageDescriptor descriptor,
        WorkerVectorValue value)
    {
        var message = (IMessage?)Activator.CreateInstance(descriptor.ClrType) ??
            throw new InvalidOperationException($"Cannot create {descriptor.FullName}.");
        var fields = descriptor.Fields.InFieldNumberOrder()
            .Where(candidate => candidate.FieldType == FieldType.Double)
            .Take(3)
            .ToArray();
        if (fields.Length != 3)
        {
            throw new InvalidOperationException(
                $"{descriptor.FullName} cannot receive a three-component SDK vector.");
        }

        fields[0].Accessor.SetValue(message, value.X);
        fields[1].Accessor.SetValue(message, value.Y);
        fields[2].Accessor.SetValue(message, value.Z);
        return message;
    }

    private static Api.PointName ProtocolPoint(WorkerPointNameValue value) => new()
    {
        CollectionName = value.CollectionName,
        GroupName = value.GroupName,
        TargetName = value.TargetName
    };

    private static Api.CollectionObjectName ProtocolObject(
        WorkerCollectionObjectNameValue value) => new()
        {
            CollectionName = value.CollectionName,
            ObjectName = value.ObjectName,
            ObjectType = (Api.ObjectType)(int)value.ObjectType
        };

    private static Api.CollectionItemName ProtocolItem(
        WorkerCollectionItemNameValue value) => new()
        {
            CollectionName = value.CollectionName,
            ItemName = value.ItemName,
            ItemType = (Api.ItemType)(int)value.ItemType
        };

    private static Api.ToleranceVectorOptions ProtocolToleranceVector(
        WorkerToleranceVectorOptionsValue value) => new()
        {
            HighX = ProtocolTolerance(value.HighX),
            HighY = ProtocolTolerance(value.HighY),
            HighZ = ProtocolTolerance(value.HighZ),
            HighMagnitude = ProtocolTolerance(value.HighMagnitude),
            LowX = ProtocolTolerance(value.LowX),
            LowY = ProtocolTolerance(value.LowY),
            LowZ = ProtocolTolerance(value.LowZ),
            LowMagnitude = ProtocolTolerance(value.LowMagnitude)
        };

    private static Api.ToleranceLimit ProtocolTolerance(WorkerToleranceLimit value) =>
        new() { Enabled = value.Enabled, Value = value.Value };

    private static Api.FitConstraintScalarOptions ProtocolFitConstraint(
        WorkerFitConstraintScalarOptionsValue value) => new()
        {
            High = ProtocolScalarTolerance(value.High),
            Low = ProtocolScalarTolerance(value.Low)
        };

    private static Api.ToleranceScalarOptions ProtocolToleranceScalar(
        WorkerToleranceScalarOptionsValue value) => new()
        {
            High = ProtocolScalarTolerance(value.High),
            Low = ProtocolScalarTolerance(value.Low)
        };

    private static Api.ScalarToleranceLimit ProtocolScalarTolerance(
        WorkerScalarToleranceLimit value) =>
        new() { Enabled = value.Enabled, Value = value.Value };

    private static void SetResultField(
        IMessage result,
        FieldDescriptor field,
        object value)
    {
        if (!field.IsRepeated)
        {
            field.Accessor.SetValue(result, value);
            return;
        }

        var target = field.Accessor.GetValue(result);
        var elementType = target.GetType().GetGenericArguments().Single();
        var add = target.GetType().GetMethods()
            .Single(method => method.Name == "Add" &&
                method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType == elementType);
        foreach (var item in (object[])value)
        {
            _ = add.Invoke(target, [item]);
        }
    }

    private static string Normalize(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit)).ToUpperInvariant();
}
