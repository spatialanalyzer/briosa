namespace Briosa.Worker.Sdk;

internal sealed record SdkBSplineFitOptionsValue(
    bool UseInterpolationFit,
    bool OpenCurve,
    int SortMethod,
    int TerminateMethod,
    int Degree,
    double TerminateLength,
    double TerminateAverageMultiplier,
    int NumberOfControlPoints,
    bool UniqueCheck,
    double UniqueThreshold,
    double Extension,
    bool UseGlobalTessellationOptions,
    double MaximumChordalDeviation,
    double MaximumTrimEdgeAngle);
