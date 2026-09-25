using Briosa.Worker.Control;
namespace Briosa.Worker.Sdk;

internal enum SdkConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Faulted,
    Stopping
}
