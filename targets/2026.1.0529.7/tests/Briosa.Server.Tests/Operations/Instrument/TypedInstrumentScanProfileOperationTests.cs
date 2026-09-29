using Briosa.Server.Operations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedInstrumentScanProfileOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.edit_scan_perimeter_profile",
        "instrument_operations.get_inspection_verification_mode",
        "instrument_operations.scan_cad_faces",
        "instrument_operations.scan_within_perimeter",
        "instrument_operations.set_inspection_verification_mode"
    ];

    [Fact]
    public void ScanAndVerificationCommandsPreserveDefaultsAndRegistration()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var perimeter = new Api.CollectionObjectName { CollectionName = "CAD", ObjectName = "Outer" };
        var profile = EditScanPerimeterProfileOperation.CreateCommand(new()
        {
            Instrument = instrument,
            ScanPerimeters = { perimeter },
            ExclusionPerimeters = { new Api.CollectionObjectName { CollectionName = "CAD", ObjectName = "Exclude" } }
        });
        Assert.True(profile.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(profile.InputArguments[6].RequireValue<WorkerBooleanValue>().Value);

        var cadScan = ScanCadFacesOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(string.Empty, cadScan.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.True(cadScan.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(cadScan.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);

        var perimeterScan = ScanWithinPerimeterOperation.CreateCommand(new()
        {
            Instrument = instrument,
            ScanPerimeters = { perimeter },
            ExclusionPerimeters = { new Api.CollectionObjectName { CollectionName = "CAD", ObjectName = "Exclude" } },
            PointGroup = new Api.CollectionObjectName
            {
                CollectionName = "Measured",
                ObjectName = "Scan Points",
                ObjectType = Api.ObjectType.PointGroup
            }
        });
        Assert.True(perimeterScan.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            perimeterScan.InputArguments[4].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.False(SetInspectionVerificationModeOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedScanAndVerificationRoutes()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var perimeter = new Api.CollectionObjectName { CollectionName = "CAD", ObjectName = "Outer" };

        await client.EditScanPerimeterProfileAsync(new()
        {
            Instrument = instrument,
            ScanPerimeters = { perimeter },
            ExclusionPerimeters = { new Api.CollectionObjectName { CollectionName = "CAD", ObjectName = "Exclude" } },
            ParameterSetName = "Default",
            ProfileName = "Scan A",
            ClearProfile = false,
            CreateNewProfile = true
        }, options);
        var verification = await client.GetInspectionVerificationModeAsync(new(), options);
        await client.ScanCadFacesAsync(new()
        {
            Instrument = instrument,
            SurfaceFaces = new Api.SurfaceFaceList { Value = "1,2" },
            ParameterSetName = "Fine",
            EnableExclusions = false,
            WaitForCompletion = false
        }, options);
        await client.ScanWithinPerimeterAsync(new()
        {
            Instrument = instrument,
            ScanPerimeters = { perimeter },
            ExclusionPerimeters = { new Api.CollectionObjectName { CollectionName = "CAD", ObjectName = "Exclude" } },
            PointGroup = new Api.CollectionObjectName
            {
                CollectionName = "Measured",
                ObjectName = "Scan Points",
                ObjectType = Api.ObjectType.PointGroup
            },
            WaitForCompletion = false
        }, options);
        await client.SetInspectionVerificationModeAsync(new() { VerificationEnabled = true }, options);

        Assert.True(verification.VerificationEnabled);
        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.False(worker.Commands[0].InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(worker.Commands[0].InputArguments[6].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("1,2", worker.Commands[2].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.False(worker.Commands[2].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(worker.Commands[3].InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(worker.Commands[4].InputArguments[0].RequireValue<WorkerBooleanValue>().Value);
    }

    private sealed class InstrumentWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == "instrument_operations.get_inspection_verification_mode"
                ? [new WorkerRetrievedOutput("Verification Enabled?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true))]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
