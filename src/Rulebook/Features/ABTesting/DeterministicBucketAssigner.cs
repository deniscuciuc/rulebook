using System.Buffers;
using System.IO.Hashing;
using System.Text;

namespace Rulebook.Features.ABTesting;

public sealed class DeterministicBucketAssigner : IBucketAssigner
{
    private const int StackAllocThreshold = 256;

    public int AssignBucket(string experimentId, string subjectId, int totalBuckets = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(experimentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(totalBuckets, 0);

        // Calculate max byte count: experimentId + ':' + subjectId in UTF-8
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(experimentId.Length + 1 + subjectId.Length);

        byte[]? rented = null;
        var buffer = maxByteCount <= StackAllocThreshold
            ? stackalloc byte[StackAllocThreshold]
            : rented = ArrayPool<byte>.Shared.Rent(maxByteCount);

        try
        {
            var written = Encoding.UTF8.GetBytes(experimentId, buffer);
            buffer[written++] = (byte)':';
            written += Encoding.UTF8.GetBytes(subjectId, buffer[written..]);

            var hash = XxHash32.HashToUInt32(buffer[..written]);
            return (int)(hash % (uint)totalBuckets);
        }
        finally
        {
            if (rented is not null)
                ArrayPool<byte>.Shared.Return(rented);
        }
    }
}
