namespace Briosa.Worker.Sdk;

internal sealed record SdkUdpTransmitSettingsValue(
    bool Enabled,
    bool Broadcast,
    string IpAddress,
    int Port);
