using Rulebook.Operators;

namespace Rulebook.UnitTests;

public class OperatorTests
{
    [Theory]
    [InlineData(10.0, 10.0, true)]
    [InlineData(10.0, 20.0, false)]
    [InlineData("hello", "HELLO", true)]
    [InlineData(null, null, true)]
    [InlineData(null, "test", false)]
    public void Equal_EvaluatesCorrectly(object? left, object? right, bool expected)
    {
        var op = new EqualOperator();
        Assert.Equal(expected, op.Evaluate(left, right));
    }

    [Theory]
    [InlineData(10.0, 10.0, false)]
    [InlineData(10.0, 20.0, true)]
    [InlineData(null, null, false)]
    public void NotEqual_EvaluatesCorrectly(object? left, object? right, bool expected)
    {
        var op = new NotEqualOperator();
        Assert.Equal(expected, op.Evaluate(left, right));
    }

    [Theory]
    [InlineData(20.0, 10.0, true)]
    [InlineData(10.0, 10.0, false)]
    [InlineData(5.0, 10.0, false)]
    public void GreaterThan_EvaluatesCorrectly(object? left, object? right, bool expected)
    {
        var op = new GreaterThanOperator();
        Assert.Equal(expected, op.Evaluate(left, right));
    }

    [Theory]
    [InlineData(5.0, 10.0, true)]
    [InlineData(10.0, 10.0, false)]
    [InlineData(20.0, 10.0, false)]
    public void LessThan_EvaluatesCorrectly(object? left, object? right, bool expected)
    {
        var op = new LessThanOperator();
        Assert.Equal(expected, op.Evaluate(left, right));
    }

    [Theory]
    [InlineData(20.0, 10.0, true)]
    [InlineData(10.0, 10.0, true)]
    [InlineData(5.0, 10.0, false)]
    public void GreaterThanOrEqual_EvaluatesCorrectly(object? left, object? right, bool expected)
    {
        var op = new GreaterThanOrEqualOperator();
        Assert.Equal(expected, op.Evaluate(left, right));
    }

    [Theory]
    [InlineData(5.0, 10.0, true)]
    [InlineData(10.0, 10.0, true)]
    [InlineData(20.0, 10.0, false)]
    public void LessThanOrEqual_EvaluatesCorrectly(object? left, object? right, bool expected)
    {
        var op = new LessThanOrEqualOperator();
        Assert.Equal(expected, op.Evaluate(left, right));
    }

    [Fact]
    public void GreaterThan_NullLeft_ReturnsFalse()
    {
        var op = new GreaterThanOperator();
        Assert.False(op.Evaluate(null, 10.0));
    }

    [Fact]
    public void In_ValueInCollection_ReturnsTrue()
    {
        var op = new InOperator();
        var list = new List<object?> { "US", "UK", "DE" };
        Assert.True(op.Evaluate("US", list));
    }

    [Fact]
    public void In_ValueNotInCollection_ReturnsFalse()
    {
        var op = new InOperator();
        var list = new List<object?> { "US", "UK", "DE" };
        Assert.False(op.Evaluate("FR", list));
    }

    [Fact]
    public void NotIn_ValueInCollection_ReturnsFalse()
    {
        var op = new NotInOperator();
        var list = new List<object?> { "US", "UK", "DE" };
        Assert.False(op.Evaluate("US", list));
    }

    [Fact]
    public void Contains_SubstringFound_ReturnsTrue()
    {
        var op = new ContainsOperator();
        Assert.True(op.Evaluate("hello world", "world"));
    }

    [Fact]
    public void Contains_CaseInsensitive()
    {
        var op = new ContainsOperator();
        Assert.True(op.Evaluate("Hello World", "HELLO"));
    }

    [Fact]
    public void StartsWith_MatchesPrefix()
    {
        var op = new StartsWithOperator();
        Assert.True(op.Evaluate("hello world", "hello"));
        Assert.False(op.Evaluate("hello world", "world"));
    }

    [Fact]
    public void EndsWith_MatchesSuffix()
    {
        var op = new EndsWithOperator();
        Assert.True(op.Evaluate("hello world", "world"));
        Assert.False(op.Evaluate("hello world", "hello"));
    }

    [Fact]
    public void Equal_StringAndNumber_Coerces()
    {
        var op = new EqualOperator();
        Assert.True(op.Evaluate("10", 10.0));
    }

    [Fact]
    public void GreaterThan_IntAndDouble_ComparesNumerically()
    {
        var op = new GreaterThanOperator();
        Assert.True(op.Evaluate(20, 10.5));
    }
}
