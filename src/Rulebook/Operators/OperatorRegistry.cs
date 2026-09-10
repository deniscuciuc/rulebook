using System.Collections.Frozen;

namespace Rulebook.Operators;

public sealed class OperatorRegistry : IOperatorRegistry
{
    private readonly Dictionary<string, IOperator> _mutable = new(StringComparer.OrdinalIgnoreCase);
    private FrozenDictionary<string, IOperator>? _frozen;

    public OperatorRegistry()
    {
        RegisterBuiltIns();
    }

    public IOperator? Resolve(string name)
    {
        // Use frozen dictionary for O(1) lookups after first access (hot path)
        var frozen = _frozen ??= _mutable.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        return frozen.GetValueOrDefault(name);
    }

    public void Register(IOperator op)
    {
        ArgumentNullException.ThrowIfNull(op);

        _mutable[op.Name] = op;
        _frozen = null; // Invalidate frozen cache so next Get rebuilds it
    }

    private void RegisterBuiltIns()
    {
        Register(new EqualOperator());
        Register(new NotEqualOperator());
        Register(new GreaterThanOperator());
        Register(new LessThanOperator());
        Register(new GreaterThanOrEqualOperator());
        Register(new LessThanOrEqualOperator());
        Register(new InOperator());
        Register(new NotInOperator());
        Register(new ContainsOperator());
        Register(new StartsWithOperator());
        Register(new EndsWithOperator());
    }
}
