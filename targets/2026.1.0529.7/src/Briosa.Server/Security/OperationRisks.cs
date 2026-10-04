namespace Briosa.Server.Security;

/// <summary>
/// The closed D1 risk vocabulary decided on #242. Each member has exactly one
/// snake_case name in <see cref="OperationRiskVocabulary"/>; snake_case appears
/// only at a wire or audit boundary, and hyphenated or unknown strings cannot be
/// represented.
/// </summary>
[Flags]
internal enum OperationRisks
{
    /// <summary>No reviewed risk applies.</summary>
    None = 0,

    /// <summary>Reads paths or existence, never file content.</summary>
    FilesystemMetadata = 1 << 0,

    /// <summary>Reads caller-named file content.</summary>
    FilesystemRead = 1 << 1,

    /// <summary>Creates or overwrites caller-named files.</summary>
    FilesystemWrite = 1 << 2,

    /// <summary>Deletes files.</summary>
    FilesystemDelete = 1 << 3,

    /// <summary>Removes SA objects, or discards or replaces unsaved job state.</summary>
    Destructive = 1 << 4,

    /// <summary>Runs or stops MP code.</summary>
    CodeExecution = 1 << 5,

    /// <summary>Can move hardware such as a tracker head, robot, or projector.</summary>
    PhysicalMotion = 1 << 6,

    /// <summary>Requires or controls a live instrument, robot, or appliance session.</summary>
    DeviceSession = 1 << 7,

    /// <summary>Changes configuration that a live device session consumes.</summary>
    DeviceConfig = 1 << 8,

    /// <summary>Blocks on operator input or opens operator UI.</summary>
    InteractiveUi = 1 << 9,

    /// <summary>Talks to systems other than SpatialAnalyzer, such as OPC DA or an appliance IP address.</summary>
    ExternalIo = 1 << 10
}
