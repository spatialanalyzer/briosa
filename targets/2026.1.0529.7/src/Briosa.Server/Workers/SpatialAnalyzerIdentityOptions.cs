using System.Globalization;

namespace Briosa.Server.Workers;

internal sealed record SpatialAnalyzerIdentityOptions(
    OperatorAttestationOptions? ActivatedSdk,
    OperatorAttestationOptions? ConnectedSpatialAnalyzer)
{
    public static SpatialAnalyzerIdentityOptions BindAndValidate(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new SpatialAnalyzerIdentityOptions(
            OperatorAttestationOptions.BindAndValidate(
                configuration,
                ExactTargetIdentityPolicy.ActivatedSdkVersionKey,
                ExactTargetIdentityPolicy.ActivatedSdkReferenceKey),
            OperatorAttestationOptions.BindAndValidate(
                configuration,
                ExactTargetIdentityPolicy.ConnectedSpatialAnalyzerVersionKey,
                ExactTargetIdentityPolicy.ConnectedSpatialAnalyzerReferenceKey));
    }
}
