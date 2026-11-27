using global::Briosa;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Server0100 = Briosa.Protocol.Tests.Legacy.Server0100;

namespace Briosa.Protocol.Tests;

/// <summary>
/// The value-free request validation detail (#442): its reviewed field number, presence,
/// and enum values, and a reader of the frozen Server 0.10.0 schema keeping every field it
/// knows of a validation failure that carries it.
/// </summary>
public sealed class RequestValidationProtocolTests
{
    [Fact]
    public void TheDetailKeepsItsReviewedNumberShapeAndPresence()
    {
        Assert.Equal(12, OperationError.RequestValidationFieldNumber);
        var field = OperationError.Descriptor.FindFieldByNumber(12);
        Assert.Equal("request_validation", field.Name);
        Assert.Equal(FieldType.Message, field.FieldType);
        Assert.False(field.IsRepeated);
        Assert.True(field.HasPresence);
        Assert.Equal(RequestValidationDetails.Descriptor, field.MessageType);
        Assert.Equal(
            Enumerable.Range(1, 12).Except([4]),
            OperationError.Descriptor.Fields.InFieldNumberOrder().Select(candidate => candidate.FieldNumber));

        Assert.Equal(
            [(1, "field_path", FieldType.String), (2, "constraint", FieldType.Enum)],
            RequestValidationDetails.Descriptor.Fields.InFieldNumberOrder()
                .Select(candidate => (candidate.FieldNumber, candidate.Name, candidate.FieldType)));
        Assert.All(RequestValidationDetails.Descriptor.Fields.InFieldNumberOrder(),
            candidate => Assert.False(candidate.IsRepeated));
    }

    [Fact]
    public void TheConstraintEnumHasAnUnspecifiedZeroAndTheReviewedValues()
    {
        var type = OperationOutcomesReflection.Descriptor.FindTypeByName<EnumDescriptor>(nameof(RequestFieldConstraint));
        Assert.Equal(
            [
                "REQUEST_FIELD_CONSTRAINT_UNSPECIFIED",
                "REQUEST_FIELD_CONSTRAINT_REQUIRED",
                "REQUEST_FIELD_CONSTRAINT_NOT_BLANK",
                "REQUEST_FIELD_CONSTRAINT_NOT_EMPTY",
                "REQUEST_FIELD_CONSTRAINT_ELEMENT_COUNT",
                "REQUEST_FIELD_CONSTRAINT_RANGE",
                "REQUEST_FIELD_CONSTRAINT_SUPPORTED_CHOICE",
                "REQUEST_FIELD_CONSTRAINT_ONEOF_REQUIRED"
            ],
            type.Values.OrderBy(value => value.Number).Select(value => value.Name));
        Assert.Equal(Enumerable.Range(0, 8), type.Values.Select(value => value.Number).Order());
    }

    [Fact]
    public void AnErrorFromAnEarlierServerHasNoDetail()
    {
        var error = OperationError.Parser.ParseFrom(new Server0100.OperationError
        {
            OperationId = "utility_operations.scale_objects",
            Kind = OperationFailureKind.Validation,
            DiagnosticCode = "request-validation-failed"
        }.ToByteArray());

        Assert.Null(error.RequestValidation);
    }

    [Fact]
    public void TheDetailFollowsTheBytesOfEveryEarlierField()
    {
        var earlier = ValidationFailure();
        var detailed = earlier.Clone();
        detailed.RequestValidation = new RequestValidationDetails
        {
            FieldPath = "point_name.target_name",
            Constraint = RequestFieldConstraint.NotBlank
        };

        var bytes = detailed.ToByteArray();
        var prefix = earlier.ToByteArray();

        Assert.Equal(prefix, bytes[..prefix.Length]);
        Assert.Equal(detailed, OperationError.Parser.ParseFrom(bytes));
    }

    [Fact]
    public void Server0100ReaderKeepsEveryKnownFieldOfAValidationFailure()
    {
        var current = ValidationFailure();
        current.RequestValidation = new RequestValidationDetails
        {
            FieldPath = "transform.values",
            Constraint = RequestFieldConstraint.ElementCount
        };

        var old = Server0100.OperationError.Parser.ParseFrom(current.ToByteArray());

        foreach (var oldField in Server0100.OperationError.Descriptor.Fields.InFieldNumberOrder())
        {
            var currentField = OperationError.Descriptor.FindFieldByNumber(oldField.FieldNumber);
            Assert.Equal(oldField.Name, currentField.Name);
            Assert.Equal(currentField.Accessor.GetValue(current), oldField.Accessor.GetValue(old));
        }

        // The older reader keeps the detail as an unknown field and writes it back.
        Assert.Equal(current, OperationError.Parser.ParseFrom(old.ToByteArray()));
    }

    [Fact]
    public void AnUnknownConstraintStillReadsWithItsPath()
    {
        var bytes = new RequestValidationDetails
        {
            FieldPath = "objects.object_type",
            Constraint = (RequestFieldConstraint)99
        }.ToByteArray();

        var parsed = RequestValidationDetails.Parser.ParseFrom(bytes);

        Assert.Equal("objects.object_type", parsed.FieldPath);
        Assert.Equal(99, (int)parsed.Constraint);
    }

    private static OperationError ValidationFailure() => new()
    {
        OperationId = "utility_operations.scale_objects",
        Kind = OperationFailureKind.Validation,
        DiagnosticCode = "request-validation-failed",
        WorkerGeneration = 0,
        ExecutionDisposition = ExecutionDisposition.NotStarted,
        RecoveryGuidance = RecoveryGuidance.None,
        ReplayGuidance = ReplayGuidance.DoNotReplay,
        ReplaySafety = ReplaySafety.Unsafe
    };
}
