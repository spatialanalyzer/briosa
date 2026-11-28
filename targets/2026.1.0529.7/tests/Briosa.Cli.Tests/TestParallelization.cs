// The CLI tests start in-process servers and sweep every registered message. Run one
// test class at a time, so that beside the server tests' real-time process bounds
// (for example a 500 ms graceful worker shutdown) this assembly adds at most about
// one core of load when a whole solution runs its test assemblies in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
