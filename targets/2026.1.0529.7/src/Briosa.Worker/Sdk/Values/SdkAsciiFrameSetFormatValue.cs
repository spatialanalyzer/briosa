namespace Briosa.Worker.Sdk;

internal enum SdkAsciiFrameSetFormatValue
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
