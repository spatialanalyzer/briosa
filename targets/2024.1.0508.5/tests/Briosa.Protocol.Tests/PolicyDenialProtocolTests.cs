using global::Briosa;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Server0100 = Briosa.Protocol.Tests.Legacy.Server0100;

namespace Briosa.Protocol.Tests;

/// <summary>
/// The typed policy denial and request option schema of Server 0.10.1 (#397):
/// reviewed field numbers, presence, and enum values, and a reader of the frozen
/// Server 0.10.0 schema keeping every field it knows.
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
            ["rule", "denied_flag", "request_option_field", "admission_profile", "caller_admission_profile",
                "caller_denied_flag"],
            Names(PolicyDenialDetails.Descriptor));
        Assert.Equal(
            ["field_name", "effect", "condition", "enabling_enum_values", "omission", "admission", "policy_denial"],
            Names(OperationRequestOption.Descriptor));

        foreach (var field in new[]
        {
            PolicyDenialDetails.Descriptor.FindFieldByName("denied_flag"),
            PolicyDenialDetails.Descriptor.FindFieldByName("request_option_field"),
            PolicyDenialDetails.Descriptor.FindFieldByName("admission_profile"),
            PolicyDenialDetails.Descriptor.FindFieldByName("caller_admission_profile"),
            PolicyDenialDetails.Descriptor.FindFieldByName("caller_denied_flag"),
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
                ["UNSPECIFIED", "PROFILE", "FLAG", "OVERRIDE", "EXCLUSIVE", "UNREVIEWED", "CALLER_PROFILE", "CALLER_FLAG"],
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

    [Fact]
    public void FrozenServer0100SchemaKeepsTheNumbersOfTheCurrentFields()
    {
        Assert.Equal(
            [1, 2, 3, 5, 6, 7, 8, 9, 10],
            Server0100.OperationError.Descriptor.Fields.InFieldNumberOrder().Select(field => field.FieldNumber));
        Assert.Equal(
            Enumerable.Range(1, 11),
            Server0100.OperationCapability.Descriptor.Fields.InFieldNumberOrder().Select(field => field.FieldNumber));
        Assert.Equal(
            Enumerable.Range(3, 5),
            Server0100.ListCapabilitiesResponse.Descriptor.Fields.InFieldNumberOrder().Select(field => field.FieldNumber));
    }

    [Fact]
    public void Server0100ReaderKeepsEveryKnownFieldOfADeniedCall()
    {
        var current = new OperationError
        {
            OperationId = "analysis_operations.query_points_to_objects",
            Kind = OperationFailureKind.PolicyDenied,
            DiagnosticCode = "operation-option-denied",
            WorkerGeneration = 0,
            ExecutionDisposition = ExecutionDisposition.NotStarted,
            RecoveryGuidance = RecoveryGuidance.None,
            ReplayGuidance = ReplayGuidance.DoNotReplay,
            ReplaySafety = ReplaySafety.Safe,
            PolicyDenial = new PolicyDenialDetails
            {
                Rule = PolicyDenialRule.Flag,
                DeniedFlag = "interactive_ui",
                RequestOptionField = "show_results_dialog"
            }
        };

        var old = Server0100.OperationError.Parser.ParseFrom(current.ToByteArray());

        AssertKnownFieldsEqual(current, old);
        // The older reader keeps the detail as an unknown field and writes it back.
        Assert.Equal(current, OperationError.Parser.ParseFrom(old.ToByteArray()));
    }

    [Fact]
    public void Server0100ReaderKeepsEveryKnownFieldOfTheCapabilityListing()
    {
        var current = new ListCapabilitiesResponse
        {
            SpatialAnalyzerTarget = "exact-target",
            ProtocolPackage = "briosa",
            AdmissionProfile = "standard",
            PolicyFingerprint = "sha256:test",
            Operations =
            {
                new OperationCapability
                {
                    OperationId = "file_operations.save_as",
                    GrpcService = "briosa.FileOperations",
                    Rpc = "SaveAs",
                    FullyQualifiedMethod = "/briosa.FileOperations/SaveAs",
                    Effect = OperationEffect.Mutating,
                    ReplaySafety = ReplaySafety.Unsafe,
                    ExecutionScope = OperationExecutionScope.GlobalStateMutation,
                    RiskFlags = { OperationRiskFlag.FilesystemWrite },
                    DurationClass = OperationDurationClass.LongRunning,
                    ValidationStatus = OperationValidationStatus.NoRecordedGap,
                    Admission = OperationAdmission.DeniedFlag,
                    PolicyDenial = new PolicyDenialDetails { Rule = PolicyDenialRule.Flag, DeniedFlag = "filesystem_write" },
                    RequestOptions = new OperationRequestOptions()
                },
                new OperationCapability
                {
                    OperationId = "analysis_operations.query_points_to_objects",
                    Effect = OperationEffect.ReadOnly,
                    Admission = OperationAdmission.Admitted,
                    RequestOptions = new OperationRequestOptions
                    {
                        Options =
                        {
                            new OperationRequestOption
                            {
                                FieldName = "show_results_dialog",
                                Effect = OperationRequestOptionEffect.InteractiveUi,
                                Condition = OperationRequestOptionCondition.WhenTrue,
                                Omission = OperationRequestOptionOmission.DoesNotActivate,
                                Admission = OperationAdmission.DeniedProfile,
                                PolicyDenial = new PolicyDenialDetails
                                {
                                    Rule = PolicyDenialRule.Profile,
                                    AdmissionProfile = "standard",
                                    RequestOptionField = "show_results_dialog"
                                }
                            }
                        }
                    }
                }
            }
        };

        var old = Server0100.ListCapabilitiesResponse.Parser.ParseFrom(current.ToByteArray());

        AssertKnownFieldsEqual(current, old);
        Assert.Equal(current, ListCapabilitiesResponse.Parser.ParseFrom(old.ToByteArray()));
    }

    // Every field of the frozen schema, by number, has the same name, type, presence,
    // and value; frozen nested messages are compared the same way.
    private static void AssertKnownFieldsEqual(IMessage current, IMessage old)
    {
        foreach (var oldField in old.Descriptor.Fields.InFieldNumberOrder())
        {
            var currentField = current.Descriptor.FindFieldByNumber(oldField.FieldNumber);
            Assert.NotNull(currentField);
            Assert.Equal(oldField.Name, currentField.Name);
            Assert.Equal(oldField.FieldType, currentField.FieldType);
            Assert.Equal(oldField.IsRepeated, currentField.IsRepeated);
            if (oldField.HasPresence)
            {
                Assert.Equal(currentField.Accessor.HasValue(current), oldField.Accessor.HasValue(old));
            }

            var currentValue = currentField.Accessor.GetValue(current);
            var oldValue = oldField.Accessor.GetValue(old);
            if (oldField.FieldType == FieldType.Message && oldField.MessageType.File.Package != "briosa")
            {
                var currentItems = oldField.IsRepeated
                    ? ((System.Collections.IList)currentValue).Cast<IMessage>().ToArray()
                    : [(IMessage)currentValue];
                var oldItems = oldField.IsRepeated
                    ? ((System.Collections.IList)oldValue).Cast<IMessage>().ToArray()
                    : [(IMessage)oldValue];
                Assert.Equal(currentItems.Length, oldItems.Length);
                for (var index = 0; index < currentItems.Length; index++)
                {
                    AssertKnownFieldsEqual(currentItems[index], oldItems[index]);
                }
            }
            else
            {
                Assert.Equal(currentValue, oldValue);
            }
        }
    }

    private static string[] Names(MessageDescriptor descriptor) =>
        descriptor.Fields.InFieldNumberOrder().Select(field => field.Name).ToArray();
}
