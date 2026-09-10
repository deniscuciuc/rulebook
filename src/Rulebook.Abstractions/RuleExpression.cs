namespace Rulebook;

public abstract record RuleExpression;

public sealed record ConditionExpression(
    string Path,
    string Operator,
    object? Value) : RuleExpression;

public sealed record GroupExpression(
    LogicalOperator Op,
    IReadOnlyList<RuleExpression> Children) : RuleExpression;

public sealed record NotExpression(
    RuleExpression Inner) : RuleExpression;

public enum LogicalOperator
{
    And,
    Or
}
