using System.Buffers.Binary;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text.Json;

namespace Briosa.Desktop;

public sealed record DesktopRequest(
    string Instance, DesktopAction Action = DesktopAction.Status,
    string? Credential = null, int ExpectedSdkGeneration = 0);
