namespace Briosa.Worker.Control;

public enum WorkerAsciiFrameSetFormatValue
{
    FrameNameXyzRxRyRzTimestamp,
    FrameNameXyzEulerXyzTimestamp,
    FrameNameXyzEulerZyxTimestamp,
    FrameNameXyzEulerZyzTimestamp,
    FrameNameXyzEulerZxzTimestamp,
    FrameNameTransformationMatrixTimestamp,
    TransformationMatrixTimestamp,
    FrameNameXyzQuaternionTimestamp
}
