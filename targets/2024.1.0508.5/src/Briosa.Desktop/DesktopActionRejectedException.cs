using System.Diagnostics;

namespace Briosa.Desktop;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "A desktop rejection requires a safe server diagnostic, never arbitrary exception text.")]
public sealed class DesktopActionRejectedException(string diagnostic) : InvalidOperationException("The server rejected the desktop action.")
{
    public string Diagnostic { get; } = SafeText.Code(diagnostic);
}
