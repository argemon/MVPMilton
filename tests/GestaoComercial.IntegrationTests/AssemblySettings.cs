using Xunit;

// Keep fixtures within the shared Atlas connection/operation budget. Individual
// concurrency scenarios still run requests and transactions in parallel.
[assembly: CollectionBehavior(MaxParallelThreads = 2)]
