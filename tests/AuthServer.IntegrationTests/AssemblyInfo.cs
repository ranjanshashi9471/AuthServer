using Xunit;

// Critical for preventing database reset race conditions across tests
[assembly: CollectionBehavior(DisableTestParallelization = true)]
