namespace Briosa.Worker.Sdk;

internal sealed record SdkReportViewOptionsValue(
    SdkReportViewTypeValue ViewType,
    string CollectionName,
    string CalloutName);
