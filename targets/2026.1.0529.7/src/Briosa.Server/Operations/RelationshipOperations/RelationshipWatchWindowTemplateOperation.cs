using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class RelationshipWatchWindowTemplateOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.relationship_watch_window_template", "Relationship Watch Window Template",
        "briosa.RelationshipOperations", "RelationshipWatchWindowTemplate",
        "/briosa.RelationshipOperations/RelationshipWatchWindowTemplate",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RelationshipWatchWindowTemplateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var udp = request.UdpNetworkTransmitSettings ??
            throw new ArgumentException("Request field 'udp_network_transmit_settings' is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Watch Window Template Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.WatchWindowTemplateName, "watch_window_template_name"), "SetCollectionObjectNameArg2"),
                new("Linear Precision", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasLinearPrecision ? request.LinearPrecision : 4), "SetIntegerArg"),
                new("Angular Precision", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasAngularPrecision ? request.AngularPrecision : 3), "SetIntegerArg"),
                new("Font", WorkerMpValueKind.Font, FontMapper.WithDefaults(request.Font), "SetFontTypeArg"),
                new("Text Color", WorkerMpValueKind.RgbColor, ToColor(request.TextColor), "SetColorArg"),
                new("Background Color", WorkerMpValueKind.RgbColor, ToColor(request.BackgroundColor), "SetColorArg"),
                new("Highlight Color", WorkerMpValueKind.RgbColor, ToColor(request.HighlightColor), "SetColorArg"),
                new("Show Deviation X (Rx)?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasShowDeviationX || request.ShowDeviationX), "SetBoolArg"),
                new("Show Deviation Y (Ry)?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasShowDeviationY || request.ShowDeviationY), "SetBoolArg"),
                new("Show Deviation Z (Rz)?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasShowDeviationZ || request.ShowDeviationZ), "SetBoolArg"),
                new("Show Deviation Mag?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasShowDeviationMagnitude || request.ShowDeviationMagnitude), "SetBoolArg"),
                new("UDP Network Transmit Settings", WorkerMpValueKind.UdpTransmitSettings,
                    new WorkerUdpTransmitSettingsValue(udp.HasEnabled && udp.Enabled,
                        !udp.HasBroadcast || udp.Broadcast, udp.HasIpAddress ? udp.IpAddress : string.Empty,
                        udp.HasPort ? udp.Port : 10000), "SetUdpTransmitSettingsArg"),
                new("Transparent Background?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasTransparentBackground && request.TransparentBackground), "SetBoolArg"),
                new("Hide Units?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasHideUnits && request.HideUnits), "SetBoolArg")
            ], []);
    }

    public static Api.RelationshipWatchWindowTemplateResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };

    private static WorkerRgbColorValue ToColor(Api.Color? value)
    {
        if (value is null)
            return new(255, 0, 0);
        if (value.Red > byte.MaxValue || value.Green > byte.MaxValue || value.Blue > byte.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), "Color channels must be in 0..255.");
        return new((byte)value.Red, (byte)value.Green, (byte)value.Blue);
    }
}
