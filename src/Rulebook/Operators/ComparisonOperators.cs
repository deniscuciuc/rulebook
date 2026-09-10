namespace Rulebook.Operators;

internal sealed class EqualOperator : IOperator
{
    public string Name => OperatorNames.Equal;

    public bool Evaluate(object? left, object? right)
    {
        return ValueComparer.AreEqual(left, right);
    }
}

internal sealed class NotEqualOperator : IOperator
{
    public string Name => OperatorNames.NotEqual;

    public bool Evaluate(object? left, object? right)
    {
        return !ValueComparer.AreEqual(left, right);
    }
}

internal sealed class GreaterThanOperator : IOperator
{
    public string Name => OperatorNames.GreaterThan;

    public bool Evaluate(object? left, object? right)
    {
        return ValueComparer.CompareOrdered(left, right) is > 0;
    }
}

internal sealed class LessThanOperator : IOperator
{
    public string Name => OperatorNames.LessThan;

    public bool Evaluate(object? left, object? right)
    {
        return ValueComparer.CompareOrdered(left, right) is < 0;
    }
}

internal sealed class GreaterThanOrEqualOperator : IOperator
{
    public string Name => OperatorNames.GreaterThanOrEqual;

    public bool Evaluate(object? left, object? right)
    {
        return ValueComparer.CompareOrdered(left, right) is >= 0;
    }
}

internal sealed class LessThanOrEqualOperator : IOperator
{
    public string Name => OperatorNames.LessThanOrEqual;

    public bool Evaluate(object? left, object? right)
    {
        return ValueComparer.CompareOrdered(left, right) is <= 0;
    }
}
