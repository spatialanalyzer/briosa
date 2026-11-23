using global::Briosa;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Briosa.Protocol.Tests;

/// <summary>
/// The typed policy denial and request option schema of Server 0.10.1 (#397):
/// reviewed field numbers, presence, and enum values.
/// </summary>
public sealed class PolicyDenialProtocolTests
{
    [Fact]
    public void NewFieldsKeepTheirReviewedNumbersAndPresence()
    {
        Assert.Equal(11, OperationError.PolicyDenialFieldNumber);
        Assert.Equal(12, OperationCapability.PolicyDenialFieldNumber);
        Assert.Equal(13, OperationCapability.RequestOptionsFieldNumber);
        Assert.Equal(1, OperationRequestOptions.OptionsFieldNumber);
        Assert.Equal(
            ["rule", "denied_flag", "request_option_field", "admission_profile"],
            Names(PolicyDenialDetails.Descriptor));
        Assert.Equal(
            ["field_name", "effect", "condition", "enabling_enum_values", "omission", "admission", "policy_denial"],
            Names(OperationRequestOption.Descriptor));

        foreach (var field in new[]
        {
            PolicyDenialDetails.Descriptor.FindFieldByName("denied_flag"),
            PolicyDenialDetails.Descriptor.FindFieldByName("request_option_field"),
            PolicyDenialDetails.Descriptor.FindFieldByName("admission_profile"),
            OperationError.Descriptor.FindFieldByName("policy_denial"),
            OperationCapability.Descriptor.FindFieldByName("policy_denial"),
            OperationCapability.Descriptor.FindFieldByName("request_options"),
            OperationRequestOption.Descriptor.FindFieldByName("policy_denial")
        })
        {
            Assert.True(field.HasPresence, field.FullName);
        }
    }

    [Fact]
    public void NewEnumsHaveAnUnspecifiedZeroAndReviewedValues()
    {
        var expected = new Dictionary<EnumDescriptor, string[]>
        {
            [OperationOutcomesReflection.Descriptor.FindTypeByName<EnumDescriptor>(nameof(PolicyDenialRule))] =
                ["UNSPECIFIED", "PROFILE", "FLAG", "OVERRIDE", "EXCLUSIVE", "UNREVIEWED"],
            [DiscoveryReflection.Descriptor.FindTypeByName<EnumDescriptor>(nameof(OperationRequestOptionEffect))] =
                ["UNSPECIFIED", "INTERACTIVE_UI", "BACKGROUND_WORK"],
            [DiscoveryReflection.Descriptor.FindTypeByName<EnumDescriptor>(nameof(OperationRequestOptionCondition))] =
                ["UNSPECIFIED", "WHEN_TRUE", "WHEN_FALSE", "WHEN_PRESENT", "WHEN_NON_EMPTY", "WHEN_ONE_OF"],
            [DiscoveryReflection.Descriptor.FindTypeByName<EnumDescriptor>(nameof(OperationRequestOptionOmission))] =
                ["UNSPECIFIED", "DOES_NOT_ACTIVATE", "ACTIVATES", "REQUIRED"]
        };

        foreach (var (type, values) in expected)
        {
            var prefix = string.Concat(type.Name.Select((character, index) =>
                index > 0 && char.IsUpper(character) ? $"_{character}" : $"{char.ToUpperInvariant(character)}"));
            Assert.Equal(
                values.Select(value => $"{prefix}_{value}"),
                type.Values.OrderBy(value => value.Number).Select(value => value.Name));
            Assert.Equal(Enumerable.Range(0, values.Length), type.Values.Select(value => value.Number).Order());
        }
    }

    [Fact]
    public void AMessageFromServer0100HasNoDetailAndNoDescribedOptions()
    {
        // Server 0.10.0 never writes the new fields, so they read as absent.
        var capability = OperationCapability.Parser.ParseFrom(new OperationCapability
        {
            OperationId = "file_operations.get_working_directory",
            Admission = OperationAdmission.DeniedFlag
        }.ToByteArray());
        var error = OperationError.Parser.ParseFrom(new OperationError
        {
            OperationId = "file_operations.get_working_directory",
            Kind = OperationFailureKind.PolicyDenied
        }.ToByteArray());

        Assert.Null(capability.PolicyDenial);
        Assert.Null(capability.RequestOptions);
        Assert.Null(error.PolicyDenial);
    }

    [Fact]
    public void DetailRoundTripsWithoutChangingEarlierFields()
    {
        var earlier = new OperationError
        {
            OperationId = "analysis_operations.query_points_to_objects",
            Kind = OperationFailureKind.PolicyDenied,
            DiagnosticCode = "operation-option-denied",
            WorkerGeneration = 3,
            ExecutionDisposition = ExecutionDisposition.NotStarted,
            RecoveryGuidance = RecoveryGuidance.None,
            ReplayGuidance = ReplayGuidance.DoNotReplay,
            ReplaySafety = ReplaySafety.Safe
        };
        var detailed = earlier.Clone();
        detailed.PolicyDenial = new PolicyDenialDetails
        {
            Rule = PolicyDenialRule.Profile,
            AdmissionProfile = "standard",
            RequestOptionField = "show_results_dialog"
        };

        var bytes = detailed.ToByteArray();
        var prefix = earlier.ToByteArray();

        Assert.Equal(prefix, bytes[..prefix.Length]);
        Assert.Equal(detailed, OperationError.Parser.ParseFrom(bytes));
        Assert.False(detailed.PolicyDenial.HasDeniedFlag);
    }

    private static string[] Names(MessageDescriptor descriptor) =>
        descriptor.Fields.InFieldNumberOrder().Select(field => field.Name).ToArray();
}
