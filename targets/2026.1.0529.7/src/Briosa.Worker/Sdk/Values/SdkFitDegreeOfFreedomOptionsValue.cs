namespace Briosa.Worker.Sdk;

internal sealed record SdkFitDegreeOfFreedomOptionsValue(
    bool AllowX,
    bool AllowY,
    bool AllowZ,
    bool AllowRx,
    bool AllowRy,
    bool AllowRz,
    bool RotateAboutCentroid);
