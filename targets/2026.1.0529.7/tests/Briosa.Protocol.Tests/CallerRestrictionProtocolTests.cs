using global::Briosa;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Server0101 = Briosa.Protocol.Tests.Legacy.Server0101;

namespace Briosa.Protocol.Tests;

/// <summary>
/// The caller restriction denial detail of Server 0.10.2 (#443): two new rule values
/// and two new detail fields, numbered after every existing one, and a reader of the
/// frozen Server 0.10.1 schema keeping every field it knows.
/// </summary>
public sealed class CallerRestrictionProtocolTests
{
    [Fact]
    public void TheNewRulesAndFieldsFollowEveryExistingNumber()
    {
        var rules = OperationOutcomesReflection.Descriptor.FindTypeByName<EnumDescriptor>(nameof(PolicyDenialRule));
        Assert.Equal(6, rules.FindValueByName("POLICY_DENIAL_RULE_CALLER_PROFILE").Number);
        Assert.Equal(7, rules.FindValueByName("POLICY_DENIAL_RULE_CALLER_FLAG").Number);

        // The 0.10.1 numbers and names are unchanged.
        var frozenRules = Server0101.PolicyDenialReflection.Descriptor.FindTypeByName<EnumDescriptor>(
            nameof(Server0101.PolicyDenialRule));
        Assert.All(frozenRules.Values, value => Assert.Equal(value.Name, rules.FindValueByNumber(value.Number).Name));
        Assert.All(Server0101.PolicyDenialDetails.Descriptor.Fields.InFieldNumberOrder(), field =>
        {
            var current = PolicyDenialDetails.Descriptor.FindFieldByNumber(field.FieldNumber);
            Assert.Equal(field.Name, current.Name);
            Assert.Equal(field.FieldType, current.FieldType);
            Assert.Equal(field.HasPresence, current.HasPresence);
        });

        Assert.Equal(
            [(5, "caller_admission_profile"), (6, "caller_denied_flag")],
            PolicyDenialDetails.Descriptor.Fields.InFieldNumberOrder()
                .Where(field => Server0101.PolicyDenialDetails.Descriptor.FindFieldByNumber(field.FieldNumber) is null)
                .Select(field => (field.FieldNumber, field.Name)));
        foreach (var name in new[] { "caller_admission_profile", "caller_denied_flag" })
        {
            var field = PolicyDenialDetails.Descriptor.FindFieldByName(name);
            Assert.Equal(FieldType.String, field.FieldType);
            Assert.True(field.HasPresence, name);
            Assert.False(field.IsRepeated, name);
        }

        // The error keeps its numbers: the caller detail travels inside field 11.
        Assert.Equal(
            Enumerable.Range(1, 12).Except([4]),
            OperationError.Descriptor.Fields.InFieldNumberOrder().Select(field => field.FieldNumber));
    }

    [Fact]
    public void ADetailFromServer0101HasNoCallerFields()
    {
        var details = PolicyDenialDetails.Parser.ParseFrom(new Server0101.PolicyDenialDetails
        {
            Rule = Server0101.PolicyDenialRule.Flag,
            DeniedFlag = "interactive_ui",
            RequestOptionField = "show_results_dialog"
        }.ToByteArray());

        Assert.Equal(PolicyDenialRule.Flag, details.Rule);
        Assert.False(details.HasCallerAdmissionProfile);
        Assert.False(details.HasCallerDeniedFlag);
    }

    [Theory]
    [InlineData(PolicyDenialRule.CallerFlag)]
    [InlineData(PolicyDenialRule.CallerProfile)]
    public void Server0101ReaderKeepsEveryKnownFieldOfACallerDenial(PolicyDenialRule rule)
    {
        var details = new PolicyDenialDetails { Rule = rule, RequestOptionField = "show_results_dialog" };
        if (rule == PolicyDenialRule.CallerFlag)
        {
            details.CallerDeniedFlag = "interactive_ui";
        }
        else
        {
            details.CallerAdmissionProfile = "read-only";
        }

        var current = new OperationError
        {
            OperationId = "analysis_operations.query_points_to_objects",
            Kind = OperationFailureKind.PolicyDenied,
            DiagnosticCode = "caller-restriction-option-denied",
            WorkerGeneration = 0,
            ExecutionDisposition = ExecutionDisposition.NotStarted,
            RecoveryGuidance = RecoveryGuidance.None,
            ReplayGuidance = ReplayGuidance.DoNotReplay,
            ReplaySafety = ReplaySafety.Safe,
            PolicyDenial = details
        };

        var old = Server0101.OperationError.Parser.ParseFrom(current.ToByteArray());

        // Everything an older client reads is intact. The rule is a number it does not
        // know, and it never reads the caller's profile or flag as the server's.
        Assert.Equal(current.OperationId, old.OperationId);
        Assert.Equal(current.Kind, old.Kind);
        Assert.Equal(current.DiagnosticCode, old.DiagnosticCode);
        Assert.Equal(current.ExecutionDisposition, old.ExecutionDisposition);
        Assert.Equal(current.ReplayGuidance, old.ReplayGuidance);
        Assert.Equal(current.ReplaySafety, old.ReplaySafety);
        Assert.Equal((int)rule, (int)old.PolicyDenial.Rule);
        Assert.False(Enum.IsDefined(old.PolicyDenial.Rule));
        Assert.Equal("show_results_dialog", old.PolicyDenial.RequestOptionField);
        Assert.False(old.PolicyDenial.HasDeniedFlag);
        Assert.False(old.PolicyDenial.HasAdmissionProfile);

        // The older reader keeps the caller fields as unknown fields and writes them back.
        Assert.Equal(current, OperationError.Parser.ParseFrom(old.ToByteArray()));
    }
}
