using System.Collections.Immutable;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class RelationshipOperationValueMapper
{
    public static WorkerCollectionItemNameListValue RequiredRelationships(
        IReadOnlyList<Api.CollectionItemName> values, string field)
    {
        if (values.Count == 0)
            throw RequestFieldViolation.NotEmpty(field);

        return new(values.Select(value => CollectionItemNameMapper.Required(
            value, field, WorkerItemTypeValue.Relationship)).ToImmutableArray());
    }

    public static WorkerAutoFilterProximitySettingsValue RequiredProximity(
        Api.FilterProximitySettings? value, string field)
    {
        if (value is null)
            throw RequestFieldViolation.Required(field);

        return new(value.SurfaceInclusionProximity, value.EdgeExclusionProximity,
            value.PlanarInclusionProximity, value.PlanarExclusionProximity,
            value.RadialInclusionProximity, value.GeometryExtractionTolerance,
            OffsetMode(value.SurfaceProximityMode, RequestFieldViolation.Path(field, "surface_proximity_mode")),
            OffsetMode(value.PlanarProximityMode, RequestFieldViolation.Path(field, "planar_proximity_mode")),
            OffsetMode(value.RadialProximityMode, RequestFieldViolation.Path(field, "radial_proximity_mode")),
            value.ProjectToPlane, value.AssertPlaneBoundaries);
    }

    public static WorkerMpValue RequiredOffsetDirection(Api.OffsetDirectionType? value, string field)
    {
        if (value is null)
            throw RequestFieldViolation.Required(field);
        if (value == Api.OffsetDirectionType.Unspecified || !Enum.IsDefined(value.Value))
            throw RequestFieldViolation.SupportedChoice(field);
        return WorkerChoiceFactory.FromOrdinal(WorkerMpValueKind.OffsetDirectionType, (int)value.Value - 1);
    }

    public static WorkerFitDegreeOfFreedomOptionsValue RequiredMotion(Api.FitDofOptions? value, string field)
    {
        if (value is null)
            throw RequestFieldViolation.Required(field);

        return new(value.AllowX, value.AllowY, value.AllowZ,
            value.AllowRx, value.AllowRy, value.AllowRz, value.RotateAboutCentroid);
    }

    public static WorkerTextValue Solver(Api.SolverMode? value, string fieldName)
    {
        var mode = value ?? Api.SolverMode.GaussNewton;
        return new(mode switch
        {
            Api.SolverMode.GaussNewton => "Gauss-Newton",
            Api.SolverMode.LevenbergMarquardt => "Levenberg-Marquardt",
            Api.SolverMode.GaussNewtonWithGradientSearch => "Gauss-Newton /w Gradient Search",
            Api.SolverMode.DirectSearch => "Direct Search",
            _ => throw RequestFieldViolation.SupportedChoice(fieldName)
        });
    }

    private static int OffsetMode(Api.OffsetDirectionType value, string field)
    {
        if (value == Api.OffsetDirectionType.Unspecified || !Enum.IsDefined(value))
            throw RequestFieldViolation.SupportedChoice(field);
        return (int)value - 1;
    }
}
