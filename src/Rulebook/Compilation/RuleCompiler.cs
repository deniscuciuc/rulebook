using System.Collections.Concurrent;

namespace Rulebook.Compilation;

/// <summary>
/// Compiles <see cref="RuleExpression"/> trees into fast <see cref="CompiledRule"/> delegates.
/// Operators are resolved once at compile time (pre-bound), not at every evaluation.
/// Context paths are captured as string constants to avoid repeated allocations.
/// Thread-safe — compiled rules are cached by expression reference.
/// </summary>
public sealed class RuleCompiler
{
    private readonly IOperatorRegistry _operators;
    private readonly ConcurrentDictionary<RuleExpression, CompiledRule> _cache = new();

    public RuleCompiler(IOperatorRegistry operators)
    {
        _operators = operators;
    }

    /// <summary>
    /// Compiles a rule expression into a delegate. The result is cached by expression reference.
    /// </summary>
    public CompiledRule Compile(RuleExpression expression)
    {
        return _cache.GetOrAdd(expression, static (expr, self) =>
        {
            var evaluator = self.CompileCore(expr);
            return new CompiledRule(expr, evaluator);
        }, this);
    }

    private Func<IDecisionContext, bool> CompileCore(RuleExpression expression)
    {
        return expression switch
        {
            ConditionExpression condition => CompileCondition(condition),
            GroupExpression group => CompileGroup(group),
            NotExpression not => CompileNot(not),
            _ => throw new InvalidOperationException($"Unknown expression type: {expression.GetType().Name}")
        };
    }

    private Func<IDecisionContext, bool> CompileCondition(ConditionExpression condition)
    {
        // Pre-resolve operator at compile time — zero lookup at evaluation time
        var op = _operators.Resolve(condition.Operator)
            ?? throw new InvalidOperationException($"Unknown operator '{condition.Operator}' for path '{condition.Path}'.");

        // Capture path and value as closure constants
        var path = condition.Path;
        var value = condition.Value;

        return ctx =>
        {
            if (!ctx.TryGetValue(path, out var leftValue))
                return false;
            return op.Evaluate(leftValue, value);
        };
    }

    private Func<IDecisionContext, bool> CompileGroup(GroupExpression group)
    {
        // Pre-compile all children
        var children = new Func<IDecisionContext, bool>[group.Children.Count];
        for (var i = 0; i < group.Children.Count; i++)
            children[i] = CompileCore(group.Children[i]);

        if (group.Op == LogicalOperator.And)
        {
            return ctx =>
            {
                for (var i = 0; i < children.Length; i++)
                {
                    if (!children[i](ctx))
                        return false; // Short-circuit
                }
                return true;
            };
        }
        else
        {
            return ctx =>
            {
                for (var i = 0; i < children.Length; i++)
                {
                    if (children[i](ctx))
                        return true; // Short-circuit
                }
                return false;
            };
        }
    }

    private Func<IDecisionContext, bool> CompileNot(NotExpression not)
    {
        var inner = CompileCore(not.Inner);
        return ctx => !inner(ctx);
    }
}
