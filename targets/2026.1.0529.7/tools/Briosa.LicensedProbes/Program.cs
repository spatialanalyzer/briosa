using Briosa.LicensedProbes;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    // Record the interruption as an unknown outcome instead of killing the process mid-call.
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

// Licensed session 4 (#402): the read-only process observation is a separate command;
// everything else is a probe phase. Both run inside the command boundary.
if (ProcessObservation.IsObservation(args))
{
    return await CommandBoundary.RunAsync(() => ProcessObservation.RunAsync(args, Console.Out, Console.Error, WindowsProcessTable.Instance,
        SystemPause.Instance, TimeProvider.System, cancellation.Token), Console.Error).ConfigureAwait(false);
}

return await CommandBoundary.RunAsync(() => LicensedProbeProgram.RunAsync(
    args, Console.Out, Console.Error, LicensedProbeProgram.CreateTransport, LicensedProbeProgram.CreateOperatorConsole,
    TimeProvider.System, cancellation.Token), Console.Error).ConfigureAwait(false);
