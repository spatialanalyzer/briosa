using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal enum RuntimeIdentityMatchState
{
    Unavailable,
    ExactMatch,
    Mismatch
}
