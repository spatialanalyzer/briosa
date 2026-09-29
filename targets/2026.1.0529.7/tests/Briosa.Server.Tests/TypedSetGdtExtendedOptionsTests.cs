using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedSetGdtExtendedOptionsTests
{
    private static readonly string[] InputBindings =
    [
        "SetBoolArg", "SetStringArg", "SetStringArg", "SetStringArg", "SetStringArg",
        "SetStringArg", "SetStringArg", "SetStringArg", "SetStringArg", "SetStringArg"
    ];

    [Fact]
    public void MapsAllOptionsAndDefaultsWithExactBindings()
    {
        var defaultCommand = SetGdtExtendedOptionsOperation.CreateCommand(new());
        Assert.Equal(InputBindings, defaultCommand.InputArguments.Select(argument => argument.SdkBinding));
        Assert.True(defaultCommand.InputArguments[0].RequireValue<WorkerBooleanValue>().Value);
        Assert.All(defaultCommand.InputArguments.Skip(1), argument =>
            Assert.Equal("Least Squares", argument.RequireValue<WorkerTextValue>().Value));

        var command = SetGdtExtendedOptionsOperation.CreateCommand(new()
        {
            UseExtendedOptions = false,
            CircleExtendedOptions = Api.GdtExtendedEvaluationMethod.LeastSquares3D,
            ConeExtendedOptions = Api.GdtExtendedEvaluationMethod.MinimumSeparation,
            CylinderExtendedOptions = Api.GdtExtendedEvaluationMethod.HighPoint,
            EllipseExtendedOptions = Api.GdtExtendedEvaluationMethod.LeastSquaresHighPoint1StdDev,
            LineExtendedOptions = Api.GdtExtendedEvaluationMethod.MinimumSeparation,
            OpenSlotExtendedOptions = Api.GdtExtendedEvaluationMethod.HighPoint,
            PlaneExtendedOptions = Api.GdtExtendedEvaluationMethod.EqualizedLsqHighPoint,
            SlotExtendedOptions = Api.GdtExtendedEvaluationMethod.MinimumSeparation,
            SphereExtendedOptions = Api.GdtExtendedEvaluationMethod.LeastSquaresHighPointHalfway
        });

        Assert.Equal(InputBindings, command.InputArguments.Select(argument => argument.SdkBinding));
        Assert.False(command.InputArguments[0].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(
            [
                "Least Squares 3D", "Minimum Separation", "High Point",
                "Least Squares High Point 1 STD DEV", "Minimum Separation", "High Point",
                "Equalized LSQ High Point", "Minimum Separation", "Least Squares High Point Halfway"
            ],
            command.InputArguments.Skip(1).Select(argument => argument.RequireValue<WorkerTextValue>().Value));

        Assert.Throws<ArgumentOutOfRangeException>(() => SetGdtExtendedOptionsOperation.CreateCommand(new()
        {
            ConeExtendedOptions = Api.GdtExtendedEvaluationMethod.LeastSquares3D
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => SetGdtExtendedOptionsOperation.CreateCommand(new()
        {
            LineExtendedOptions = Api.GdtExtendedEvaluationMethod.HighPoint
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => SetGdtExtendedOptionsOperation.CreateCommand(new()
        {
            PlaneExtendedOptions = (Api.GdtExtendedEvaluationMethod)100
        }));

        var details = new Api.MpExecutionDetails { State = Api.MpExecutionState.Succeeded, MpResultCode = 2 };
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
        var result = SetGdtExtendedOptionsOperation.CreateResult(new SuccessfulOperationExecution(execution, details));
        Assert.Same(details, result.Execution);

        var id = SetGdtExtendedOptionsOperation.Descriptor.OperationId;
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
    }
}
