using System.Buffers.Binary;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text.Json;

namespace Briosa.Desktop;

public enum DesktopAction
{
    Status, StopServer, StartSdk, Connect, Reconnect, StopSdk, RecoverSdk
}
