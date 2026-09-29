using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class BSplineFitOptionsMapper
{
    public static WorkerBSplineFitOptionsValue Map(Api.BSplineFitOptions? options)
    {
        options ??= new Api.BSplineFitOptions();
        return new(
            options.HasUseInterpolationForFit ? options.UseInterpolationForFit : true,
            options.HasOpenCurve ? options.OpenCurve : true,
            (int)(options.HasSortMethod ? options.SortMethod : Api.BSplinePointSortMode.UseSelectionOrder) - 1,
            options.HasSpanAnyGap ? (options.SpanAnyGap ? 0 : 1) : 0,
            options.HasDegreeOfCurve ? options.DegreeOfCurve : 3,
            options.HasTerminationGapLength ? options.TerminationGapLength : 0,
            options.HasTerminationAverageMultiplier ? options.TerminationAverageMultiplier : 10,
            options.HasNumberOfControlPoints ? options.NumberOfControlPoints : 8,
            options.HasIgnoreProximatePoints && options.IgnoreProximatePoints,
            options.HasProximatePointThreshold ? options.ProximatePointThreshold : 0,
            options.HasExtension ? options.Extension : 0,
            !options.HasUseGlobalTessellationOptions || options.UseGlobalTessellationOptions,
            options.HasMaximumChordalDeviation ? options.MaximumChordalDeviation : 0.05,
            options.HasMaximumTrimEdgeAngle ? options.MaximumTrimEdgeAngle : 15);
    }
}
