using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Text.Json;
using Briosa.Desktop;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Google.Protobuf;

namespace Briosa.Server.Services;

internal sealed record DesktopHostOptions(bool Enabled, bool AutoLaunch, string Instance, string? Credential)
{
    public bool Announced { get; init; }
    public static DesktopHostOptions Create(IConfiguration configuration)
    {
        var instance = Environment.GetEnvironmentVariable(DesktopProtocol.InstanceVariable);
        var credential = Environment.GetEnvironmentVariable(DesktopProtocol.CredentialVariable);
        // Child workers and the monitoring companion must never inherit shutdown authority.
        Environment.SetEnvironmentVariable(DesktopProtocol.InstanceVariable, null);
        Environment.SetEnvironmentVariable(DesktopProtocol.CredentialVariable, null);
        var mode = configuration["Briosa:Desktop:Mode"] ?? "Auto";
        if (mode is not ("Auto" or "Disabled" or "Owned"))
            throw new InvalidOperationException("Invalid Briosa desktop mode.");
        if (mode == "Owned" && (!DesktopProtocol.IsInstance(instance) || credential is not { Length: 64 } ||
            !credential.All(char.IsAsciiHexDigit)))
            throw new InvalidOperationException("Invalid desktop ownership configuration.");
        using var process = Process.GetCurrentProcess();
        bool interactive = Environment.UserInteractive && process.SessionId != 0;
        bool enabled = mode == "Owned" || mode == "Auto" && interactive &&
            File.Exists(Path.Combine(AppContext.BaseDirectory, "Briosa.ControlCenter.exe"));
        return new(enabled, enabled && mode == "Auto", mode == "Owned" ? instance! : DesktopProtocol.NewInstance(), mode == "Owned" ? credential : null);
    }
}
