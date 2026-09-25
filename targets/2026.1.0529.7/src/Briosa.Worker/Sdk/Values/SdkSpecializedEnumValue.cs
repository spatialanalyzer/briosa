namespace Briosa.Worker.Sdk;

internal sealed record SdkSpecializedEnumValue<T>(T Value) : ISdkSpecializedEnumValue
    where T : struct, Enum;
