using BenchmarkDotNet.Attributes;
using Rulebook.Parsing;

namespace Rulebook.Benchmarks;

public class ParserBenchmarks
{
    private ExpressionParser _parser = null!;

    private const string SimpleExpr = "user.country == 'US'";
    private const string MediumExpr = "user.country == 'US' AND user.age >= 18 AND device.os == 'ios'";
    private const string ComplexExpr =
        "(user.country == 'US' OR user.country == 'GB') AND user.age >= 18 AND NOT device.os == 'android' AND user.plan == 'premium'";

    [GlobalSetup]
    public void Setup()
    {
        _parser = new ExpressionParser();

        // Warm up cache for cached benchmarks
        _parser.Parse(SimpleExpr);
        _parser.Parse(MediumExpr);
        _parser.Parse(ComplexExpr);
    }

    [Benchmark(Baseline = true)]
    public RuleExpression Parse_Simple_Cached()
    {
        return _parser.Parse(SimpleExpr);
    }

    [Benchmark]
    public RuleExpression Parse_Medium_Cached()
    {
        return _parser.Parse(MediumExpr);
    }

    [Benchmark]
    public RuleExpression Parse_Complex_Cached()
    {
        return _parser.Parse(ComplexExpr);
    }

    [Benchmark]
    public RuleExpression Parse_Simple_Cold()
    {
        var parser = new ExpressionParser();
        return parser.Parse(SimpleExpr);
    }

    [Benchmark]
    public RuleExpression Parse_Complex_Cold()
    {
        var parser = new ExpressionParser();
        return parser.Parse(ComplexExpr);
    }
}
