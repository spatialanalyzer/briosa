using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;

namespace Briosa.Desktop;

public sealed record DesktopRegistration(string Instance, string PackageDirectory, int ProcessId, long StartTimeUtcTicks);
