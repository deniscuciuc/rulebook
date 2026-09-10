using Rulebook.Features.ABTesting;

namespace Rulebook.UnitTests;

public class DeterministicBucketAssignerTests
{
    private readonly DeterministicBucketAssigner _assigner = new();

    [Fact]
    public void AssignBucket_SameInputs_SameResult()
    {
        var bucket1 = _assigner.AssignBucket("exp-1", "user-123");
        var bucket2 = _assigner.AssignBucket("exp-1", "user-123");

        Assert.Equal(bucket1, bucket2);
    }

    [Fact]
    public void AssignBucket_DifferentUsers_MayDiffer()
    {
        var bucket1 = _assigner.AssignBucket("exp-1", "user-123");
        var bucket2 = _assigner.AssignBucket("exp-1", "user-456");

        // Not guaranteed to differ, but validates the API works
        Assert.InRange(bucket1, 0, 99);
        Assert.InRange(bucket2, 0, 99);
    }

    [Fact]
    public void AssignBucket_DifferentExperiments_MayDiffer()
    {
        var bucket1 = _assigner.AssignBucket("exp-1", "user-123");
        var bucket2 = _assigner.AssignBucket("exp-2", "user-123");

        Assert.InRange(bucket1, 0, 99);
        Assert.InRange(bucket2, 0, 99);
    }

    [Fact]
    public void AssignBucket_ResultWithinRange()
    {
        for (var i = 0; i < 1000; i++)
        {
            var bucket = _assigner.AssignBucket("exp", $"user-{i}");
            Assert.InRange(bucket, 0, 99);
        }
    }

    [Fact]
    public void AssignBucket_CustomBucketCount()
    {
        var bucket = _assigner.AssignBucket("exp-1", "user-123", 10);
        Assert.InRange(bucket, 0, 9);
    }

    [Fact]
    public void AssignBucket_Distribution_IsReasonablyUniform()
    {
        var buckets = new int[100];
        const int samples = 100_000;

        for (var i = 0; i < samples; i++)
        {
            var bucket = _assigner.AssignBucket("distribution-test", $"user-{i}");
            buckets[bucket]++;
        }

        var expected = samples / 100.0;
        foreach (var count in buckets)
            // Each bucket should be within 30% of expected (generous tolerance)
            Assert.InRange(count, expected * 0.7, expected * 1.3);
    }
}
