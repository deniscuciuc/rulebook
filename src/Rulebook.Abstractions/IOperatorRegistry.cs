namespace Rulebook;

public interface IOperatorRegistry
{
    IOperator? Resolve(string name);

    void Register(IOperator op);
}
