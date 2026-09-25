using System.Buffers.Binary;
using System.Text.Json;

namespace Briosa.Worker.Control;

public sealed class WorkerControlChannel(Stream stream, bool leaveOpen = false) : IDisposable
{
    private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private readonly bool _leaveOpen = leaveOpen;
    private int _disposeState;

    public void Send(WorkerControlMessage message)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        var payload = Serialize(message);
        Span<byte> header = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        _stream.Write(header);
        _stream.Write(payload);
        _stream.Flush();
    }

    public WorkerControlMessage Receive()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        Span<byte> header = stackalloc byte[sizeof(int)];
        _stream.ReadExactly(header);
        var payload = new byte[ReadLength(header)];
        _stream.ReadExactly(payload);
        return Deserialize(payload);
    }

    public async ValueTask SendAsync(
        WorkerControlMessage message,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        var payload = Serialize(message);
        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        await _stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await _stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<WorkerControlMessage> ReceiveAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        var header = new byte[sizeof(int)];
        await _stream.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);
        var payload = new byte[ReadLength(header)];
        await _stream.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);
        return Deserialize(payload);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) == 0 && !_leaveOpen)
        {
            _stream.Dispose();
        }
    }

    private static byte[] Serialize(WorkerControlMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        try
        {
            Validate(message);
            var payload = JsonSerializer.SerializeToUtf8Bytes(message, WorkerControlJsonContext.Default.WorkerControlMessage);
            if (payload.Length > WorkerControlProtocol.MaximumMessageBytes)
            {
                throw new InvalidDataException("The worker control message exceeds the size limit.");
            }

            return payload;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or JsonException)
        {
            // Preparation is complete before Send/SendAsync writes even the frame header.
            throw new WorkerMessageRejectedException("The worker message could not be encoded.", exception);
        }
    }

    private static WorkerControlMessage Deserialize(ReadOnlySpan<byte> payload)
    {
        try
        {
            var message = JsonSerializer.Deserialize(payload, WorkerControlJsonContext.Default.WorkerControlMessage)
                ?? throw new InvalidDataException("The worker control message was empty.");
            Validate(message);
            return message;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            // Constructors enforce outcome invariants during deserialization.
            // Keep their failures inside the malformed-channel boundary.
            throw new InvalidDataException("The worker control message contains invalid data.", exception);
        }
    }

    private static int ReadLength(ReadOnlySpan<byte> header)
    {
        var length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length is <= 0 or > WorkerControlProtocol.MaximumMessageBytes)
        {
            throw new InvalidDataException("The worker control message length is invalid.");
        }

        return length;
    }

    private static void Validate(WorkerControlMessage message)
    {
        if (message.ProtocolVersion != WorkerControlProtocol.CurrentVersion)
        {
            throw new InvalidDataException(
                $"Unsupported worker control protocol version '{message.ProtocolVersion}'.");
        }

        if (!Enum.IsDefined(message.Kind) || message.Kind == WorkerControlMessageKind.None)
        {
            throw new InvalidDataException("The worker control message kind is invalid.");
        }

        if (message.Kind == WorkerControlMessageKind.Ready &&
            (message.ProcessId is not > 0 || message.Connection is null))
        {
            throw new InvalidDataException(
                "A worker ready message requires a process identifier and connection snapshot.");
        }

        if (message.Kind == WorkerControlMessageKind.Pong &&
            message.Connection is null)
        {
            throw new InvalidDataException(
                "A worker heartbeat response requires a connection snapshot.");
        }

        if (message.Connection is not null)
        {
            ValidateConnection(message.Connection);
        }

        if (message.Kind == WorkerControlMessageKind.Execute)
        {
            ValidateCommand(message.Command);
        }

        if (message.Kind == WorkerControlMessageKind.ExecutionResult)
        {
            ValidateExecutionResponse(message.ExecutionResponse);
        }

        if (message.Kind == WorkerControlMessageKind.ConnectionResult &&
            message.Connection is null)
        {
            throw new InvalidDataException(
                "A connection result requires a connection snapshot.");
        }

        if (message.Kind == WorkerControlMessageKind.ExecutionVerificationResult &&
            (message.Connection is null ||
                message.Connection.State != WorkerConnectionState.Connected ||
                message.Connection.ExecutionReadinessState is not
                    (WorkerExecutionReadinessState.ExecutionReady or
                        WorkerExecutionReadinessState.OperatorRecoveryRequired)))
        {
            throw new InvalidDataException(
                "An execution-verification result requires a connection snapshot.");
        }
    }

    private static void ValidateConnection(WorkerConnectionSnapshot connection)
    {
        if (!Enum.IsDefined(connection.State) ||
            !Enum.IsDefined(connection.Failure) ||
            connection.Failure != WorkerConnectionFailure.None &&
            connection.State is not (WorkerConnectionState.Faulted or WorkerConnectionState.Connecting) ||
            !Enum.IsDefined(connection.ExecutionReadinessState) ||
            connection.Attempt < 0 ||
            connection.MaximumAttempts < 1 ||
            connection.Attempt > connection.MaximumAttempts ||
            string.IsNullOrWhiteSpace(connection.DiagnosticCode) ||
            connection.State != WorkerConnectionState.Connected &&
            connection.ExecutionReadinessState != WorkerExecutionReadinessState.Unverified)
        {
            throw new InvalidDataException(
                "The worker connection snapshot has an invalid state or shape.");
        }

        if (connection.RuntimeIdentity is { } identity &&
            (!IsValidIdentityEvidence(identity.ActivatedSdk) ||
                !IsValidIdentityEvidence(identity.ConnectedSpatialAnalyzer)))
        {
            throw new InvalidDataException(
                "The worker runtime identity evidence has an invalid shape.");
        }
    }

    private static bool IsValidIdentityEvidence(WorkerRuntimeIdentityEvidence? evidence) =>
        evidence is not null && Enum.IsDefined(evidence.Source) && evidence.Source switch
        {
            WorkerRuntimeIdentityEvidenceSource.Unavailable => evidence.Version is null,
            WorkerRuntimeIdentityEvidenceSource.RuntimeVerified =>
                !string.IsNullOrWhiteSpace(evidence.Version) &&
                evidence.Version.Length <= 128 &&
                !evidence.Version.Contains('\r', StringComparison.Ordinal) &&
                !evidence.Version.Contains('\n', StringComparison.Ordinal),
            _ => false
        };

    private static void ValidateCommand(WorkerMpCommand? command)
    {
        if (command is null ||
            string.IsNullOrWhiteSpace(command.OperationId) ||
            string.IsNullOrWhiteSpace(command.StepName) ||
            command.InputArguments is null ||
            command.OutputArguments is null)
        {
            throw new InvalidDataException(
                "A worker execute message requires a valid MP command.");
        }

        if (command.InputArguments.Count + command.OutputArguments.Count > 128 ||
            command.InputArguments.Any(argument =>
                argument is null || string.IsNullOrWhiteSpace(argument.Name) ||
                !HasInputValueForKind(argument)) ||
            command.OutputArguments.Any(argument =>
                argument is null || string.IsNullOrWhiteSpace(argument.Name) ||
                !Enum.IsDefined(argument.Kind) ||
                argument.ArraySize is < 0 or > 1_000_000 ||
                argument.ArraySize.HasValue && argument.Kind != WorkerMpValueKind.DoubleArray ||
                argument.ObjectTypeWhenOmitted is { } objectType &&
                (argument.Kind != WorkerMpValueKind.CollectionObjectName ||
                 objectType == WorkerObjectTypeValue.Unspecified ||
                 !Enum.IsDefined(objectType))))
        {
            throw new InvalidDataException("The worker MP argument collection is invalid.");
        }
    }

    private static bool HasInputValueForKind(WorkerMpInputArgument argument) =>
        argument.Kind switch
        {
            WorkerMpValueKind.Logical => ((argument.Value as WorkerBooleanValue)?.Value).HasValue,
            WorkerMpValueKind.WholeNumber => ((argument.Value as WorkerIntegerValue)?.Value).HasValue,
            WorkerMpValueKind.FloatingPoint => ((argument.Value as WorkerDoubleValue)?.Value).HasValue,
            WorkerMpValueKind.DoubleArray => IsValid((argument.Value as WorkerDoubleArrayValue)),
            WorkerMpValueKind.EditText => IsValid((argument.Value as WorkerStringListValue)),
            WorkerMpValueKind.Transform => IsValid((argument.Value as WorkerTransformValue)),
            WorkerMpValueKind.WorldTransform => IsValid((argument.Value as WorkerWorldTransformValue)),
            WorkerMpValueKind.RgbColor => (argument.Value as WorkerRgbColorValue) is not null,
            WorkerMpValueKind.FileReference => IsValid((argument.Value as WorkerFileReferenceValue)),
            WorkerMpValueKind.AngularUnit =>
                IsValid(((argument.Value as WorkerAngularUnitChoice)?.Value), WorkerAngularUnitValue.Unspecified),
            WorkerMpValueKind.DistanceUnit =>
                IsValid(((argument.Value as WorkerDistanceUnitChoice)?.Value), WorkerDistanceUnitValue.Unspecified),
            WorkerMpValueKind.TemperatureUnit =>
                IsValid(((argument.Value as WorkerTemperatureUnitChoice)?.Value), WorkerTemperatureUnitValue.Unspecified),
            WorkerMpValueKind.Font => IsValid((argument.Value as WorkerFontValue)),
            WorkerMpValueKind.Text or
            WorkerMpValueKind.InstrumentTypeName or
            WorkerMpValueKind.ChartName or
            WorkerMpValueKind.CloudName or
            WorkerMpValueKind.CollectionName or
            WorkerMpValueKind.FrameName or
            WorkerMpValueKind.VectorGroupName or
            WorkerMpValueKind.ViewName => ((argument.Value as WorkerTextValue)?.Value) is not null,
            WorkerMpValueKind.PointName => IsValid((argument.Value as WorkerPointNameValue)),
            WorkerMpValueKind.Vector => (argument.Value as WorkerVectorValue) is not null,
            WorkerMpValueKind.ToleranceVectorOptions =>
                IsValid((argument.Value as WorkerToleranceVectorOptionsValue)),
            WorkerMpValueKind.CollectionInstrumentId =>
                IsValid((argument.Value as WorkerCollectionInstrumentIdValue)),
            WorkerMpValueKind.CollectionInstrumentIdList =>
                IsValid((argument.Value as WorkerCollectionInstrumentIdListValue)),
            WorkerMpValueKind.CollectionMachineId =>
                IsValid((argument.Value as WorkerCollectionMachineIdValue)),
            WorkerMpValueKind.CollectionItemName =>
                IsValid((argument.Value as WorkerCollectionItemNameValue)),
            WorkerMpValueKind.CollectionItemNameList =>
                IsValid((argument.Value as WorkerCollectionItemNameListValue)),
            WorkerMpValueKind.CollectionObjectName =>
                IsValid((argument.Value as WorkerCollectionObjectNameValue)),
            WorkerMpValueKind.CollectionObjectNameList =>
                IsValid((argument.Value as WorkerCollectionObjectNameListValue)),
            WorkerMpValueKind.CollectionGroupNameList =>
                IsValid((argument.Value as WorkerCollectionGroupNameListValue)),
            WorkerMpValueKind.CollectionVectorGroupName =>
                IsValid((argument.Value as WorkerCollectionVectorGroupNameValue)),
            WorkerMpValueKind.CollectionVectorGroupNameList =>
                IsValid((argument.Value as WorkerCollectionVectorGroupNameListValue)),
            WorkerMpValueKind.PointNameList => IsValid((argument.Value as WorkerPointNameListValue)),
            WorkerMpValueKind.StringList => IsValid((argument.Value as WorkerStringListValue)),
            WorkerMpValueKind.VectorNameList => IsValid((argument.Value as WorkerVectorNameListValue)),
            _ => WorkerSpecializedValueValidation.HasInputValueForKind(argument)
        };

    private static bool HasOutputValueForKind(WorkerMpOutputValue output) =>
        !output.Retrieved ||
        output.Kind switch
        {
            WorkerMpValueKind.Logical => ((output.ReadValue() as WorkerBooleanValue)?.Value).HasValue,
            WorkerMpValueKind.WholeNumber => ((output.ReadValue() as WorkerIntegerValue)?.Value).HasValue,
            WorkerMpValueKind.FloatingPoint => ((output.ReadValue() as WorkerDoubleValue)?.Value).HasValue,
            WorkerMpValueKind.DoubleArray => IsValid((output.ReadValue() as WorkerDoubleArrayValue)),
            WorkerMpValueKind.EditText => IsValid((output.ReadValue() as WorkerStringListValue)),
            WorkerMpValueKind.Transform => IsValid((output.ReadValue() as WorkerTransformValue)),
            WorkerMpValueKind.WorldTransform => IsValid((output.ReadValue() as WorkerWorldTransformValue)),
            WorkerMpValueKind.FileReference => IsValid((output.ReadValue() as WorkerFileReferenceValue)),
            WorkerMpValueKind.Text or
            WorkerMpValueKind.InstrumentTypeName or
            WorkerMpValueKind.ChartName or
            WorkerMpValueKind.CloudName or
            WorkerMpValueKind.CollectionName or
            WorkerMpValueKind.FrameName or
            WorkerMpValueKind.VectorGroupName or
            WorkerMpValueKind.ViewName => ((output.ReadValue() as WorkerTextValue)?.Value) is not null,
            WorkerMpValueKind.PointName => IsValid((output.ReadValue() as WorkerPointNameValue)),
            WorkerMpValueKind.Vector => (output.ReadValue() as WorkerVectorValue) is not null,
            WorkerMpValueKind.ToleranceVectorOptions =>
                IsValid((output.ReadValue() as WorkerToleranceVectorOptionsValue)),
            WorkerMpValueKind.CollectionInstrumentId =>
                IsValid((output.ReadValue() as WorkerCollectionInstrumentIdValue)),
            WorkerMpValueKind.CollectionInstrumentIdList =>
                IsValid((output.ReadValue() as WorkerCollectionInstrumentIdListValue)),
            WorkerMpValueKind.CollectionMachineId =>
                IsValid((output.ReadValue() as WorkerCollectionMachineIdValue)),
            WorkerMpValueKind.CollectionItemName =>
                IsValid((output.ReadValue() as WorkerCollectionItemNameValue)),
            WorkerMpValueKind.CollectionItemNameList =>
                IsValid((output.ReadValue() as WorkerCollectionItemNameListValue)),
            WorkerMpValueKind.CollectionObjectName =>
                IsValid((output.ReadValue() as WorkerCollectionObjectNameValue)),
            WorkerMpValueKind.CollectionObjectNameList =>
                IsValid((output.ReadValue() as WorkerCollectionObjectNameListValue)),
            WorkerMpValueKind.CollectionGroupNameList =>
                IsValid((output.ReadValue() as WorkerCollectionGroupNameListValue)),
            WorkerMpValueKind.CollectionVectorGroupName =>
                IsValid((output.ReadValue() as WorkerCollectionVectorGroupNameValue)),
            WorkerMpValueKind.CollectionVectorGroupNameList =>
                IsValid((output.ReadValue() as WorkerCollectionVectorGroupNameListValue)),
            WorkerMpValueKind.PointNameList => IsValid((output.ReadValue() as WorkerPointNameListValue)),
            WorkerMpValueKind.StringList => IsValid((output.ReadValue() as WorkerStringListValue)),
            WorkerMpValueKind.VectorNameList => IsValid((output.ReadValue() as WorkerVectorNameListValue)),
            _ => WorkerSpecializedValueValidation.HasOutputValueForKind(output)
        };

    private static bool IsValid(WorkerPointNameValue? value) =>
        value is not null &&
        value.CollectionName is not null &&
        value.GroupName is not null &&
        value.TargetName is not null;

    private static bool IsValid(WorkerCollectionInstrumentIdValue? value) =>
        value is not null && value.CollectionName is not null;

    private static bool IsValid(WorkerCollectionMachineIdValue? value) =>
        value is not null && value.CollectionName is not null;

    private static bool IsValid(WorkerCollectionItemNameValue? value) =>
        value is not null &&
        value.CollectionName is not null &&
        value.ItemName is not null &&
        value.ItemType is not WorkerItemTypeValue.Unspecified &&
        Enum.IsDefined(value.ItemType);

    private static bool IsValid(WorkerCollectionObjectNameValue? value) =>
        value is not null &&
        value.CollectionName is not null &&
        value.ObjectName is not null &&
        value.ObjectType is not WorkerObjectTypeValue.Unspecified &&
        Enum.IsDefined(value.ObjectType);

    private static bool IsValid(WorkerCollectionGroupNameValue? value) =>
        value is not null && value.CollectionName is not null && value.GroupName is not null;

    private static bool IsValid(WorkerCollectionVectorGroupNameValue? value) =>
        value is not null && value.CollectionName is not null && value.VectorGroupName is not null;

    private static bool IsValid(WorkerVectorNameValue? value) =>
        value is not null &&
        value.CollectionName is not null &&
        value.GroupName is not null &&
        value.VectorName is not null;

    private static bool IsValid(WorkerCollectionInstrumentIdListValue? value) =>
        value?.Values is not null && value.Values.All(IsValid);

    private static bool IsValid(WorkerCollectionGroupNameListValue? value) =>
        value?.Values is not null && value.Values.All(IsValid);

    private static bool IsValid(WorkerCollectionItemNameListValue? value) =>
        value?.Values is not null && value.Values.All(IsValid);

    private static bool IsValid(WorkerCollectionObjectNameListValue? value) =>
        value?.Values is not null && value.Values.All(IsValid);

    private static bool IsValid(WorkerCollectionVectorGroupNameListValue? value) =>
        value?.Values is not null && value.Values.All(IsValid);

    private static bool IsValid(WorkerPointNameListValue? value) =>
        value?.Values is not null && value.Values.All(IsValid);

    private static bool IsValid(WorkerStringListValue? value) =>
        value?.Values is not null && value.Values.All(item => item is not null);

    private static bool IsValid(WorkerDoubleArrayValue? value) =>
        value?.Values is not null;

    private static bool IsValid(WorkerTransformValue? value) =>
        value?.Values is { Count: 16 };

    private static bool IsValid(WorkerWorldTransformValue? value) =>
        value is not null && IsValid(value.Transform);

    private static bool IsValid(WorkerFileReferenceValue? value) =>
        value?.Path is not null;

    private static bool IsValid(WorkerFontValue? value) =>
        value is not null &&
        value.FontName is not null &&
        value.Color is not null;

    private static bool IsValid<T>(T? value, T unspecified)
        where T : struct, Enum =>
        value.HasValue &&
        !EqualityComparer<T>.Default.Equals(value.Value, unspecified) &&
        Enum.IsDefined(value.Value);

    private static bool IsValid(WorkerVectorNameListValue? value) =>
        value?.Values is not null && value.Values.All(IsValid);
    private static bool IsValid(WorkerToleranceVectorOptionsValue? value) =>
        value is not null &&
        value.HighX is not null &&
        value.HighY is not null &&
        value.HighZ is not null &&
        value.HighMagnitude is not null &&
        value.LowX is not null &&
        value.LowY is not null &&
        value.LowZ is not null &&
        value.LowMagnitude is not null;

    private static void ValidateExecutionResponse(WorkerExecutionResponse? response)
    {
        if (response is null || !Enum.IsDefined(response.Status) ||
            response.Connection is null ||
            ((response.Status == WorkerExecutionResponseStatus.Completed) !=
                (response.Execution is not null)) ||
            response.Execution is { } execution &&
            execution.OutputValues.Any(output =>
                    output is null || !Enum.IsDefined(output.Kind) ||
                    string.IsNullOrWhiteSpace(output.Name) ||
                    !HasOutputValueForKind(output)))
        {
            throw new InvalidDataException(
                "The worker execution-result message has an invalid response shape.");
        }

        ValidateConnection(response.Connection);
    }
}
