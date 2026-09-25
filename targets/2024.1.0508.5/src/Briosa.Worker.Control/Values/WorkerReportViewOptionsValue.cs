namespace Briosa.Worker.Control;

public sealed record WorkerReportViewOptionsValue(
    int ViewType,
    string CollectionName,
    string CalloutName);
