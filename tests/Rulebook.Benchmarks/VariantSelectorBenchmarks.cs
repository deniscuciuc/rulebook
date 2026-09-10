using BenchmarkDotNet.Attributes;
using Rulebook.Features.ABTesting;

namespace Rulebook.Benchmarks;

public class VariantSelectorBenchmarks
{
    private IReadOnlyList<Variant> _twoVariants = null!;
    private IReadOnlyList<Variant> _tenVariants = null!;
    private CumulativeWeightTable _twoVariantsTable = null!;
    private CumulativeWeightTable _tenVariantsTable = null!;

    [GlobalSetup]
    public void Setup()
    {
        _twoVariants =
        [
            new Variant("control", 50.0),
            new Variant("treatment", 50.0)
        ];

        _tenVariants = Enumerable.Range(0, 10)
            .Select(i => new Variant($"variant-{i}", 10.0))
            .ToList();

        _twoVariantsTable = new CumulativeWeightTable(_twoVariants);
        _tenVariantsTable = new CumulativeWeightTable(_tenVariants);
    }

    [Benchmark(Baseline = true)]
    public Variant? Static_TwoVariants()
    {
        return VariantSelector.Select(_twoVariants, 42);
    }

    [Benchmark]
    public Variant? Precomputed_TwoVariants()
    {
        return _twoVariantsTable.Select(42);
    }

    [Benchmark]
    public Variant? Static_TenVariants()
    {
        return VariantSelector.Select(_tenVariants, 75);
    }

    [Benchmark]
    public Variant? Precomputed_TenVariants()
    {
        return _tenVariantsTable.Select(75);
    }
}
