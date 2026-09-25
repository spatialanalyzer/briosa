namespace Briosa.Worker.Sdk;

internal sealed record SdkCloudThinningOptionsValue(
    SdkCloudThinningModeValue Mode,
    int PointIncrement,
    int MinimumNumberOfPoints,
    int MaximumNumberOfPoints);
