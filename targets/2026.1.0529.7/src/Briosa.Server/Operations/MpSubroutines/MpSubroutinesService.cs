using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.MpSubroutines;

internal sealed class MpSubroutinesService(OperationExecutor executor)
    : Api.MpSubroutines.MpSubroutinesBase
{
    [OperationImplementation("mp_subroutines.run_subroutine")]
    public override Task<Api.RunSubroutineResult> RunSubroutine(
        Api.RunSubroutineRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, RunSubroutineOperation.Descriptor,
            RunSubroutineOperation.CreateCommand, RunSubroutineOperation.OutputContracts,
            RunSubroutineOperation.CreateResult);

}
