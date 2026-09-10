namespace Rulebook;

public interface IOperator
{
    string Name { get; }

    bool Evaluate(object? left, object? right);
}
