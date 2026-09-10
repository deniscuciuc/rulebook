namespace Rulebook.Features.ABTesting;

/// <summary>
/// Pre-computed cumulative weight table for a set of variants.
/// Computing once avoids iterating and summing weights on every bucket assignment.
/// Thread-safe and immutable after construction.
/// </summary>
public sealed class CumulativeWeightTable
{
    private readonly double[] _cumulativeWeights;
    private readonly IReadOnlyList<Variant> _variants;
    private readonly double _totalWeight;

    public CumulativeWeightTable(IReadOnlyList<Variant> variants)
    {
        ArgumentNullException.ThrowIfNull(variants);

        _variants = variants;
        _cumulativeWeights = new double[variants.Count];
        var cumulative = 0.0;

        for (var i = 0; i < variants.Count; i++)
        {
            cumulative += variants[i].Weight;
            _cumulativeWeights[i] = cumulative;
        }

        _totalWeight = cumulative;
    }

    public double TotalWeight => _totalWeight;

    public int Count => _variants.Count;

    /// <summary>
    /// Selects a variant by bucket using pre-computed cumulative weights.
    /// O(n) scan but with no per-call weight summation.
    /// </summary>
    public Variant? Select(int bucket)
    {
        if (_variants.Count == 0 || _totalWeight <= 0)
            return null;

        var target = bucket / 100.0 * _totalWeight;

        for (var i = 0; i < _cumulativeWeights.Length; i++)
        {
            if (target < _cumulativeWeights[i])
                return _variants[i];
        }

        return _variants[^1];
    }
}
