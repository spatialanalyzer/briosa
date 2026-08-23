using Briosa.Server.Operations.WaveA;

namespace Briosa.Server.Operations.WaveB;

internal static class WaveBOperationCatalog
{
    public static IReadOnlyList<MpOperationContract> Operations { get; } =
    [
        .. CloudAndMeshOperationCatalog.Operations,
        .. ConstructionWaveBOperationCatalog.Operations,
        .. GdtWaveBOperationCatalog.Operations,
        .. InstrumentWaveBOperationCatalog.Operations,
        .. RelationshipWaveBOperationCatalog.Operations,
        .. RobotCalibrationApplianceNodeWaveBOperationCatalog.Operations,
        .. RobotWaveBOperationCatalog.Operations
    ];
}
