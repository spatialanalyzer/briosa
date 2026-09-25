using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal interface IWorkerProcessFactory
{
    ValueTask<IWorkerProcess> StartAsync(
        int generation,
        CancellationToken cancellationToken = default);
}
