namespace Rulebook;

public interface IBucketAssigner
{
    int AssignBucket(string experimentId, string subjectId, int totalBuckets = 100);
}
