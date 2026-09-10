using System.Globalization;

namespace Rulebook.Operators;

internal sealed class ContainsOperator : IOperator
{
    public string Name => OperatorNames.Contains;

    public bool Evaluate(object? left, object? right)
    {
        var ls = Convert.ToString(left, CultureInfo.InvariantCulture);
        var rs = Convert.ToString(right, CultureInfo.InvariantCulture);
        if (ls is null || rs is null) return false;
        return ls.Contains(rs, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class StartsWithOperator : IOperator
{
    public string Name => OperatorNames.StartsWith;

    public bool Evaluate(object? left, object? right)
    {
        var ls = Convert.ToString(left, CultureInfo.InvariantCulture);
        var rs = Convert.ToString(right, CultureInfo.InvariantCulture);
        if (ls is null || rs is null) return false;
        return ls.StartsWith(rs, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class EndsWithOperator : IOperator
{
    public string Name => OperatorNames.EndsWith;

    public bool Evaluate(object? left, object? right)
    {
        var ls = Convert.ToString(left, CultureInfo.InvariantCulture);
        var rs = Convert.ToString(right, CultureInfo.InvariantCulture);
        if (ls is null || rs is null) return false;
        return ls.EndsWith(rs, StringComparison.OrdinalIgnoreCase);
    }
}
