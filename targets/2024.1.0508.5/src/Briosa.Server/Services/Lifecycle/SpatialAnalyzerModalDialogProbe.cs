using System.ComponentModel;

namespace Briosa.Server.Services;

/// <summary>
/// Finds a modal dialog of an eligible SpatialAnalyzer process (#313). A modal
/// dialog disables its owner window, so the rule is a visible standard dialog
/// (window class <c>#32770</c>, used by message boxes and dialog resources) whose
/// owner is a disabled window of the same process. It reads only window ownership,
/// visibility, enabled state and class: no window text and no input. Several eligible
/// instances are an ambiguous selection, as in the lifecycle coordinator, so a dialog
/// cannot be attributed to the connected instance and nothing is observed.
/// </summary>
internal sealed class SpatialAnalyzerModalDialogProbe(
    SpatialAnalyzerApplicationOptions options,
    ISpatialAnalyzerProcessPlatform processPlatform,
    ISpatialAnalyzerWindowSource windowSource) : ISpatialAnalyzerModalDialogProbe
{
    internal const string DialogClassName = "#32770";

    private readonly SpatialAnalyzerApplicationOptions _options = options;
    private readonly ISpatialAnalyzerProcessPlatform _processPlatform = processPlatform;
    private readonly ISpatialAnalyzerWindowSource _windowSource = windowSource;

    public SpatialAnalyzerModalDialogState Observe()
    {
        try
        {
            var processIds = _processPlatform.ObserveEligibleProcesses(_options.ExecutablePath)
                .Select(static observation => observation.Identity.ProcessId)
                .ToHashSet();
            if (processIds.Count != 1) return SpatialAnalyzerModalDialogState.NotObserved;
            return _windowSource.GetTopLevelWindows(processIds).Any(window =>
                processIds.Contains(window.ProcessId) &&
                window.Visible &&
                string.Equals(window.ClassName, DialogClassName, StringComparison.Ordinal) &&
                window.OwnerProcessId == window.ProcessId &&
                !window.OwnerEnabled)
                    ? SpatialAnalyzerModalDialogState.ModalDialogOpen
                    : SpatialAnalyzerModalDialogState.NoModalDialog;
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException or NotSupportedException)
        {
            return SpatialAnalyzerModalDialogState.NotObserved;
        }
    }
}
