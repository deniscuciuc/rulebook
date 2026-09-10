using BenchmarkDotNet.Attributes;
using Rulebook.Compilation;
using Rulebook.Context;
using Rulebook.Engine;
using Rulebook.Operators;

namespace Rulebook.Benchmarks;

public class RuleEvaluationBenchmarks
{
    private RuleExpression _simpleRule = null!;
    private RuleExpression _complexRule = null!;
    private IDecisionContext _context = null!;
    private RuleCompiler _compiler = null!;
    private CompiledRule _compiledSimple = null!;
    private CompiledRule _compiledComplex = null!;
    private RuleEvaluator _evaluator = null!;

    [GlobalSetup]
    public void Setup()
    {
        var operators = new OperatorRegistry();
        _compiler = new RuleCompiler(operators);
        _evaluator = new RuleEvaluator(operators, new Microsoft.Extensions.Logging.Abstractions.NullLogger<DecisionEngine>());

        _context = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["user.country"] = "US",
            ["user.age"] = 25.0,
            ["user.plan"] = "premium",
            ["user.level"] = 10.0,
            ["user.active"] = true,
            ["device.os"] = "ios",
            ["device.version"] = "17.0"
        });

        // Simple: single condition
        _simpleRule = new ConditionExpression("user.country", "==", "US");

        // Complex: nested AND/OR with multiple conditions
        _complexRule = new GroupExpression(LogicalOperator.And,
        [
            new ConditionExpression("user.country", "==", "US"),
            new GroupExpression(LogicalOperator.Or,
            [
                new ConditionExpression("user.age", ">=", 18.0),
                new ConditionExpression("user.plan", "==", "premium")
            ]),
            new NotExpression(new ConditionExpression("device.os", "==", "android")),
            new ConditionExpression("user.level", ">", 5.0)
        ]);

        _compiledSimple = _compiler.Compile(_simpleRule);
        _compiledComplex = _compiler.Compile(_complexRule);
    }

    [Benchmark(Baseline = true)]
    public bool TreeWalk_Simple()
    {
        return _evaluator.TryEvaluateSync(_simpleRule, _context) ?? false;
    }

    [Benchmark]
    public bool Compiled_Simple()
    {
        return _compiledSimple.Evaluate(_context);
    }

    [Benchmark]
    public bool TreeWalk_Complex()
    {
        return _evaluator.TryEvaluateSync(_complexRule, _context) ?? false;
    }

    [Benchmark]
    public bool Compiled_Complex()
    {
        return _compiledComplex.Evaluate(_context);
    }

    [Benchmark]
    public CompiledRule Compile_Simple()
    {
        return _compiler.Compile(_simpleRule);
    }

    [Benchmark]
    public CompiledRule Compile_Complex()
    {
        return _compiler.Compile(_complexRule);
    }
}
