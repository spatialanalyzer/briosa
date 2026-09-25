using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;

namespace Briosa.Desktop;
public sealed record DesktopExit(string Instance, bool Clean);
