namespace Briosa.Worker.Control;

public sealed record WorkerUdpTransmitSettingsValue(
    bool Enabled,
    bool Broadcast,
    string IpAddress,
    int Port) : WorkerMpValue;
