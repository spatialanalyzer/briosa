using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal sealed class UtilityOperationsService(OperationExecutor executor)
    : Api.UtilityOperations.UtilityOperationsBase
{
    [OperationImplementation("utility_operations.close_all_watch_windows")]
    public override Task<Api.CloseAllWatchWindowsResult> CloseAllWatchWindows(
        Api.CloseAllWatchWindowsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CloseAllWatchWindowsOperation.Descriptor,
            CloseAllWatchWindowsOperation.CreateCommand, CloseAllWatchWindowsOperation.OutputContracts,
            CloseAllWatchWindowsOperation.CreateResult);

    [OperationImplementation("utility_operations.delete_folder")]
    public override Task<Api.DeleteFolderResult> DeleteFolder(
        Api.DeleteFolderRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteFolderOperation.Descriptor,
            DeleteFolderOperation.CreateCommand, DeleteFolderOperation.OutputContracts,
            DeleteFolderOperation.CreateResult);

    [OperationImplementation("utility_operations.delete_items")]
    public override Task<Api.DeleteItemsResult> DeleteItems(
        Api.DeleteItemsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteItemsOperation.Descriptor,
            DeleteItemsOperation.CreateCommand, DeleteItemsOperation.OutputContracts,
            DeleteItemsOperation.CreateResult);

    [OperationImplementation("utility_operations.delete_objects")]
    public override Task<Api.DeleteObjectsResult> DeleteObjects(
        Api.DeleteObjectsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteObjectsOperation.Descriptor,
            DeleteObjectsOperation.CreateCommand, DeleteObjectsOperation.OutputContracts,
            DeleteObjectsOperation.CreateResult);

    [OperationImplementation("utility_operations.get_active_language")]
    public override Task<Api.GetActiveLanguageResult> GetActiveLanguage(
        Api.GetActiveLanguageRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetActiveLanguageOperation.Descriptor,
            GetActiveLanguageOperation.CreateCommand, GetActiveLanguageOperation.OutputContracts,
            GetActiveLanguageOperation.CreateResult);

    [OperationImplementation("utility_operations.get_active_units")]
    public override Task<Api.GetActiveUnitsResult> GetActiveUnits(
        Api.GetActiveUnitsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetActiveUnitsOperation.Descriptor,
            GetActiveUnitsOperation.CreateCommand, GetActiveUnitsOperation.OutputContracts,
            GetActiveUnitsOperation.CreateResult);

    [OperationImplementation("utility_operations.get_angular_representation")]
    public override Task<Api.GetAngularRepresentationResult> GetAngularRepresentation(
        Api.GetAngularRepresentationRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetAngularRepresentationOperation.Descriptor,
            GetAngularRepresentationOperation.CreateCommand, GetAngularRepresentationOperation.OutputContracts,
            GetAngularRepresentationOperation.CreateResult);

    [OperationImplementation("utility_operations.get_collection_notes")]
    public override Task<Api.GetCollectionNotesResult> GetCollectionNotes(
        Api.GetCollectionNotesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCollectionNotesOperation.Descriptor,
            GetCollectionNotesOperation.CreateCommand, GetCollectionNotesOperation.OutputContracts,
            GetCollectionNotesOperation.CreateResult);

    [OperationImplementation("utility_operations.get_folder_collections")]
    public override Task<Api.GetFolderCollectionsResult> GetFolderCollections(
        Api.GetFolderCollectionsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetFolderCollectionsOperation.Descriptor,
            GetFolderCollectionsOperation.CreateCommand, GetFolderCollectionsOperation.OutputContracts,
            GetFolderCollectionsOperation.CreateResult);

    [OperationImplementation("utility_operations.get_folder_notes")]
    public override Task<Api.GetFolderNotesResult> GetFolderNotes(
        Api.GetFolderNotesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetFolderNotesOperation.Descriptor,
            GetFolderNotesOperation.CreateCommand, GetFolderNotesOperation.OutputContracts,
            GetFolderNotesOperation.CreateResult);

    [OperationImplementation("utility_operations.get_folders_by_wildcard")]
    public override Task<Api.GetFoldersByWildcardResult> GetFoldersByWildcard(
        Api.GetFoldersByWildcardRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetFoldersByWildcardOperation.Descriptor,
            GetFoldersByWildcardOperation.CreateCommand, GetFoldersByWildcardOperation.OutputContracts,
            GetFoldersByWildcardOperation.CreateResult);

    [OperationImplementation("utility_operations.get_object_notes")]
    public override Task<Api.GetObjectNotesResult> GetObjectNotes(
        Api.GetObjectNotesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetObjectNotesOperation.Descriptor,
            GetObjectNotesOperation.CreateCommand, GetObjectNotesOperation.OutputContracts,
            GetObjectNotesOperation.CreateResult);

    [OperationImplementation("utility_operations.get_opc_da_tag_value_double")]
    public override Task<Api.GetOpcDaTagValueDoubleResult> GetOpcDaTagValueDouble(
        Api.GetOpcDaTagValueDoubleRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetOpcDaTagValueDoubleOperation.Descriptor,
            GetOpcDaTagValueDoubleOperation.CreateCommand, GetOpcDaTagValueDoubleOperation.OutputContracts,
            GetOpcDaTagValueDoubleOperation.CreateResult);

    [OperationImplementation("utility_operations.get_opc_da_tag_value_integer")]
    public override Task<Api.GetOpcDaTagValueIntegerResult> GetOpcDaTagValueInteger(
        Api.GetOpcDaTagValueIntegerRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetOpcDaTagValueIntegerOperation.Descriptor,
            GetOpcDaTagValueIntegerOperation.CreateCommand, GetOpcDaTagValueIntegerOperation.OutputContracts,
            GetOpcDaTagValueIntegerOperation.CreateResult);

    [OperationImplementation("utility_operations.get_opc_da_tag_value_string")]
    public override Task<Api.GetOpcDaTagValueStringResult> GetOpcDaTagValueString(
        Api.GetOpcDaTagValueStringRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetOpcDaTagValueStringOperation.Descriptor,
            GetOpcDaTagValueStringOperation.CreateCommand, GetOpcDaTagValueStringOperation.OutputContracts,
            GetOpcDaTagValueStringOperation.CreateResult);

    [OperationImplementation("utility_operations.get_point_notes")]
    public override Task<Api.GetPointNotesResult> GetPointNotes(
        Api.GetPointNotesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPointNotesOperation.Descriptor,
            GetPointNotesOperation.CreateCommand, GetPointNotesOperation.OutputContracts,
            GetPointNotesOperation.CreateResult);

    [OperationImplementation("utility_operations.get_screen_resolution")]
    public override Task<Api.GetScreenResolutionResult> GetScreenResolution(
        Api.GetScreenResolutionRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetScreenResolutionOperation.Descriptor,
            GetScreenResolutionOperation.CreateCommand, GetScreenResolutionOperation.OutputContracts,
            GetScreenResolutionOperation.CreateResult);

    [OperationImplementation("utility_operations.get_working_frame_properties")]
    public override Task<Api.GetWorkingFramePropertiesResult> GetWorkingFrameProperties(
        Api.GetWorkingFramePropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetWorkingFramePropertiesOperation.Descriptor,
            GetWorkingFramePropertiesOperation.CreateCommand, GetWorkingFramePropertiesOperation.OutputContracts,
            GetWorkingFramePropertiesOperation.CreateResult);

    [OperationImplementation("utility_operations.increment_point_name")]
    public override Task<Api.IncrementPointNameResult> IncrementPointName(
        Api.IncrementPointNameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, IncrementPointNameOperation.Descriptor,
            IncrementPointNameOperation.CreateCommand, IncrementPointNameOperation.OutputContracts,
            IncrementPointNameOperation.CreateResult);

    [OperationImplementation("utility_operations.lock_imported_items")]
    public override Task<Api.LockImportedItemsResult> LockImportedItems(
        Api.LockImportedItemsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LockImportedItemsOperation.Descriptor,
            LockImportedItemsOperation.CreateCommand, LockImportedItemsOperation.OutputContracts,
            LockImportedItemsOperation.CreateResult);

    [OperationImplementation("utility_operations.lock_unlock_selected_items")]
    public override Task<Api.LockUnlockSelectedItemsResult> LockUnlockSelectedItems(
        Api.LockUnlockSelectedItemsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LockUnlockSelectedItemsOperation.Descriptor,
            LockUnlockSelectedItemsOperation.CreateCommand, LockUnlockSelectedItemsOperation.OutputContracts,
            LockUnlockSelectedItemsOperation.CreateResult);

    [OperationImplementation("utility_operations.lock_unlock_trapping_control")]
    public override Task<Api.LockUnlockTrappingControlResult> LockUnlockTrappingControl(
        Api.LockUnlockTrappingControlRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LockUnlockTrappingControlOperation.Descriptor,
            LockUnlockTrappingControlOperation.CreateCommand, LockUnlockTrappingControlOperation.OutputContracts,
            LockUnlockTrappingControlOperation.CreateResult);

    [OperationImplementation("utility_operations.move_collection_to_folder")]
    public override Task<Api.MoveCollectionToFolderResult> MoveCollectionToFolder(
        Api.MoveCollectionToFolderRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MoveCollectionToFolderOperation.Descriptor,
            MoveCollectionToFolderOperation.CreateCommand, MoveCollectionToFolderOperation.OutputContracts,
            MoveCollectionToFolderOperation.CreateResult);

    [OperationImplementation("utility_operations.move_folder_to_folder")]
    public override Task<Api.MoveFolderToFolderResult> MoveFolderToFolder(
        Api.MoveFolderToFolderRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MoveFolderToFolderOperation.Descriptor,
            MoveFolderToFolderOperation.CreateCommand, MoveFolderToFolderOperation.OutputContracts,
            MoveFolderToFolderOperation.CreateResult);

    [OperationImplementation("utility_operations.move_instruments_drag_graphically")]
    public override Task<Api.MoveInstrumentsDragGraphicallyResult> MoveInstrumentsDragGraphically(
        Api.MoveInstrumentsDragGraphicallyRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MoveInstrumentsDragGraphicallyOperation.Descriptor,
            MoveInstrumentsDragGraphicallyOperation.CreateCommand, MoveInstrumentsDragGraphicallyOperation.OutputContracts,
            MoveInstrumentsDragGraphicallyOperation.CreateResult);

    [OperationImplementation("utility_operations.move_objects_drag_graphically")]
    public override Task<Api.MoveObjectsDragGraphicallyResult> MoveObjectsDragGraphically(
        Api.MoveObjectsDragGraphicallyRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MoveObjectsDragGraphicallyOperation.Descriptor,
            MoveObjectsDragGraphicallyOperation.CreateCommand, MoveObjectsDragGraphicallyOperation.OutputContracts,
            MoveObjectsDragGraphicallyOperation.CreateResult);

    [OperationImplementation("utility_operations.scale_objects")]
    public override Task<Api.ScaleObjectsResult> ScaleObjects(
        Api.ScaleObjectsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ScaleObjectsOperation.Descriptor,
            ScaleObjectsOperation.CreateCommand, ScaleObjectsOperation.OutputContracts,
            ScaleObjectsOperation.CreateResult);

    [OperationImplementation("utility_operations.set_active_custom_language")]
    public override Task<Api.SetActiveCustomLanguageResult> SetActiveCustomLanguage(
        Api.SetActiveCustomLanguageRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetActiveCustomLanguageOperation.Descriptor,
            SetActiveCustomLanguageOperation.CreateCommand, SetActiveCustomLanguageOperation.OutputContracts,
            SetActiveCustomLanguageOperation.CreateResult);

    [OperationImplementation("utility_operations.set_active_units")]
    public override Task<Api.SetActiveUnitsResult> SetActiveUnits(
        Api.SetActiveUnitsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetActiveUnitsOperation.Descriptor,
            SetActiveUnitsOperation.CreateCommand, SetActiveUnitsOperation.OutputContracts,
            SetActiveUnitsOperation.CreateResult);

    [OperationImplementation("utility_operations.set_angular_representation")]
    public override Task<Api.SetAngularRepresentationResult> SetAngularRepresentation(
        Api.SetAngularRepresentationRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetAngularRepresentationOperation.Descriptor,
            SetAngularRepresentationOperation.CreateCommand, SetAngularRepresentationOperation.OutputContracts,
            SetAngularRepresentationOperation.CreateResult);

    [OperationImplementation("utility_operations.set_auto_event_creation")]
    public override Task<Api.SetAutoEventCreationResult> SetAutoEventCreation(
        Api.SetAutoEventCreationRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetAutoEventCreationOperation.Descriptor,
            SetAutoEventCreationOperation.CreateCommand, SetAutoEventCreationOperation.OutputContracts,
            SetAutoEventCreationOperation.CreateResult);

    [OperationImplementation("utility_operations.set_automatic_backup_state")]
    public override Task<Api.SetAutomaticBackupStateResult> SetAutomaticBackupState(
        Api.SetAutomaticBackupStateRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetAutomaticBackupStateOperation.Descriptor,
            SetAutomaticBackupStateOperation.CreateCommand, SetAutomaticBackupStateOperation.OutputContracts,
            SetAutomaticBackupStateOperation.CreateResult);

    [OperationImplementation("utility_operations.set_automatic_relationship_construction_state")]
    public override Task<Api.SetAutomaticRelationshipConstructionStateResult> SetAutomaticRelationshipConstructionState(
        Api.SetAutomaticRelationshipConstructionStateRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetAutomaticRelationshipConstructionStateOperation.Descriptor,
            SetAutomaticRelationshipConstructionStateOperation.CreateCommand,
            SetAutomaticRelationshipConstructionStateOperation.OutputContracts,
            SetAutomaticRelationshipConstructionStateOperation.CreateResult);

    [OperationImplementation("utility_operations.set_collection_notes")]
    public override Task<Api.SetCollectionNotesResult> SetCollectionNotes(
        Api.SetCollectionNotesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCollectionNotesOperation.Descriptor,
            SetCollectionNotesOperation.CreateCommand, SetCollectionNotesOperation.OutputContracts,
            SetCollectionNotesOperation.CreateResult);

    [OperationImplementation("utility_operations.set_decimal_digits_for_display")]
    public override Task<Api.SetDecimalDigitsForDisplayResult> SetDecimalDigitsForDisplay(
        Api.SetDecimalDigitsForDisplayRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetDecimalDigitsForDisplayOperation.Descriptor,
            SetDecimalDigitsForDisplayOperation.CreateCommand, SetDecimalDigitsForDisplayOperation.OutputContracts,
            SetDecimalDigitsForDisplayOperation.CreateResult);

    [OperationImplementation("utility_operations.set_folder_notes")]
    public override Task<Api.SetFolderNotesResult> SetFolderNotes(
        Api.SetFolderNotesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetFolderNotesOperation.Descriptor,
            SetFolderNotesOperation.CreateCommand, SetFolderNotesOperation.OutputContracts,
            SetFolderNotesOperation.CreateResult);

    [OperationImplementation("utility_operations.set_interaction_mode")]
    public override Task<Api.SetInteractionModeResult> SetInteractionMode(
        Api.SetInteractionModeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetInteractionModeOperation.Descriptor,
            SetInteractionModeOperation.CreateCommand, SetInteractionModeOperation.OutputContracts,
            SetInteractionModeOperation.CreateResult);

    [OperationImplementation("utility_operations.set_logging_state")]
    public override Task<Api.SetLoggingStateResult> SetLoggingState(
        Api.SetLoggingStateRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetLoggingStateOperation.Descriptor,
            SetLoggingStateOperation.CreateCommand, SetLoggingStateOperation.OutputContracts,
            SetLoggingStateOperation.CreateResult);

    [OperationImplementation("utility_operations.set_notification_cancel_override")]
    public override Task<Api.SetNotificationCancelOverrideResult> SetNotificationCancelOverride(
        Api.SetNotificationCancelOverrideRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetNotificationCancelOverrideOperation.Descriptor,
            SetNotificationCancelOverrideOperation.CreateCommand, SetNotificationCancelOverrideOperation.OutputContracts,
            SetNotificationCancelOverrideOperation.CreateResult);

    [OperationImplementation("utility_operations.set_object_notes")]
    public override Task<Api.SetObjectNotesResult> SetObjectNotes(
        Api.SetObjectNotesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetObjectNotesOperation.Descriptor,
            SetObjectNotesOperation.CreateCommand, SetObjectNotesOperation.OutputContracts,
            SetObjectNotesOperation.CreateResult);

    [OperationImplementation("utility_operations.set_opc_da_tag_value_double")]
    public override Task<Api.SetOpcDaTagValueDoubleResult> SetOpcDaTagValueDouble(
        Api.SetOpcDaTagValueDoubleRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetOpcDaTagValueDoubleOperation.Descriptor,
            SetOpcDaTagValueDoubleOperation.CreateCommand, SetOpcDaTagValueDoubleOperation.OutputContracts,
            SetOpcDaTagValueDoubleOperation.CreateResult);

    [OperationImplementation("utility_operations.set_opc_da_tag_value_integer")]
    public override Task<Api.SetOpcDaTagValueIntegerResult> SetOpcDaTagValueInteger(
        Api.SetOpcDaTagValueIntegerRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetOpcDaTagValueIntegerOperation.Descriptor,
            SetOpcDaTagValueIntegerOperation.CreateCommand, SetOpcDaTagValueIntegerOperation.OutputContracts,
            SetOpcDaTagValueIntegerOperation.CreateResult);

    [OperationImplementation("utility_operations.set_opc_da_tag_value_string")]
    public override Task<Api.SetOpcDaTagValueStringResult> SetOpcDaTagValueString(
        Api.SetOpcDaTagValueStringRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetOpcDaTagValueStringOperation.Descriptor,
            SetOpcDaTagValueStringOperation.CreateCommand, SetOpcDaTagValueStringOperation.OutputContracts,
            SetOpcDaTagValueStringOperation.CreateResult);

    [OperationImplementation("utility_operations.set_point_notes")]
    public override Task<Api.SetPointNotesResult> SetPointNotes(
        Api.SetPointNotesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetPointNotesOperation.Descriptor,
            SetPointNotesOperation.CreateCommand, SetPointNotesOperation.OutputContracts,
            SetPointNotesOperation.CreateResult);

    [OperationImplementation("utility_operations.set_user_interface_profile")]
    public override Task<Api.SetUserInterfaceProfileResult> SetUserInterfaceProfile(
        Api.SetUserInterfaceProfileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetUserInterfaceProfileOperation.Descriptor,
            SetUserInterfaceProfileOperation.CreateCommand, SetUserInterfaceProfileOperation.OutputContracts,
            SetUserInterfaceProfileOperation.CreateResult);

    [OperationImplementation("utility_operations.set_view_idle_update_frequency")]
    public override Task<Api.SetViewIdleUpdateFrequencyResult> SetViewIdleUpdateFrequency(
        Api.SetViewIdleUpdateFrequencyRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetViewIdleUpdateFrequencyOperation.Descriptor,
            SetViewIdleUpdateFrequencyOperation.CreateCommand, SetViewIdleUpdateFrequencyOperation.OutputContracts,
            SetViewIdleUpdateFrequencyOperation.CreateResult);

    [OperationImplementation("utility_operations.set_wild_card_asterisk_mode")]
    public override Task<Api.SetWildCardAsteriskModeResult> SetWildCardAsteriskMode(
        Api.SetWildCardAsteriskModeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetWildCardAsteriskModeOperation.Descriptor,
            SetWildCardAsteriskModeOperation.CreateCommand, SetWildCardAsteriskModeOperation.OutputContracts,
            SetWildCardAsteriskModeOperation.CreateResult);

    [OperationImplementation("utility_operations.set_working_frame")]
    public override Task<Api.SetWorkingFrameResult> SetWorkingFrame(
        Api.SetWorkingFrameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetWorkingFrameOperation.Descriptor,
            SetWorkingFrameOperation.CreateCommand, SetWorkingFrameOperation.OutputContracts,
            SetWorkingFrameOperation.CreateResult);

    [OperationImplementation("utility_operations.status_dialog")]
    public override Task<Api.StatusDialogResult> StatusDialog(
        Api.StatusDialogRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, StatusDialogOperation.Descriptor,
            StatusDialogOperation.CreateCommand, StatusDialogOperation.OutputContracts,
            StatusDialogOperation.CreateResult);

    [OperationImplementation("utility_operations.trim_log_file")]
    public override Task<Api.TrimLogFileResult> TrimLogFile(
        Api.TrimLogFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, TrimLogFileOperation.Descriptor,
            TrimLogFileOperation.CreateCommand, TrimLogFileOperation.OutputContracts,
            TrimLogFileOperation.CreateResult);

    [OperationImplementation("utility_operations.write_to_log")]
    public override Task<Api.WriteToLogResult> WriteToLog(
        Api.WriteToLogRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, WriteToLogOperation.Descriptor,
            WriteToLogOperation.CreateCommand, WriteToLogOperation.OutputContracts,
            WriteToLogOperation.CreateResult);

}
