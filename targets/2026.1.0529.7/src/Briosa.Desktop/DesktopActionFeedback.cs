using Google.Protobuf;

namespace Briosa.Desktop;

public static class DesktopActionFeedback
{
    public static string Rejected(string? diagnostic) => diagnostic switch
    {
        "runtime-identity-not-ready" => "Attachment succeeded; commands remain blocked by version checks. Review Connection setup. Reconnecting cannot supply missing evidence.",
        "activated-sdk-version-mismatch" => "Connection was blocked because Windows activated a different SDK release. Stop the SDK and register the matching SDK using the vendor's installation tools.",
        "spatial-analyzer-application-not-found" => "The matching SpatialAnalyzer application was not found. Open it, then connect again.",
        "sdk-client-activation-failed" => "Windows could not activate the SDK. Check the matching SpatialAnalyzer installation and SDK registration before recovery.",
        "sdk-reconnect-not-required" => "The SDK is already ready. Reconnection is unnecessary.",
        _ => "The server rejected the action. Review its current state. Diagnostic: " + SafeText.Code(diagnostic)
    };
}
