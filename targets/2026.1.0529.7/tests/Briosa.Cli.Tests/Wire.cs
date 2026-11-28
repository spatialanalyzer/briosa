namespace Briosa.Cli.Tests;

/// <summary>
/// The CLI's public contract as literals, written here independently of the
/// production constants, so a test fails when production drifts from the contract
/// instead of following it.
/// </summary>
internal static class Wire
{
    public const string CallerRestrictionHeader = "briosa-caller-restriction";
    public const string ReadOnlyRestriction = "profile=read-only; deny=interactive_ui";
    public const string OperationErrorTrailer = "briosa-operation-error-bin";
    public const string Target = "2026.1.0529.7";
    public const int MaximumInputBytes = 262_144;
    public const int MaximumDepth = 32;
    public const int MaximumEncodedRequestBytes = 65_536;
    public const int DefaultInlineCap = 65_536;

    public const string RequirednessStatement =
        "This schema lists the exact protobuf field names and the JSON form of each value. It does not " +
        "describe which fields the server requires. Protobuf optional (explicit presence) does not mean " +
        "that a field may be omitted, and a field without it can still be required. When the server rejects " +
        "a request, the failure names the first offending field and the constraint it did not meet " +
        "(Server 0.10.2 and later).";
}

/// <summary>The public diagnostic codes the tests expect, as literals.</summary>
internal static class Codes
{
    public const string TooLarge = "input-too-large";
    public const string TooDeep = "input-too-deep";
    public const string MalformedJson = "input-malformed-json";
    public const string RootNotObject = "input-root-not-object";
    public const string UnknownField = "input-unknown-field";
    public const string FieldNameNotExact = "input-field-name-not-exact";
    public const string DuplicateField = "input-duplicate-field";
    public const string OneofConflict = "input-oneof-conflict";
    public const string Null = "input-null";
    public const string TypeMismatch = "input-type-mismatch";
    public const string NumberForm = "input-number-form";
    public const string OutOfRange = "input-out-of-range";
    public const string EnumNameRequired = "input-enum-name-required";
    public const string UnknownEnumName = "input-unknown-enum-name";
    public const string InvalidString = "input-invalid-string";
    public const string EncodedRequestTooLarge = "request-too-large";

    public const string ResultExecutionMissing = "result-execution-missing";
    public const string ResultExecutionContractViolation = "result-execution-contract-violation";
    public const string ResultOutputUnrelated = "result-output-unrelated";
    public const string ResultOutputMissing = "result-output-missing";
    public const string TrailerMalformed = "operation-error-trailer-malformed";
    public const string ErrorInconsistent = "operation-error-inconsistent";
    public const string ErrorUnrelated = "operation-error-unrelated";
    public const string ErrorEnumUnrecognized = "operation-error-enum-unrecognized";
    public const string SpillPermissionsUnsupported = "spill-permissions-unsupported";
    public const string ClientCancelled = "client-cancelled";
    public const string ClientDeadlineExceeded = "client-deadline-exceeded";
}
