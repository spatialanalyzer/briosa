using Google.Protobuf.Reflection;
using Api = global::Briosa;

namespace Briosa.Protocol.Tests;

public sealed partial class ProtocolSchemaTests
{
    [Fact]
    public void GetActiveCollectionNameRetainsMpCompatibleIdentityAndPresence()
    {
        Assert.Equal(
            "briosa.ConstructionOperations",
            Api.ConstructionOperations.Descriptor.FullName);
        var method = Assert.Single(
            Api.ConstructionOperations.Descriptor.Methods,
            candidate => candidate.Name == "GetActiveCollectionName");
        Assert.Equal(Api.GetActiveCollectionNameRequest.Descriptor, method.InputType);
        Assert.Equal(Api.GetActiveCollectionNameResult.Descriptor, method.OutputType);
        Assert.Empty(
            Api.GetActiveCollectionNameRequest.Descriptor.Fields.InFieldNumberOrder());

        var activeCollection = Api.GetActiveCollectionNameResult.Descriptor
            .FindFieldByName("currently_active_collection_name");
        Assert.Equal(FieldType.String, activeCollection.FieldType);
        Assert.True(activeCollection.HasPresence);
    }

    [Fact]
    public void GetActiveUnitsRetainsExactOutputOrderAndPresence()
    {
        Assert.Equal("briosa.UtilityOperations", Api.UtilityOperations.Descriptor.FullName);
        var method = Assert.Single(
            Api.UtilityOperations.Descriptor.Methods,
            candidate => candidate.Name == "GetActiveUnits");
        Assert.Equal(Api.GetActiveUnitsRequest.Descriptor, method.InputType);
        Assert.Equal(Api.GetActiveUnitsResult.Descriptor, method.OutputType);
        Assert.Empty(Api.GetActiveUnitsRequest.Descriptor.Fields.InFieldNumberOrder());

        var fields = Api.GetActiveUnitsResult.Descriptor.Fields.InFieldNumberOrder();
        Assert.Equal(
            ["length", "angular", "temperature", "execution"],
            fields.Select(field => field.Name));
        Assert.All(fields.Take(3), field =>
        {
            Assert.Equal(FieldType.String, field.FieldType);
            Assert.True(field.HasPresence);
        });
    }

    [Fact]
    public void GetWorkingFramePropertiesUsesTheTypedCollectionObjectDomain()
    {
        var method = Assert.Single(
            Api.UtilityOperations.Descriptor.Methods,
            candidate => candidate.Name == "GetWorkingFrameProperties");
        Assert.Equal(Api.GetWorkingFramePropertiesRequest.Descriptor, method.InputType);
        Assert.Equal(Api.GetWorkingFramePropertiesResult.Descriptor, method.OutputType);
        Assert.Empty(
            Api.GetWorkingFramePropertiesRequest.Descriptor.Fields.InFieldNumberOrder());

        var resultFields = Api.GetWorkingFramePropertiesResult.Descriptor.Fields
            .InFieldNumberOrder();
        Assert.Equal(
            ["frame_name", "collection_name", "working_frame", "execution"],
            resultFields.Select(field => field.Name));
        Assert.Equal(FieldType.String, resultFields[0].FieldType);
        Assert.True(resultFields[0].HasPresence);
        Assert.Equal(FieldType.String, resultFields[1].FieldType);
        Assert.True(resultFields[1].HasPresence);
        Assert.Equal(FieldType.Message, resultFields[2].FieldType);
        Assert.Equal(Api.CollectionObjectName.Descriptor, resultFields[2].MessageType);

        var objectFields = Api.CollectionObjectName.Descriptor.Fields
            .InFieldNumberOrder();
        Assert.Equal(
            ["collection_name", "object_name", "object_type"],
            objectFields.Select(field => field.Name));
        Assert.True(objectFields[0].HasPresence);
        Assert.True(objectFields[1].HasPresence);
        Assert.Equal(FieldType.Enum, objectFields[2].FieldType);
        Assert.Equal("briosa.ObjectType", objectFields[2].EnumType.FullName);
        Assert.Equal(27, objectFields[2].EnumType.Values.Count);
    }
}
