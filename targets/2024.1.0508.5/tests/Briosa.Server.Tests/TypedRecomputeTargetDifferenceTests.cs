using Briosa.Server.Operations.AnalysisOperations;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRecomputeTargetDifferenceTests
{
    [Fact]
    public void Sa2024RetainsItsThreeBooleanInputsWithoutThe2026CloudRefreshField()
    {
        var requestDescriptor = Api.ReComputeCalculatedItemsRequest.Descriptor;
        Assert.Null(requestDescriptor.FindFieldByName("refresh_filtered_cloud_data"));

        var command = ReComputeCalculatedItemsOperation.CreateCommand(new());
        Assert.Equal(3, command.InputArguments.Count);
        Assert.Equal(["Targets from Shots", "Hidden Points", "Relationships"],
            command.InputArguments.Select(argument => argument.Name));
    }
}
