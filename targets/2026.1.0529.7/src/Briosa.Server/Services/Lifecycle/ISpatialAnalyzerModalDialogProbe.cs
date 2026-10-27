namespace Briosa.Server.Services;

/// <summary>
/// Reports whether a SpatialAnalyzer modal dialog is open (#313). It only observes:
/// it never reads window text and never sends input, so it cannot dismiss a dialog.
/// </summary>
internal interface ISpatialAnalyzerModalDialogProbe
{
    SpatialAnalyzerModalDialogState Observe();
}

internal enum SpatialAnalyzerModalDialogState
{
    /// <summary>No eligible SpatialAnalyzer process could be observed.</summary>
    NotObserved,
    NoModalDialog,
    ModalDialogOpen
}
