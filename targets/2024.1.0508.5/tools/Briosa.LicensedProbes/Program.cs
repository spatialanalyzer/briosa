using Briosa.LicensedProbes;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    // Record the interruption as an unknown outcome instead of killing the process mid-call.
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

// Licensed session 4 (#402): the SDK-ownership probes and the offline startup-timing report
// are separate commands; everything else is a probe phase.
if (OwnershipOptions.IsOwnership(args))
{
    return await OwnershipProgram.RunAsync(args, Console.Out, Console.Error, OwnershipProgram.CreateEnvironment, cancellation.Token)
        .ConfigureAwait(false);
}

if (StartupTiming.IsStartupTiming(args))
{
    return await StartupTiming.RunAsync(args, Console.Out, Console.Error, cancellation.Token).ConfigureAwait(false);
}

return await LicensedProbeProgram.RunAsync(
    args, Console.Out, Console.Error, LicensedProbeProgram.CreateTransport, LicensedProbeProgram.CreateOperatorConsole,
    TimeProvider.System, cancellation.Token).ConfigureAwait(false);
