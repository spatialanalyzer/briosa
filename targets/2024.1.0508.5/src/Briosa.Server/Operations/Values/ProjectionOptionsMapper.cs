using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class ProjectionOptionsMapper
{
    public static WorkerProjectionOptionsValue FromRequest(Api.ProjectionOptions? value) => new(
        value is { HasProjectionType: true } ? value.ProjectionType : "Object To Probe Vectors",
        value is { HasIgnoreEdgeProjections: true, IgnoreEdgeProjections: true },
        value is { HasOverrideTargetOffsets: true, OverrideTargetOffsets: true },
        value is { HasOverrideTargetOffsetsValue: true } ? value.OverrideTargetOffsetsValue : 0d,
        value is { HasAddExtraMaterialThickness: true, AddExtraMaterialThickness: true },
        value is { HasExtraMaterialThicknessValue: true } ? value.ExtraMaterialThicknessValue : 0d);
}
