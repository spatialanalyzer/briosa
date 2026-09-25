using System.Diagnostics;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;
using Briosa.Server.Operations;

namespace Briosa.Server.Services;

internal sealed record SpatialAnalyzerInstallation(string? ExecutablePath, string? DiagnosticCode);
