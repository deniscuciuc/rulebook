using BenchmarkDotNet.Attributes;
using Rulebook.Features.ABTesting;

namespace Rulebook.Benchmarks;

public class BucketAssignerBenchmarks
{
    private DeterministicBucketAssigner _assigner = null!;

    [GlobalSetup]
    public void Setup()
    {
        _assigner = new DeterministicBucketAssigner();
    }

    [Benchmark]
    public int ShortIds()
    {
        return _assigner.AssignBucket("exp1", "user42");
    }

    [Benchmark]
    public int LongIds()
    {
        return _assigner.AssignBucket(
            "experiment-long-name-with-many-characters-to-test-array-pool",
            "user-with-a-very-long-identifier-that-exceeds-stack-buffer-threshold-to-test-rent-and-return-path-12345678901234567890");
    }

    [Benchmark]
    public int TypicalIds()
    {
        return _assigner.AssignBucket("ab-test-homepage-v2", "550e8400-e29b-41d4-a716-446655440000");
    }
}
