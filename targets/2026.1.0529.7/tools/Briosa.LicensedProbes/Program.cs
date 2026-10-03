using Briosa.LicensedProbes;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    // Record the interruption as an unknown outcome instead of killing the process mid-call.
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

return await LicensedProbeProgram.RunAsync(
    args, Console.Out, Console.Error, LicensedProbeProgram.CreateTransport, TimeProvider.System, cancellation.Token).ConfigureAwait(false);
