namespace Briosa.Worker.Control;

public sealed record WorkerReportOutputOptionsValue(
    int OutputType,
    string? ExternalPath,
    WorkerEmbeddedReportFileValue? EmbeddedFile) : WorkerMpValue;
