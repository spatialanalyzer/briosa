using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;

namespace Briosa.Server.Operations.WaveA;

internal sealed record MpArgumentContract(
    string FieldName,
    string MpName,
    WorkerMpValueKind Kind,
    string SdkBinding,
    string DefaultValue,
    bool Required,
    WorkerObjectTypeValue? ObjectTypeWhenOmitted = null,
    IReadOnlyList<string>? EnumTextValues = null,
    bool OmitWhenAbsent = false,
    string? NestedFieldName = null,
    WorkerItemTypeValue? ItemTypeWhenOmitted = null,
    string? ArraySizeFieldName = null);
