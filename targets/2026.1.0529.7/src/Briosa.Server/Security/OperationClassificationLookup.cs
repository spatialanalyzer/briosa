namespace Briosa.Server.Security;

/// <summary>
/// The outcome of <see cref="OperationClassification.Find"/>. The default value
/// is <see cref="OperationClassificationStatus.Unreviewed"/> with no row.
/// </summary>
internal readonly record struct OperationClassificationLookup(
    OperationClassificationStatus Status,
    OperationClassificationRow? Row)
{
    public static OperationClassificationLookup Unreviewed { get; } =
        new(OperationClassificationStatus.Unreviewed, Row: null);

    public bool IsReviewed => Status == OperationClassificationStatus.Reviewed && Row is not null;
}
