using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class UncertaintyCovarianceMatrixMapper
{
    public static IReadOnlyList<WorkerDoubleArrayValue> RequiredRows(
        Api.UncertaintyCovarianceMatrix? covarianceMatrix,
        string fieldName)
    {
        if (covarianceMatrix is null)
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(covarianceMatrix));

        return
        [
            RequiredRow(covarianceMatrix.Row1, fieldName, "row_1"),
            RequiredRow(covarianceMatrix.Row2, fieldName, "row_2"),
            RequiredRow(covarianceMatrix.Row3, fieldName, "row_3"),
            RequiredRow(covarianceMatrix.Row4, fieldName, "row_4"),
            RequiredRow(covarianceMatrix.Row5, fieldName, "row_5"),
            RequiredRow(covarianceMatrix.Row6, fieldName, "row_6")
        ];
    }

    public static Api.UncertaintyCovarianceMatrix FromOutputRows(IReadOnlyList<WorkerMpOutputValue> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count != 6)
            throw new InvalidOperationException("Worker output count does not match the covariance matrix contract.");

        return new()
        {
            Row1 = ToProtocol(rows[0]),
            Row2 = ToProtocol(rows[1]),
            Row3 = ToProtocol(rows[2]),
            Row4 = ToProtocol(rows[3]),
            Row5 = ToProtocol(rows[4]),
            Row6 = ToProtocol(rows[5])
        };
    }

    private static WorkerDoubleArrayValue RequiredRow(
        Api.DoubleVector6? row,
        string fieldName,
        string rowName)
    {
        if (row is null || row.Values.Count == 0)
            throw new ArgumentException($"Request field '{fieldName}.{rowName}' is required.", nameof(row));

        return new(row.Values.ToArray());
    }

    private static Api.DoubleVector6 ToProtocol(WorkerMpOutputValue output)
    {
        var row = new Api.DoubleVector6();
        row.Values.AddRange(output.RequireValue<WorkerDoubleArrayValue>().Values);
        return row;
    }
}
