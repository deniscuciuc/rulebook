using Microsoft.Extensions.Logging;

namespace Rulebook.Engine;

internal sealed class RuleEvaluator(IOperatorRegistry operators, ILogger logger)
{
    /// <summary>
    /// Synchronous evaluation — zero async overhead when context supports TryGetValue.
    /// Returns null if async fallback is needed for any condition.
    /// </summary>
    public bool? TryEvaluateSync(RuleExpression expression, IDecisionContext context)
    {
        return expression switch
        {
            ConditionExpression condition => TryEvaluateConditionSync(condition, context),
            GroupExpression group => TryEvaluateGroupSync(group, context),
            NotExpression not => TryEvaluateSync(not.Inner, context) is { } result ? !result : null,
            _ => throw new InvalidOperationException($"Unknown expression type: {expression.GetType().Name}")
        };
    }

    public ValueTask<bool> EvaluateAsync(
        RuleExpression expression,
        IDecisionContext context,
        CancellationToken ct)
    {
        // Fast path: try fully synchronous evaluation first
        if (TryEvaluateSync(expression, context) is { } syncResult)
            return new ValueTask<bool>(syncResult);

        return EvaluateAsyncCore(expression, context, ct);
    }

    private async ValueTask<bool> EvaluateAsyncCore(
        RuleExpression expression,
        IDecisionContext context,
        CancellationToken ct)
    {
        return expression switch
        {
            ConditionExpression condition => await EvaluateConditionAsync(condition, context, ct).ConfigureAwait(false),
            GroupExpression group => await EvaluateGroupAsync(group, context, ct).ConfigureAwait(false),
            NotExpression not => !await EvaluateAsyncCore(not.Inner, context, ct).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Unknown expression type: {expression.GetType().Name}")
        };
    }

    private bool? TryEvaluateConditionSync(ConditionExpression condition, IDecisionContext context)
    {
        var op = operators.Resolve(condition.Operator);
        if (op is null)
        {
            logger.LogWarning("Unknown operator '{Operator}' in condition for path '{Path}'", condition.Operator, condition.Path);
            return false;
        }

        if (!context.TryGetValue(condition.Path, out var leftValue))
            return null; // Need async fallback

        return op.Evaluate(leftValue, condition.Value);
    }

    private bool? TryEvaluateGroupSync(GroupExpression group, IDecisionContext context)
    {
        if (group.Op == LogicalOperator.And)
        {
            foreach (var child in group.Children)
            {
                var result = TryEvaluateSync(child, context);
                if (result is null) return null;
                if (!result.Value) return false;
            }
            return true;
        }
        else
        {
            foreach (var child in group.Children)
            {
                var result = TryEvaluateSync(child, context);
                if (result is null) return null;
                if (result.Value) return true;
            }
            return false;
        }
    }

    private async ValueTask<bool> EvaluateConditionAsync(
        ConditionExpression condition,
        IDecisionContext context,
        CancellationToken ct)
    {
        var op = operators.Resolve(condition.Operator);
        if (op is null)
        {
            logger.LogWarning("Unknown operator '{Operator}' in condition for path '{Path}'", condition.Operator, condition.Path);
            return false;
        }

        object? leftValue;
        if (!context.TryGetValue(condition.Path, out leftValue))
        {
            leftValue = await context.GetValueAsync(condition.Path, ct).ConfigureAwait(false);
        }

        return op.Evaluate(leftValue, condition.Value);
    }

    private async ValueTask<bool> EvaluateGroupAsync(
        GroupExpression group,
        IDecisionContext context,
        CancellationToken ct)
    {
        if (group.Op == LogicalOperator.And)
        {
            foreach (var child in group.Children)
            {
                if (!await EvaluateAsyncCore(child, context, ct).ConfigureAwait(false))
                    return false;
            }
            return true;
        }
        else
        {
            foreach (var child in group.Children)
            {
                if (await EvaluateAsyncCore(child, context, ct).ConfigureAwait(false))
                    return true;
            }
            return false;
        }
    }
}
