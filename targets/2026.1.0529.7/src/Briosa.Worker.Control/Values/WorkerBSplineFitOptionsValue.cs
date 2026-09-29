namespace Briosa.Worker.Control;

public sealed record WorkerBSplineFitOptionsValue(
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
    double MaximumTrimEdgeAngle) : WorkerMpValue;
