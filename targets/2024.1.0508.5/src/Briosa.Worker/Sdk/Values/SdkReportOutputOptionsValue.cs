namespace Briosa.Worker.Sdk;

internal sealed record SdkReportOutputOptionsValue(
    SdkReportOutputTypeValue OutputType,
    string? ExternalPath,
    SdkEmbeddedReportFileValue? EmbeddedFile);
