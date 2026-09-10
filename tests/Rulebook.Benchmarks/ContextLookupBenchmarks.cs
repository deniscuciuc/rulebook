using BenchmarkDotNet.Attributes;
using Rulebook.Context;

namespace Rulebook.Benchmarks;

public class ContextLookupBenchmarks
{
    private DictionaryDecisionContext _flatContext = null!;
    private DictionaryDecisionContext _nestedContext = null!;
    private DictionaryDecisionContext _deepNestedContext = null!;

    [GlobalSetup]
    public void Setup()
    {
        _flatContext = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["country"] = "US",
            ["age"] = 25.0,
            ["plan"] = "premium"
        });

        _nestedContext = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?>
            {
                ["country"] = "US",
                ["age"] = 25.0,
                ["plan"] = "premium"
            }
        });

        _deepNestedContext = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["app"] = new Dictionary<string, object?>
            {
                ["user"] = new Dictionary<string, object?>
                {
                    ["profile"] = new Dictionary<string, object?>
                    {
                        ["country"] = "US"
                    }
                }
            }
        });
    }

    [Benchmark(Baseline = true)]
    public bool FlatKey_Lookup()
    {
        return _flatContext.TryGetValue("country", out _);
    }

    [Benchmark]
    public bool DottedKey_OneDot()
    {
        return _nestedContext.TryGetValue("user.country", out _);
    }

    [Benchmark]
    public bool DottedKey_ThreeDots()
    {
        return _deepNestedContext.TryGetValue("app.user.profile.country", out _);
    }

    [Benchmark]
    public bool MissingKey()
    {
        return _flatContext.TryGetValue("nonexistent.path.here", out _);
    }
}
