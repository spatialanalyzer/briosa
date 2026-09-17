using Briosa.Server.Operations.WaveA;
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
        MpOperationServiceExecutor.ExecuteAsync<Api.CloseAllWatchWindowsRequest, Api.CloseAllWatchWindowsResult>(
            executor,
            request,
            context,
            "utility_operations.close_all_watch_windows");

    [OperationImplementation("utility_operations.delete_folder")]
    public override Task<Api.DeleteFolderResult> DeleteFolder(
        Api.DeleteFolderRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteFolderRequest, Api.DeleteFolderResult>(
            executor,
            request,
            context,
            "utility_operations.delete_folder");

    [OperationImplementation("utility_operations.delete_items")]
    public override Task<Api.DeleteItemsResult> DeleteItems(
        Api.DeleteItemsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteItemsRequest, Api.DeleteItemsResult>(
            executor,
            request,
            context,
            "utility_operations.delete_items");

    [OperationImplementation("utility_operations.delete_objects")]
    public override Task<Api.DeleteObjectsResult> DeleteObjects(
        Api.DeleteObjectsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteObjectsRequest, Api.DeleteObjectsResult>(
            executor,
            request,
            context,
            "utility_operations.delete_objects");

    [OperationImplementation("utility_operations.get_active_language")]
    public override Task<Api.GetActiveLanguageResult> GetActiveLanguage(
        Api.GetActiveLanguageRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetActiveLanguageRequest, Api.GetActiveLanguageResult>(
            executor,
            request,
            context,
            "utility_operations.get_active_language");

    [OperationImplementation("utility_operations.get_active_units")]
    public override Task<Api.GetActiveUnitsResult> GetActiveUnits(
        Api.GetActiveUnitsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetActiveUnitsRequest, Api.GetActiveUnitsResult>(
            executor,
            request,
            context,
            "utility_operations.get_active_units");

    [OperationImplementation("utility_operations.get_angular_representation")]
    public override Task<Api.GetAngularRepresentationResult> GetAngularRepresentation(
        Api.GetAngularRepresentationRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetAngularRepresentationRequest, Api.GetAngularRepresentationResult>(
            executor,
            request,
            context,
            "utility_operations.get_angular_representation");

    [OperationImplementation("utility_operations.get_collection_notes")]
    public override Task<Api.GetCollectionNotesResult> GetCollectionNotes(
        Api.GetCollectionNotesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCollectionNotesRequest, Api.GetCollectionNotesResult>(
            executor,
            request,
            context,
            "utility_operations.get_collection_notes");

    [OperationImplementation("utility_operations.get_folder_collections")]
    public override Task<Api.GetFolderCollectionsResult> GetFolderCollections(
        Api.GetFolderCollectionsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetFolderCollectionsRequest, Api.GetFolderCollectionsResult>(
            executor,
            request,
            context,
            "utility_operations.get_folder_collections");

    [OperationImplementation("utility_operations.get_folder_notes")]
    public override Task<Api.GetFolderNotesResult> GetFolderNotes(
        Api.GetFolderNotesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetFolderNotesRequest, Api.GetFolderNotesResult>(
            executor,
            request,
            context,
            "utility_operations.get_folder_notes");

    [OperationImplementation("utility_operations.get_folders_by_wildcard")]
    public override Task<Api.GetFoldersByWildcardResult> GetFoldersByWildcard(
        Api.GetFoldersByWildcardRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetFoldersByWildcardRequest, Api.GetFoldersByWildcardResult>(
            executor,
            request,
            context,
            "utility_operations.get_folders_by_wildcard");

    [OperationImplementation("utility_operations.get_object_notes")]
    public override Task<Api.GetObjectNotesResult> GetObjectNotes(
        Api.GetObjectNotesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetObjectNotesRequest, Api.GetObjectNotesResult>(
            executor,
            request,
            context,
            "utility_operations.get_object_notes");

    [OperationImplementation("utility_operations.get_opc_da_tag_value_double")]
    public override Task<Api.GetOpcDaTagValueDoubleResult> GetOpcDaTagValueDouble(
        Api.GetOpcDaTagValueDoubleRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetOpcDaTagValueDoubleRequest, Api.GetOpcDaTagValueDoubleResult>(
            executor,
            request,
            context,
            "utility_operations.get_opc_da_tag_value_double");

    [OperationImplementation("utility_operations.get_opc_da_tag_value_integer")]
    public override Task<Api.GetOpcDaTagValueIntegerResult> GetOpcDaTagValueInteger(
        Api.GetOpcDaTagValueIntegerRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetOpcDaTagValueIntegerRequest, Api.GetOpcDaTagValueIntegerResult>(
            executor,
            request,
            context,
            "utility_operations.get_opc_da_tag_value_integer");

    [OperationImplementation("utility_operations.get_opc_da_tag_value_string")]
    public override Task<Api.GetOpcDaTagValueStringResult> GetOpcDaTagValueString(
        Api.GetOpcDaTagValueStringRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetOpcDaTagValueStringRequest, Api.GetOpcDaTagValueStringResult>(
            executor,
            request,
            context,
            "utility_operations.get_opc_da_tag_value_string");

    [OperationImplementation("utility_operations.get_point_notes")]
    public override Task<Api.GetPointNotesResult> GetPointNotes(
        Api.GetPointNotesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPointNotesRequest, Api.GetPointNotesResult>(
            executor,
            request,
            context,
            "utility_operations.get_point_notes");

    [OperationImplementation("utility_operations.get_screen_resolution")]
    public override Task<Api.GetScreenResolutionResult> GetScreenResolution(
        Api.GetScreenResolutionRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetScreenResolutionRequest, Api.GetScreenResolutionResult>(
            executor,
            request,
            context,
            "utility_operations.get_screen_resolution");

    [OperationImplementation("utility_operations.get_working_frame_properties")]
    public override Task<Api.GetWorkingFramePropertiesResult> GetWorkingFrameProperties(
        Api.GetWorkingFramePropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetWorkingFramePropertiesRequest, Api.GetWorkingFramePropertiesResult>(
            executor,
            request,
            context,
            "utility_operations.get_working_frame_properties");

    [OperationImplementation("utility_operations.increment_point_name")]
    public override Task<Api.IncrementPointNameResult> IncrementPointName(
        Api.IncrementPointNameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.IncrementPointNameRequest, Api.IncrementPointNameResult>(
            executor,
            request,
            context,
            "utility_operations.increment_point_name");

    [OperationImplementation("utility_operations.lock_imported_items")]
    public override Task<Api.LockImportedItemsResult> LockImportedItems(
        Api.LockImportedItemsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LockImportedItemsRequest, Api.LockImportedItemsResult>(
            executor,
            request,
            context,
            "utility_operations.lock_imported_items");

    [OperationImplementation("utility_operations.lock_unlock_selected_items")]
    public override Task<Api.LockUnlockSelectedItemsResult> LockUnlockSelectedItems(
        Api.LockUnlockSelectedItemsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LockUnlockSelectedItemsRequest, Api.LockUnlockSelectedItemsResult>(
            executor,
            request,
            context,
            "utility_operations.lock_unlock_selected_items");

    [OperationImplementation("utility_operations.lock_unlock_trapping_control")]
    public override Task<Api.LockUnlockTrappingControlResult> LockUnlockTrappingControl(
        Api.LockUnlockTrappingControlRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LockUnlockTrappingControlRequest, Api.LockUnlockTrappingControlResult>(
            executor,
            request,
            context,
            "utility_operations.lock_unlock_trapping_control");

    [OperationImplementation("utility_operations.move_collection_to_folder")]
    public override Task<Api.MoveCollectionToFolderResult> MoveCollectionToFolder(
        Api.MoveCollectionToFolderRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MoveCollectionToFolderRequest, Api.MoveCollectionToFolderResult>(
            executor,
            request,
            context,
            "utility_operations.move_collection_to_folder");

    [OperationImplementation("utility_operations.move_folder_to_folder")]
    public override Task<Api.MoveFolderToFolderResult> MoveFolderToFolder(
        Api.MoveFolderToFolderRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MoveFolderToFolderRequest, Api.MoveFolderToFolderResult>(
            executor,
            request,
            context,
            "utility_operations.move_folder_to_folder");

    [OperationImplementation("utility_operations.move_instruments_drag_graphically")]
    public override Task<Api.MoveInstrumentsDragGraphicallyResult> MoveInstrumentsDragGraphically(
        Api.MoveInstrumentsDragGraphicallyRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MoveInstrumentsDragGraphicallyRequest, Api.MoveInstrumentsDragGraphicallyResult>(
            executor,
            request,
            context,
            "utility_operations.move_instruments_drag_graphically");

    [OperationImplementation("utility_operations.move_objects_drag_graphically")]
    public override Task<Api.MoveObjectsDragGraphicallyResult> MoveObjectsDragGraphically(
        Api.MoveObjectsDragGraphicallyRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MoveObjectsDragGraphicallyRequest, Api.MoveObjectsDragGraphicallyResult>(
            executor,
            request,
            context,
            "utility_operations.move_objects_drag_graphically");

    [OperationImplementation("utility_operations.scale_objects")]
    public override Task<Api.ScaleObjectsResult> ScaleObjects(
        Api.ScaleObjectsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ScaleObjectsRequest, Api.ScaleObjectsResult>(
            executor,
            request,
            context,
            "utility_operations.scale_objects");

    [OperationImplementation("utility_operations.set_active_custom_language")]
    public override Task<Api.SetActiveCustomLanguageResult> SetActiveCustomLanguage(
        Api.SetActiveCustomLanguageRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetActiveCustomLanguageRequest, Api.SetActiveCustomLanguageResult>(
            executor,
            request,
            context,
            "utility_operations.set_active_custom_language");

    [OperationImplementation("utility_operations.set_active_units")]
    public override Task<Api.SetActiveUnitsResult> SetActiveUnits(
        Api.SetActiveUnitsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetActiveUnitsRequest, Api.SetActiveUnitsResult>(
            executor,
            request,
            context,
            "utility_operations.set_active_units");

    [OperationImplementation("utility_operations.set_angular_representation")]
    public override Task<Api.SetAngularRepresentationResult> SetAngularRepresentation(
        Api.SetAngularRepresentationRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetAngularRepresentationRequest, Api.SetAngularRepresentationResult>(
            executor,
            request,
            context,
            "utility_operations.set_angular_representation");

    [OperationImplementation("utility_operations.set_auto_event_creation")]
    public override Task<Api.SetAutoEventCreationResult> SetAutoEventCreation(
        Api.SetAutoEventCreationRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetAutoEventCreationRequest, Api.SetAutoEventCreationResult>(
            executor,
            request,
            context,
            "utility_operations.set_auto_event_creation");

    [OperationImplementation("utility_operations.set_automatic_backup_state")]
    public override Task<Api.SetAutomaticBackupStateResult> SetAutomaticBackupState(
        Api.SetAutomaticBackupStateRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetAutomaticBackupStateRequest, Api.SetAutomaticBackupStateResult>(
            executor,
            request,
            context,
            "utility_operations.set_automatic_backup_state");

    [OperationImplementation("utility_operations.set_automatic_relationship_construction_state")]
    public override Task<Api.SetAutomaticRelationshipConstructionStateResult> SetAutomaticRelationshipConstructionState(
        Api.SetAutomaticRelationshipConstructionStateRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetAutomaticRelationshipConstructionStateRequest, Api.SetAutomaticRelationshipConstructionStateResult>(
            executor,
            request,
            context,
            "utility_operations.set_automatic_relationship_construction_state");

    [OperationImplementation("utility_operations.set_collection_notes")]
    public override Task<Api.SetCollectionNotesResult> SetCollectionNotes(
        Api.SetCollectionNotesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCollectionNotesRequest, Api.SetCollectionNotesResult>(
            executor,
            request,
            context,
            "utility_operations.set_collection_notes");

    [OperationImplementation("utility_operations.set_decimal_digits_for_display")]
    public override Task<Api.SetDecimalDigitsForDisplayResult> SetDecimalDigitsForDisplay(
        Api.SetDecimalDigitsForDisplayRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetDecimalDigitsForDisplayRequest, Api.SetDecimalDigitsForDisplayResult>(
            executor,
            request,
            context,
            "utility_operations.set_decimal_digits_for_display");

    [OperationImplementation("utility_operations.set_folder_notes")]
    public override Task<Api.SetFolderNotesResult> SetFolderNotes(
        Api.SetFolderNotesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetFolderNotesRequest, Api.SetFolderNotesResult>(
            executor,
            request,
            context,
            "utility_operations.set_folder_notes");

    [OperationImplementation("utility_operations.set_interaction_mode")]
    public override Task<Api.SetInteractionModeResult> SetInteractionMode(
        Api.SetInteractionModeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetInteractionModeRequest, Api.SetInteractionModeResult>(
            executor,
            request,
            context,
            "utility_operations.set_interaction_mode");

    [OperationImplementation("utility_operations.set_logging_state")]
    public override Task<Api.SetLoggingStateResult> SetLoggingState(
        Api.SetLoggingStateRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetLoggingStateRequest, Api.SetLoggingStateResult>(
            executor,
            request,
            context,
            "utility_operations.set_logging_state");

    [OperationImplementation("utility_operations.set_notification_cancel_override")]
    public override Task<Api.SetNotificationCancelOverrideResult> SetNotificationCancelOverride(
        Api.SetNotificationCancelOverrideRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetNotificationCancelOverrideRequest, Api.SetNotificationCancelOverrideResult>(
            executor,
            request,
            context,
            "utility_operations.set_notification_cancel_override");

    [OperationImplementation("utility_operations.set_object_notes")]
    public override Task<Api.SetObjectNotesResult> SetObjectNotes(
        Api.SetObjectNotesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetObjectNotesRequest, Api.SetObjectNotesResult>(
            executor,
            request,
            context,
            "utility_operations.set_object_notes");

    [OperationImplementation("utility_operations.set_opc_da_tag_value_double")]
    public override Task<Api.SetOpcDaTagValueDoubleResult> SetOpcDaTagValueDouble(
        Api.SetOpcDaTagValueDoubleRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetOpcDaTagValueDoubleRequest, Api.SetOpcDaTagValueDoubleResult>(
            executor,
            request,
            context,
            "utility_operations.set_opc_da_tag_value_double");

    [OperationImplementation("utility_operations.set_opc_da_tag_value_integer")]
    public override Task<Api.SetOpcDaTagValueIntegerResult> SetOpcDaTagValueInteger(
        Api.SetOpcDaTagValueIntegerRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetOpcDaTagValueIntegerRequest, Api.SetOpcDaTagValueIntegerResult>(
            executor,
            request,
            context,
            "utility_operations.set_opc_da_tag_value_integer");

    [OperationImplementation("utility_operations.set_opc_da_tag_value_string")]
    public override Task<Api.SetOpcDaTagValueStringResult> SetOpcDaTagValueString(
        Api.SetOpcDaTagValueStringRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetOpcDaTagValueStringRequest, Api.SetOpcDaTagValueStringResult>(
            executor,
            request,
            context,
            "utility_operations.set_opc_da_tag_value_string");

    [OperationImplementation("utility_operations.set_point_notes")]
    public override Task<Api.SetPointNotesResult> SetPointNotes(
        Api.SetPointNotesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetPointNotesRequest, Api.SetPointNotesResult>(
            executor,
            request,
            context,
            "utility_operations.set_point_notes");

    [OperationImplementation("utility_operations.set_user_interface_profile")]
    public override Task<Api.SetUserInterfaceProfileResult> SetUserInterfaceProfile(
        Api.SetUserInterfaceProfileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetUserInterfaceProfileRequest, Api.SetUserInterfaceProfileResult>(
            executor,
            request,
            context,
            "utility_operations.set_user_interface_profile");

    [OperationImplementation("utility_operations.set_view_idle_update_frequency")]
    public override Task<Api.SetViewIdleUpdateFrequencyResult> SetViewIdleUpdateFrequency(
        Api.SetViewIdleUpdateFrequencyRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetViewIdleUpdateFrequencyRequest, Api.SetViewIdleUpdateFrequencyResult>(
            executor,
            request,
            context,
            "utility_operations.set_view_idle_update_frequency");

    [OperationImplementation("utility_operations.set_wild_card_asterisk_mode")]
    public override Task<Api.SetWildCardAsteriskModeResult> SetWildCardAsteriskMode(
        Api.SetWildCardAsteriskModeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetWildCardAsteriskModeRequest, Api.SetWildCardAsteriskModeResult>(
            executor,
            request,
            context,
            "utility_operations.set_wild_card_asterisk_mode");

    [OperationImplementation("utility_operations.set_working_frame")]
    public override Task<Api.SetWorkingFrameResult> SetWorkingFrame(
        Api.SetWorkingFrameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetWorkingFrameRequest, Api.SetWorkingFrameResult>(
            executor,
            request,
            context,
            "utility_operations.set_working_frame");

    [OperationImplementation("utility_operations.status_dialog")]
    public override Task<Api.StatusDialogResult> StatusDialog(
        Api.StatusDialogRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.StatusDialogRequest, Api.StatusDialogResult>(
            executor,
            request,
            context,
            "utility_operations.status_dialog");

    [OperationImplementation("utility_operations.trim_log_file")]
    public override Task<Api.TrimLogFileResult> TrimLogFile(
        Api.TrimLogFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.TrimLogFileRequest, Api.TrimLogFileResult>(
            executor,
            request,
            context,
            "utility_operations.trim_log_file");

    [OperationImplementation("utility_operations.write_to_log")]
    public override Task<Api.WriteToLogResult> WriteToLog(
        Api.WriteToLogRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.WriteToLogRequest, Api.WriteToLogResult>(
            executor,
            request,
            context,
            "utility_operations.write_to_log");

}
