using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using Rulebook.Context;
using Rulebook.Features.ABTesting;
using Rulebook.Operators;
using Rulebook.Parsing;

namespace Rulebook.Benchmarks;

public class EndToEndBenchmarks
{
    private DecisionEngine _engine = null!;
    private IDecisionContext _context = null!;
    private DecisionDefinition _simpleDecision = null!;
    private FeatureFlagDefinition _featureFlag = null!;
    private ExperimentDefinition _experiment = null!;

    [GlobalSetup]
    public void Setup()
    {
        var operators = new OperatorRegistry();
        var parser = new ExpressionParser();
        var bucketAssigner = new DeterministicBucketAssigner();
        _engine = new DecisionEngine(parser, operators, bucketAssigner,
            NullLogger<DecisionEngine>.Instance);

        _context = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["user.country"] = "US",
            ["user.age"] = 25.0,
            ["user.plan"] = "premium",
            ["subject.id"] = "user-12345"
        });

        var rule = parser.Parse("user.country == 'US' AND user.age >= 18");

        _simpleDecision = new DecisionDefinition("dec-1", "Test", rule);

        _featureFlag = new FeatureFlagDefinition("flag-1", "Test Flag", rule, 50.0);

        _experiment = new ExperimentDefinition("exp-1", "AB Test", rule,
        [
            new Variant("control", 50.0),
            new Variant("treatment", 50.0)
        ]);
    }

    [Benchmark(Baseline = true)]
    public ValueTask<DecisionResult> Evaluate_SimpleDecision()
    {
        return _engine.EvaluateAsync(_simpleDecision, _context);
    }

    [Benchmark]
    public ValueTask<DecisionResult> Evaluate_FeatureFlag()
    {
        return _engine.EvaluateFeatureFlagAsync(_featureFlag, _context);
    }

    [Benchmark]
    public ValueTask<DecisionResult> Evaluate_Experiment()
    {
        return _engine.EvaluateExperimentAsync(_experiment, _context);
    }

    [Benchmark]
    public ValueTask<bool> Evaluate_RuleOnly()
    {
        return _engine.EvaluateRuleAsync(_experiment.EligibilityRule!, _context);
    }
}
