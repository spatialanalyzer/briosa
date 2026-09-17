using Briosa.Worker.Control;

namespace Briosa.Worker.Tests;

public sealed class WorkerMpCommandTests
{
    [Fact]
    public void CommandFreezesCallerOwnedArgumentCollections()
    {
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Enabled", WorkerMpValueKind.Logical, BooleanValue: false)
        };
        var outputs = new List<WorkerMpOutputArgument>
        {
            new("Directory", WorkerMpValueKind.Text, "GetStringArg")
        };

        var command = new WorkerMpCommand("operation", "Step", inputs, outputs);
        inputs.Clear();
        outputs.Clear();

        Assert.Single(command.InputArguments);
        Assert.Single(command.OutputArguments);
        Assert.False(command.InputArguments is WorkerMpInputArgument[]);
        Assert.False(command.OutputArguments is WorkerMpOutputArgument[]);
    }
}
