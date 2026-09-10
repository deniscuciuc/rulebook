namespace Rulebook;

public interface IExpressionParser
{
    RuleExpression Parse(string expression);

    bool TryParse(string expression, out RuleExpression? result, out string? errorMessage);
}
