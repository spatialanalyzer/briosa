using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedConstructionReferenceUtilityTests
{
    private static readonly string[] Ids =
    [
        "construction_operations.add_collection_instruments_to_ref_list_wildcard_selection",
        "construction_operations.construct_surfaces_by_dissecting_surfaces_from_ref_list",
        "construction_operations.construct_vector_group_from_vector_name_ref_list",
        "construction_operations.get_collection_instrument_ref_list_variable",
        "construction_operations.make_callout_view_ref_list_wildcard_selection",
        "construction_operations.make_collection_instrument_id_runtime_select",
        "construction_operations.make_collection_instrument_ref_list_runtime_select",
        "construction_operations.make_collection_vector_group_name_ref_list_runtime_select",
        "construction_operations.make_event_ref_list_wildcard_selection",
        "construction_operations.make_picture_name_ref_list_runtime_select",
        "construction_operations.make_relationship_ref_list_runtime_select",
        "construction_operations.make_relationship_ref_list_wildcard_selection",
        "construction_operations.make_report_ref_list_from_collection",
        "construction_operations.make_report_ref_list_runtime_select",
        "construction_operations.make_system_string",
        "construction_operations.make_vector_name_ref_list_from_vector_group",
        "construction_operations.make_vector_name_ref_list_runtime_select",
        "construction_operations.make_vector_names_unique_in_vector_group",
        "construction_operations.set_collection_instrument_ref_list_variable"
    ];

    [Fact]
    public void OperationsAreRegisteredAndAbsentFromInterpretedCatalog()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, descriptor => descriptor.OperationId == id);
        }
    }

    [Fact]
    public void InstrumentListsPreserveBindingOrderAndWildcardDefaults()
    {
        Assert.Throws<ArgumentException>(() => SetCollectionInstrumentRefListVariableOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => AddCollectionInstrumentsToRefListWildcardSelectionOperation.CreateCommand(new()));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "C", InstrumentId = 7 };
        var set = SetCollectionInstrumentRefListVariableOperation.CreateCommand(new()
            { Name = "instruments", Value = { instrument } });
        Assert.Equal(["SetStringArg", "SetColInstIdRefListArg"], set.InputArguments.Select(input => input.SdkBinding));
        Assert.Equal(7, Assert.Single(set.InputArguments[1]
            .RequireValue<WorkerCollectionInstrumentIdListValue>().Values).InstrumentId);

        var add = AddCollectionInstrumentsToRefListWildcardSelectionOperation.CreateCommand(new()
            { CollectionInstrumentRefList = { instrument } });
        Assert.Equal(["SetColInstIdRefListArg", "SetStringArg", "SetStringArg"],
            add.InputArguments.Select(input => input.SdkBinding));
        Assert.Equal(["*", "*"], add.InputArguments.Skip(1)
            .Select(input => input.RequireValue<WorkerTextValue>().Value));
        Assert.Equal("GetColInstIdRefListArg", Assert.Single(add.OutputArguments).SdkBinding);

        var get = GetCollectionInstrumentRefListVariableOperation.CreateCommand(new() { Name = "instruments" });
        Assert.Equal("SetStringArg", Assert.Single(get.InputArguments).SdkBinding);
        Assert.Equal("GetColInstIdRefListArg", Assert.Single(get.OutputArguments).SdkBinding);
        Assert.Equal("GetColInstIdArg", Assert.Single(MakeCollectionInstrumentIdRuntimeSelectOperation
            .CreateCommand(new()).OutputArguments).SdkBinding);
        Assert.Equal("GetColInstIdRefListArg", Assert.Single(MakeCollectionInstrumentRefListRuntimeSelectOperation
            .CreateCommand(new()).OutputArguments).SdkBinding);
    }

    [Fact]
    public void ReferenceSelectorsPreserveExactBindingsAndDefaults()
    {
        var callouts = MakeCalloutViewRefListWildcardSelectionOperation.CreateCommand(new());
        Assert.Equal(["*", "*"], callouts.InputArguments
            .Select(input => input.RequireValue<WorkerTextValue>().Value));
        Assert.Equal("GetCollectionObjectNameRefListArg", Assert.Single(callouts.OutputArguments).SdkBinding);
        var events = MakeEventRefListWildcardSelectionOperation.CreateCommand(new());
        Assert.Equal(["*", "*"], events.InputArguments
            .Select(input => input.RequireValue<WorkerTextValue>().Value));
        var relationships = MakeRelationshipRefListWildcardSelectionOperation.CreateCommand(new());
        Assert.Equal(["*", "*"], relationships.InputArguments
            .Select(input => input.RequireValue<WorkerTextValue>().Value));
        Assert.Equal("GetCollectionObjectNameRefListArg", Assert.Single(relationships.OutputArguments).SdkBinding);
        Assert.Equal("GetCollectionObjectNameRefListArg", Assert.Single(MakeRelationshipRefListRuntimeSelectOperation
            .CreateCommand(new()).OutputArguments).SdkBinding);
        Assert.Equal("GetCollectionObjectNameRefListArg", Assert.Single(MakePictureNameRefListRuntimeSelectOperation
            .CreateCommand(new()).OutputArguments).SdkBinding);
        Assert.Equal("GetCollectionObjectNameRefListArg", Assert.Single(MakeReportRefListRuntimeSelectOperation
            .CreateCommand(new()).OutputArguments).SdkBinding);
        Assert.Throws<ArgumentException>(() => MakeReportRefListFromCollectionOperation.CreateCommand(new()));
        Assert.Equal("SetCollectionNameArg", Assert.Single(MakeReportRefListFromCollectionOperation.CreateCommand(new()
            { CollectionName = new Api.CollectionName { Name = "C" } }).InputArguments).SdkBinding);
        Assert.Equal("GetCollectionVectorGroupNameRefListArg", Assert.Single(
            MakeCollectionVectorGroupNameRefListRuntimeSelectOperation.CreateCommand(new()).OutputArguments).SdkBinding);
    }

    [Fact]
    public void VectorAndSystemStringCommandsPreservePresenceAndType()
    {
        Assert.Throws<ArgumentException>(() => ConstructSurfacesByDissectingSurfacesFromRefListOperation.CreateCommand(new()));
        var surfaces = ConstructSurfacesByDissectingSurfacesFromRefListOperation.CreateCommand(new()
        {
            SurfacesToDissect = { new Api.CollectionObjectName { CollectionName = "C", ObjectName = "S" } }
        });
        Assert.Equal("SetCollectionObjectNameRefListArg", Assert.Single(surfaces.InputArguments).SdkBinding);
        Assert.Equal("GetCollectionObjectNameRefListArg", Assert.Single(surfaces.OutputArguments).SdkBinding);
        Assert.Throws<ArgumentException>(() => ConstructVectorGroupFromVectorNameRefListOperation.CreateCommand(new()));
        var vectors = ConstructVectorGroupFromVectorNameRefListOperation.CreateCommand(new()
        {
            VectorNameList = { new Api.VectorName { CollectionName = "C", GroupName = "G", Name = "V" } },
            ResultantVectorGroupName = new Api.CollectionVectorGroupName { CollectionName = "C", VectorGroupName = "G2" }
        });
        Assert.Equal(["SetVectorNameRefListArg", "SetColVectorGroupNameArg"],
            vectors.InputArguments.Select(input => input.SdkBinding));
        Assert.Throws<ArgumentException>(() => MakeVectorNameRefListFromVectorGroupOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => MakeVectorNamesUniqueInVectorGroupOperation.CreateCommand(new()));
        Assert.Equal("GetVectorNameRefListArg", Assert.Single(MakeVectorNameRefListFromVectorGroupOperation
            .CreateCommand(new() { VectorGroupName = new Api.CollectionObjectName { ObjectName = "G" } })
            .OutputArguments).SdkBinding);
        Assert.Equal(" Select Vectors (ENTER when done) ", MakeVectorNameRefListRuntimeSelectOperation
            .CreateCommand(new()).InputArguments[0].RequireValue<WorkerTextValue>().Value);

        Assert.Throws<ArgumentException>(() => MakeSystemStringOperation.CreateCommand(new()));
        var system = MakeSystemStringOperation.CreateCommand(new()
            { StringContent = Api.SystemString.SaVersion });
        Assert.Single(system.InputArguments);
        Assert.Equal("SetSystemStringArg", system.InputArguments[0].SdkBinding);
        Assert.Equal(WorkerSystemStringValue.SaVersion, system.InputArguments[0]
            .RequireValue<WorkerChoiceValue<WorkerSystemStringValue>>().Value);
        Assert.Equal(2, MakeSystemStringOperation.CreateCommand(new()
            { StringContent = Api.SystemString.Date, FormatString = "yyyy" }).InputArguments.Count);
    }

    [Fact]
    public void ResultsMapInstrumentItemObjectVectorAndTextFamilies()
    {
        var instrument = new WorkerCollectionInstrumentIdValue("C", 7);
        var instrumentList = new WorkerRetrievedOutput("Value", WorkerMpValueKind.CollectionInstrumentIdList,
            new WorkerCollectionInstrumentIdListValue([instrument]));
        Assert.Equal(7, Assert.Single(GetCollectionInstrumentRefListVariableOperation.CreateResult(
            Success(instrumentList)).Value).InstrumentId);
        Assert.Equal(7, MakeCollectionInstrumentIdRuntimeSelectOperation.CreateResult(Success(
            new WorkerRetrievedOutput("Instrument ID", WorkerMpValueKind.CollectionInstrumentId, instrument)))
            .InstrumentId.InstrumentId);

        var item = new WorkerRetrievedOutput("Resultant Callout View List", WorkerMpValueKind.CollectionItemNameList,
            new WorkerCollectionItemNameListValue(
                [new WorkerCollectionItemNameValue("C", "CV", WorkerItemTypeValue.CalloutView)]));
        Assert.Equal(Api.ItemType.CalloutView, Assert.Single(
            MakeCalloutViewRefListWildcardSelectionOperation.CreateResult(Success(item)).CalloutViews).ItemType);
        var surface = new WorkerRetrievedOutput("Resultant Surfaces List", WorkerMpValueKind.CollectionObjectNameList,
            new WorkerCollectionObjectNameListValue(
                [new WorkerCollectionObjectNameValue("C", "S", WorkerObjectTypeValue.Surface)]));
        Assert.Equal("S", Assert.Single(ConstructSurfacesByDissectingSurfacesFromRefListOperation
            .CreateResult(Success(surface)).ResultantSurfacesList).ObjectName);
        var vector = new WorkerRetrievedOutput("Resultant Vector Name List", WorkerMpValueKind.VectorNameList,
            new WorkerVectorNameListValue([new WorkerVectorNameValue("C", "G", "V")]));
        Assert.Equal("V", Assert.Single(MakeVectorNameRefListRuntimeSelectOperation.CreateResult(
            Success(vector)).ResultantVectorNameList).Name);
        var vectorGroups = new WorkerRetrievedOutput("Resultant Collection Vector Group Name Reference List",
            WorkerMpValueKind.CollectionVectorGroupNameList,
            new WorkerCollectionVectorGroupNameListValue([new WorkerCollectionVectorGroupNameValue("C", "G")]));
        Assert.Equal("G", Assert.Single(MakeCollectionVectorGroupNameRefListRuntimeSelectOperation.CreateResult(
            Success(vectorGroups)).ResultantCollectionVectorGroupNameReferenceList).VectorGroupName);
        Assert.Equal("2026", MakeSystemStringOperation.CreateResult(Success(
            new WorkerRetrievedOutput("Resultant String", WorkerMpValueKind.Text,
                new WorkerTextValue("2026")))).ResultantString);
    }

    [Fact]
    public async Task GeneratedClientUsesTypedInstrumentListRoute()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(host.Channel);
        var result = await client.GetCollectionInstrumentRefListVariableAsync(new() { Name = "tools" },
            new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(7, Assert.Single(result.Value).InstrumentId);
        Assert.Equal("construction_operations.get_collection_instrument_ref_list_variable",
            Assert.Single(worker.Commands).OperationId);
    }

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs =
            [
                new WorkerRetrievedOutput("Value", WorkerMpValueKind.CollectionInstrumentIdList,
                    new WorkerCollectionInstrumentIdListValue([new WorkerCollectionInstrumentIdValue("C", 7)]))
            ];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }

    private static SuccessfulOperationExecution Success(WorkerMpOutputValue output)
    {
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [output], "completed");
        return new(execution, new Api.MpExecutionDetails());
    }
}
