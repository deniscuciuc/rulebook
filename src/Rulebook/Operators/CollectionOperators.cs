using System.Collections;

namespace Rulebook.Operators;

internal sealed class InOperator : IOperator
{
    public string Name => OperatorNames.In;

    public bool Evaluate(object? left, object? right)
    {
        if (left is null || right is null) return false;
        if (right is not IEnumerable enumerable || right is string) return false;

        foreach (var item in enumerable)
        {
            if (ValueComparer.AreEqual(left, item))
                return true;
        }

        return false;
    }
}

internal sealed class NotInOperator : IOperator
{
    public string Name => OperatorNames.NotIn;

    public bool Evaluate(object? left, object? right)
    {
        if (left is null || right is null) return true;
        if (right is not IEnumerable enumerable || right is string) return true;

        foreach (var item in enumerable)
        {
            if (ValueComparer.AreEqual(left, item))
                return false;
        }

        return true;
    }
}
