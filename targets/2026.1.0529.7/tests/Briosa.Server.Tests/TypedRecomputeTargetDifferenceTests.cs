using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRecomputeTargetDifferenceTests
{
    [Fact]
    public void Sa2026IncludesItsFourthCloudRefreshBooleanInput()
    {
        var field = Api.ReComputeCalculatedItemsRequest.Descriptor
            .FindFieldByName("refresh_filtered_cloud_data");
        Assert.NotNull(field);
        Assert.Equal(4, field!.FieldNumber);

        var defaults = ReComputeCalculatedItemsOperation.CreateCommand(new());
        Assert.Equal(4, defaults.InputArguments.Count);
        Assert.Equal("Refresh Filtered Cloud Data?", defaults.InputArguments[3].Name);
        Assert.Equal("SetBoolArg", defaults.InputArguments[3].SdkBinding);
        Assert.False(defaults.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);

        var enabled = ReComputeCalculatedItemsOperation.CreateCommand(new() { RefreshFilteredCloudData = true });
        Assert.True(enabled.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
    }
}
