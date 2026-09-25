namespace Briosa.Worker.Control;

public sealed record WorkerFontValue(
    string FontName,
    byte Size,
    WorkerRgbColorValue Color);
