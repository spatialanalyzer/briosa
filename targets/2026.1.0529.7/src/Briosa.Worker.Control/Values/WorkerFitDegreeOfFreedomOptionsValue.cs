namespace Briosa.Worker.Control;

public sealed record WorkerFitDegreeOfFreedomOptionsValue(
    bool AllowX,
    bool AllowY,
    bool AllowZ,
    bool AllowRx,
    bool AllowRy,
    bool AllowRz,
    bool RotateAboutCentroid);
