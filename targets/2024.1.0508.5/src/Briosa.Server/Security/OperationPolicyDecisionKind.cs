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

/// <summary>What a call's caller restriction (#443) decided.</summary>
internal enum OperationCallerRestrictionOutcome
{
    /// <summary>The call carried no caller restriction.</summary>
    Absent,

    /// <summary>The server's configuration admitted the request, and so did the caller restriction.</summary>
    Admitted,

    /// <summary>The server's configuration admitted the request, and the caller restriction denied it.</summary>
    Denied,

    /// <summary>
    /// The server's configuration already denied the request, or the operation is
    /// unsupported. The restriction was not evaluated and changed nothing.
    /// </summary>
    NotEvaluated
}
