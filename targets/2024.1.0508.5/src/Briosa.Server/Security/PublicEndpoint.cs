using System.Globalization;
using System.Net;

namespace Briosa.Server.Security;

internal sealed record PublicEndpoint(IPAddress Address, int Port);
