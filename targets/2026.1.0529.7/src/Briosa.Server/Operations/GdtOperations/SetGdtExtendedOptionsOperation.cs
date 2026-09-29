using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class SetGdtExtendedOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.set_gdt_extended_options", "Set GD&T Extended Options",
        "briosa.GdtOperations", "SetGdtExtendedOptions", "/briosa.GdtOperations/SetGdtExtendedOptions",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetGdtExtendedOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Use Extended Options", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasUseExtendedOptions ? request.UseExtendedOptions : true), "SetBoolArg"),
            new("Circle Extended Options", WorkerMpValueKind.Text,
                MapOption(request.HasCircleExtendedOptions, request.CircleExtendedOptions, ExtendedOptionFamily.Circle, "circle_extended_options"), "SetStringArg"),
            new("Cone Extended Options", WorkerMpValueKind.Text,
                MapOption(request.HasConeExtendedOptions, request.ConeExtendedOptions, ExtendedOptionFamily.Cone, "cone_extended_options"), "SetStringArg"),
            new("Cylinder Extended Options", WorkerMpValueKind.Text,
                MapOption(request.HasCylinderExtendedOptions, request.CylinderExtendedOptions, ExtendedOptionFamily.Standard, "cylinder_extended_options"), "SetStringArg"),
            new("Ellipse Extended Options", WorkerMpValueKind.Text,
                MapOption(request.HasEllipseExtendedOptions, request.EllipseExtendedOptions, ExtendedOptionFamily.Standard, "ellipse_extended_options"), "SetStringArg"),
            new("Line Extended Options", WorkerMpValueKind.Text,
                MapOption(request.HasLineExtendedOptions, request.LineExtendedOptions, ExtendedOptionFamily.Line, "line_extended_options"), "SetStringArg"),
            new("Open Slot Extended Options", WorkerMpValueKind.Text,
                MapOption(request.HasOpenSlotExtendedOptions, request.OpenSlotExtendedOptions, ExtendedOptionFamily.OpenSlot, "open_slot_extended_options"), "SetStringArg"),
            new("Plane Extended Options", WorkerMpValueKind.Text,
                MapOption(request.HasPlaneExtendedOptions, request.PlaneExtendedOptions, ExtendedOptionFamily.Plane, "plane_extended_options"), "SetStringArg"),
            new("Slot Extended Options", WorkerMpValueKind.Text,
                MapOption(request.HasSlotExtendedOptions, request.SlotExtendedOptions, ExtendedOptionFamily.Standard, "slot_extended_options"), "SetStringArg"),
            new("Sphere Extended Options", WorkerMpValueKind.Text,
                MapOption(request.HasSphereExtendedOptions, request.SphereExtendedOptions, ExtendedOptionFamily.Standard, "sphere_extended_options"), "SetStringArg")
        ], []);
    }

    public static Api.SetGdtExtendedOptionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };

    private static WorkerTextValue MapOption(
        bool hasValue,
        Api.GdtExtendedEvaluationMethod value,
        ExtendedOptionFamily family,
        string argumentName)
    {
        if (!hasValue)
            return new("Least Squares");

        var sdkValue = ToSdkValue(value, argumentName);
        if (!IsSupportedFor(value, family))
            throw new ArgumentOutOfRangeException(argumentName, value,
                $"The selected GD&T evaluation method is not supported for '{argumentName}'.");
        return new(sdkValue);
    }

    private static string ToSdkValue(Api.GdtExtendedEvaluationMethod value, string argumentName) => value switch
    {
        Api.GdtExtendedEvaluationMethod.LeastSquares => "Least Squares",
        Api.GdtExtendedEvaluationMethod.HighPoint => "High Point",
        Api.GdtExtendedEvaluationMethod.MinimumSeparation => "Minimum Separation",
        Api.GdtExtendedEvaluationMethod.LeastSquaresHighPoint => "Least Squares High Point",
        Api.GdtExtendedEvaluationMethod.LeastSquares3D => "Least Squares 3D",
        Api.GdtExtendedEvaluationMethod.LeastSquaresHighPoint1StdDev => "Least Squares High Point 1 STD DEV",
        Api.GdtExtendedEvaluationMethod.LeastSquaresHighPoint2StdDev => "Least Squares High Point 2 STD DEV",
        Api.GdtExtendedEvaluationMethod.LeastSquaresHighPointHalfway => "Least Squares High Point Halfway",
        Api.GdtExtendedEvaluationMethod.MinimumSeparationHighPoint => "Minimum Separation High Point",
        Api.GdtExtendedEvaluationMethod.EqualizedHighPoint => "Equalized High Point",
        Api.GdtExtendedEvaluationMethod.EqualizedLsqHighPoint => "Equalized LSQ High Point",
        _ => throw new ArgumentOutOfRangeException(argumentName, value,
            "The GD&T evaluation method is not supported by this SpatialAnalyzer target.")
    };

    private static bool IsSupportedFor(Api.GdtExtendedEvaluationMethod value, ExtendedOptionFamily family) => family switch
    {
        ExtendedOptionFamily.Circle or ExtendedOptionFamily.Plane => true,
        ExtendedOptionFamily.Cone => value is
            Api.GdtExtendedEvaluationMethod.LeastSquares or
            Api.GdtExtendedEvaluationMethod.MinimumSeparation or
            Api.GdtExtendedEvaluationMethod.LeastSquaresHighPoint or
            Api.GdtExtendedEvaluationMethod.LeastSquaresHighPoint1StdDev or
            Api.GdtExtendedEvaluationMethod.LeastSquaresHighPoint2StdDev or
            Api.GdtExtendedEvaluationMethod.LeastSquaresHighPointHalfway,
        ExtendedOptionFamily.Standard => value is
            Api.GdtExtendedEvaluationMethod.LeastSquares or
            Api.GdtExtendedEvaluationMethod.HighPoint or
            Api.GdtExtendedEvaluationMethod.MinimumSeparation or
            Api.GdtExtendedEvaluationMethod.LeastSquaresHighPoint or
            Api.GdtExtendedEvaluationMethod.LeastSquaresHighPoint1StdDev or
            Api.GdtExtendedEvaluationMethod.LeastSquaresHighPoint2StdDev or
            Api.GdtExtendedEvaluationMethod.LeastSquaresHighPointHalfway,
        ExtendedOptionFamily.Line => value is
            Api.GdtExtendedEvaluationMethod.LeastSquares or
            Api.GdtExtendedEvaluationMethod.MinimumSeparation,
        ExtendedOptionFamily.OpenSlot => value is
            Api.GdtExtendedEvaluationMethod.LeastSquares or
            Api.GdtExtendedEvaluationMethod.HighPoint or
            Api.GdtExtendedEvaluationMethod.LeastSquaresHighPoint or
            Api.GdtExtendedEvaluationMethod.LeastSquaresHighPoint1StdDev or
            Api.GdtExtendedEvaluationMethod.LeastSquaresHighPoint2StdDev or
            Api.GdtExtendedEvaluationMethod.LeastSquaresHighPointHalfway,
        _ => false
    };

    private enum ExtendedOptionFamily
    {
        Circle,
        Cone,
        Standard,
        Line,
        OpenSlot,
        Plane
    }
}
