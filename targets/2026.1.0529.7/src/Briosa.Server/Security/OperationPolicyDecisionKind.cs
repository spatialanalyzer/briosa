using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text;
using Briosa.Worker.Control;

namespace Briosa.Server.Security;

internal enum OperationPolicyDecisionKind
{
    Allowed,
    Denied,
    Unsupported
}
