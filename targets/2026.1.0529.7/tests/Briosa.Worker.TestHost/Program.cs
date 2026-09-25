using Briosa.Worker.TestHost;

if (args.Contains("--performance", StringComparer.Ordinal))
    return await PerformanceWorkerProcess.RunAsync(args).ConfigureAwait(false);

return TestWorkerProcess.Run(args);
