using System.Buffers.Binary;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text.Json;

namespace Briosa.Desktop;

// This private envelope is never written to diagnostic logs or support exports.
// It contains local routing information; public state remains standard protobuf.
public sealed record DesktopReply
{
    public int SchemaVersion { get; init; } = 1;
    public required string Instance { get; init; }
    public required string Target { get; init; }
    public required string Endpoint { get; init; }
    public string HostState { get; init; } = "Starting";
    public bool CanManage { get; init; }
    public bool Accepted { get; init; } = true;
    public string Diagnostic { get; init; } = "";
    public string LogInstance { get; init; } = "";
    public string? LogDirectory { get; init; }
    public string ServerInfoJson { get; init; } = "{}";
    public string SdkStateJson { get; init; } = "{}";
    public string ApplicationStateJson { get; init; } = "{}";
}
