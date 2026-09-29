using Briosa.Server.Operations;
using Briosa.Server.Operations.UtilityOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedUtilitySettingsOperationTests
{
    private static readonly string[] Ids =
    [
        "utility_operations.get_active_language", "utility_operations.get_angular_representation",
        "utility_operations.set_active_custom_language", "utility_operations.set_active_units",
        "utility_operations.set_angular_representation", "utility_operations.set_auto_event_creation",
        "utility_operations.set_automatic_backup_state", "utility_operations.set_automatic_relationship_construction_state",
        "utility_operations.set_decimal_digits_for_display", "utility_operations.set_interaction_mode",
        "utility_operations.set_logging_state", "utility_operations.set_notification_cancel_override",
        "utility_operations.set_user_interface_profile", "utility_operations.set_wild_card_asterisk_mode",
        "utility_operations.set_working_frame", "utility_operations.status_dialog"
    ];

    [Fact]
    public void SettingsOperationsAreRegisteredAndRemovedFromCatalog()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }

        Assert.Equal(SpatialAnalyzerApi.TargetVersion.StartsWith("2024", StringComparison.Ordinal)
            ? "Set WildCard Asterisk Mode" : "Set Wild Card Asterisk Mode",
            SetWildCardAsteriskModeOperation.Descriptor.MpStep);
    }

    [Fact]
    public void MappingsPreserveDefaultsRequiredInputsAndOutputTypes()
    {
        var language = GetActiveLanguageOperation.CreateCommand(new());
        Assert.Empty(language.InputArguments);
        Assert.Equal(["GetFilePathArg", "GetBoolArg"], language.OutputArguments.Select(output => output.SdkBinding));
        var languageResult = GetActiveLanguageOperation.CreateResult(Success(
        [
            new WorkerRetrievedOutput("Language File Name", WorkerMpValueKind.FileReference,
                new WorkerFileReferenceValue("custom.lng", true)),
            new WorkerRetrievedOutput("Custom Language?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true))
        ]));
        Assert.Equal("custom.lng", languageResult.LanguageFileName.Path);
        Assert.True(languageResult.LanguageFileName.EmbeddedFile);
        Assert.True(languageResult.CustomLanguage);

        var angular = GetAngularRepresentationOperation.CreateCommand(new());
        Assert.Equal("GetBoolArg", Assert.Single(angular.OutputArguments).SdkBinding);
        Assert.False(SetAngularRepresentationOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);
        Assert.True(GetAngularRepresentationOperation.CreateResult(Success(
            [new WorkerRetrievedOutput("0-360, (FALSE = +/-180)", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(true))])).Value0360);

        var languageSet = SetActiveCustomLanguageOperation.CreateCommand(new()
            { LanguageFileName = new Api.FileReference { Path = "custom.lng" } });
        Assert.Equal("SetFilePathArg", languageSet.InputArguments[0].SdkBinding);
        var font = languageSet.InputArguments[1].RequireValue<WorkerFontValue>();
        Assert.Equal("MS Shell Dlg", font.FontName);
        Assert.Equal((byte)8, font.Size);
        Assert.Throws<ArgumentException>(() => SetActiveCustomLanguageOperation.CreateCommand(new()));

        var units = SetActiveUnitsOperation.CreateCommand(new());
        Assert.Equal(WorkerDistanceUnitValue.Inches,
            units.InputArguments[0].RequireValue<WorkerDistanceUnitChoice>().Value);
        Assert.False(units.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(16d, units.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.True(units.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerTemperatureUnitValue.Fahrenheit,
            units.InputArguments[4].RequireValue<WorkerTemperatureUnitChoice>().Value);
        Assert.Equal(WorkerAngularUnitValue.Degrees,
            units.InputArguments[5].RequireValue<WorkerAngularUnitChoice>().Value);

        var backup = SetAutomaticBackupStateOperation.CreateCommand(new());
        Assert.All(backup.InputArguments, argument => Assert.True(argument.RequireValue<WorkerBooleanValue>().Value));
        Assert.False(SetAutoEventCreationOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);
        Assert.False(SetAutomaticRelationshipConstructionStateOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal([4, 4, 6, 6, 3], SetDecimalDigitsForDisplayOperation.CreateCommand(new()).InputArguments
            .Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));
        Assert.False(SetLoggingStateOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);
        Assert.True(SetNotificationCancelOverrideOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);
        Assert.True(SetWildCardAsteriskModeOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);

        var interaction = SetInteractionModeOperation.CreateCommand(new()
        {
            SaInteractionMode = Api.SaInteractionMode.Automatic,
            MeasurementPlanInteractionMode = Api.MpInteractionMode.NeverHalt,
            MeasurementPlanDialogInteractionMode = Api.MpDialogInteractionMode.AllowApplicationInteraction
        });
        Assert.Equal(WorkerSaInteractionModeValue.Automatic,
            interaction.InputArguments[0].RequireValue<WorkerChoiceValue<WorkerSaInteractionModeValue>>().Value);
        Assert.Equal(WorkerMpInteractionModeValue.NeverHalt,
            interaction.InputArguments[1].RequireValue<WorkerChoiceValue<WorkerMpInteractionModeValue>>().Value);
        Assert.Equal(WorkerMpDialogInteractionModeValue.AllowApplicationInteraction,
            interaction.InputArguments[2].RequireValue<WorkerChoiceValue<WorkerMpDialogInteractionModeValue>>().Value);
        Assert.Throws<ArgumentException>(() => SetInteractionModeOperation.CreateCommand(new()));

        Assert.Throws<ArgumentException>(() => SetUserInterfaceProfileOperation.CreateCommand(new()));
        var profile = SetUserInterfaceProfileOperation.CreateCommand(new()
        {
            ProfileFileName = new Api.FileReference { Path = "profile.sa" }
        });
        Assert.Equal("Default", profile.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("profile.sa", profile.InputArguments[1].RequireValue<WorkerFileReferenceValue>().Path);
        Assert.Throws<ArgumentException>(() => SetWorkingFrameOperation.CreateCommand(new()));
        var status = StatusDialogOperation.CreateCommand(new());
        Assert.Equal(["", "", "0", "0", "True", "False"], status.InputArguments.Select(argument => argument.Value switch
        {
            WorkerTextValue value => value.Value,
            WorkerIntegerValue value => value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            WorkerBooleanValue value => value.Value.ToString(),
            _ => throw new InvalidOperationException("Unexpected status dialog value.")
        }));
    }

    [Fact]
    public async Task GeneratedClientRoutesSettingsThroughTheTypedWorkerMapping()
    {
        var worker = new SettingsWorker();
        var grpcHost = await GrpcTestHost.StartAsync<UtilityOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.UtilityOperations.UtilityOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var language = await client.GetActiveLanguageAsync(new(), options);
        var units = await client.SetActiveUnitsAsync(new(), options);
        var status = await client.StatusDialogAsync(new(), options);

        Assert.Equal("custom.lng", language.LanguageFileName.Path);
        Assert.True(language.CustomLanguage);
        Assert.Equal(Api.MpExecutionState.Succeeded, units.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, status.Execution.State);
        Assert.Equal(
        [
            "utility_operations.get_active_language", "utility_operations.set_active_units",
            "utility_operations.status_dialog"
        ], worker.Commands.Select(command => command.OperationId));
    }

    private static SuccessfulOperationExecution Success(IReadOnlyList<WorkerMpOutputValue> outputs)
    {
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
        return new(execution, new Api.MpExecutionDetails());
    }

    private sealed class SettingsWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == "utility_operations.get_active_language"
                ?
                [
                    new WorkerRetrievedOutput("Language File Name", WorkerMpValueKind.FileReference,
                        new WorkerFileReferenceValue("custom.lng", true)),
                    new WorkerRetrievedOutput("Custom Language?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true))
                ]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
