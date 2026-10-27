namespace Briosa.Server.Workers;

/// <summary>
/// The SA Interaction Mode that the supervisor sets after each Connect (#313).
/// <see cref="Unchanged"/> sends nothing and leaves SA's mode to the operator.
/// SA keeps the mode it is given across restarts and has no getter for it.
/// </summary>
internal enum SpatialAnalyzerInteractionMode
{
    Unchanged,
    Silent,
    Automatic,
    Manual
}
