namespace Rulebook.Features.ABTesting;

public static class VariantSelector
{
    public static Variant? Select(IReadOnlyList<Variant> variants, int bucket)
    {
        ArgumentNullException.ThrowIfNull(variants);

        if (variants.Count == 0) return null;

        var totalWeight = 0.0;
        for (var i = 0; i < variants.Count; i++)
            totalWeight += variants[i].Weight;

        if (totalWeight <= 0) return null;

        // Normalize bucket to 0..totalWeight range
        var target = bucket / 100.0 * totalWeight;
        var accumulated = 0.0;

        for (var i = 0; i < variants.Count; i++)
        {
            accumulated += variants[i].Weight;
            if (target < accumulated)
                return variants[i];
        }

        // Edge case: return last variant
        return variants[^1];
    }
}
