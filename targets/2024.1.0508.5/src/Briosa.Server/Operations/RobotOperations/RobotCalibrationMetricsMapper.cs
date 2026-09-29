using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class RobotCalibrationMetricsMapper
{
    public static Api.RobotCalibrationMetrics ToProtocol(IReadOnlyList<WorkerMpOutputValue> outputs) => new()
    {
        XyzMax = Double(outputs[0]),
        XyzAverage = Double(outputs[1]),
        XyzRms = Double(outputs[2]),
        OrientMax = Double(outputs[3]),
        OrientAverage = Double(outputs[4]),
        OrientRms = Double(outputs[5]),
        Robustness = Double(outputs[6])
    };

    private static double Double(WorkerMpOutputValue value) => value.RequireValue<WorkerDoubleValue>().Value;
}