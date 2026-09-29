using global::Briosa;

namespace Briosa.Server.Security;

/// <summary>
/// Describes one implemented public MP operation for policy, discovery, and audit.
/// </summary>
internal sealed record OperationDescriptor
{
    public OperationDescriptor(
        string OperationId,
        string MpStep,
        string GrpcService,
        string Rpc,
        string FullyQualifiedMethod,
        string Effect,
        OperationExecutionScope ExecutionScope,
        ReplaySafety ReplaySafety,
        IReadOnlyList<string> RiskFlags)
    {
        this.OperationId = OperationId;
        this.MpStep = MpStep;
        this.GrpcService = GrpcService;
        this.Rpc = Rpc;
        this.FullyQualifiedMethod = FullyQualifiedMethod;
        this.Effect = Effect switch
        {
            "read_only" => OperationEffect.ReadOnly,
            "state_mutation" => OperationEffect.Mutating,
            "unknown" => OperationEffect.Unknown,
            _ => throw new ArgumentOutOfRangeException(nameof(Effect), Effect,
                "The operation effect is not recognized.")
        };
        this.ExecutionScope = ExecutionScope;
        this.ReplaySafety = ReplaySafety;
        this.RiskFlags = RiskFlags;
    }

    public string OperationId { get; }
    public string MpStep { get; }
    public string GrpcService { get; }
    public string Rpc { get; }
    public string FullyQualifiedMethod { get; }
    public OperationEffect Effect { get; }
    public OperationExecutionScope ExecutionScope { get; init; }
    public ReplaySafety ReplaySafety { get; }
    public IReadOnlyList<string> RiskFlags { get; }

    public string EffectLabel => Effect switch
    {
        OperationEffect.ReadOnly => "read_only",
        OperationEffect.Mutating => "state_mutation",
        OperationEffect.Unknown => "unknown",
        _ => throw new InvalidOperationException("The operation effect is not recognized.")
    };
}
