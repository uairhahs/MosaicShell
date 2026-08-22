using Xunit;

// AppPaths uses process-wide overrides; keep Core tests single-threaded.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
