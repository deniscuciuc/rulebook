namespace Rulebook.Compilation;

/// <summary>
/// A pre-compiled rule that evaluates a <see cref="RuleExpression"/> against an <see cref="IDecisionContext"/>
/// without tree-walking overhead. The delegate is compiled once and reused for every evaluation.
/// </summary>
public sealed class CompiledRule
{
    private readonly Func<IDecisionContext, bool> _evaluator;

    internal CompiledRule(RuleExpression source, Func<IDecisionContext, bool> evaluator)
    {
        Source = source;
        _evaluator = evaluator;
    }

    /// <summary>The original expression tree.</summary>
    public RuleExpression Source { get; }

    /// <summary>Evaluates the rule synchronously. Returns false if a required context value is missing.</summary>
    public bool Evaluate(IDecisionContext context) => _evaluator(context);
}
