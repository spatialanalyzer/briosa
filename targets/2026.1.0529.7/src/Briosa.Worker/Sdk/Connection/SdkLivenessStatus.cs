using Briosa.Worker.Control;
namespace Briosa.Worker.Sdk;

internal enum SdkLivenessStatus
{
    Alive,
    ProcessExited,
    Unavailable
}
