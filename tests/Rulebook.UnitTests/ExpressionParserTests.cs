using Rulebook.Parsing;

namespace Rulebook.UnitTests;

public class ExpressionParserTests
{
    private readonly ExpressionParser _parser = new();

    [Fact]
    public void Parse_SimpleCondition_ReturnsConditionExpression()
    {
        var result = _parser.Parse("player.level > 10");

        var condition = Assert.IsType<ConditionExpression>(result);
        Assert.Equal("player.level", condition.Path);
        Assert.Equal(">", condition.Operator);
        Assert.Equal(10.0, condition.Value);
    }

    [Fact]
    public void Parse_StringComparison_ReturnsConditionWithString()
    {
        var result = _parser.Parse("player.country == \"US\"");

        var condition = Assert.IsType<ConditionExpression>(result);
        Assert.Equal("player.country", condition.Path);
        Assert.Equal("==", condition.Operator);
        Assert.Equal("US", condition.Value);
    }

    [Fact]
    public void Parse_AndExpression_ReturnsGroupExpression()
    {
        var result = _parser.Parse("player.level > 10 && player.country == \"US\"");

        var group = Assert.IsType<GroupExpression>(result);
        Assert.Equal(LogicalOperator.And, group.Op);
        Assert.Equal(2, group.Children.Count);
    }

    [Fact]
    public void Parse_OrExpression_ReturnsGroupExpression()
    {
        var result = _parser.Parse("player.vip == true || player.spent > 100");

        var group = Assert.IsType<GroupExpression>(result);
        Assert.Equal(LogicalOperator.Or, group.Op);
        Assert.Equal(2, group.Children.Count);
    }

    [Fact]
    public void Parse_NestedParentheses_RespectsGrouping()
    {
        var result = _parser.Parse("(player.level > 10 || player.vip == true) && player.country == \"US\"");

        var group = Assert.IsType<GroupExpression>(result);
        Assert.Equal(LogicalOperator.And, group.Op);
        Assert.Equal(2, group.Children.Count);
        Assert.IsType<GroupExpression>(group.Children[0]);
    }

    [Fact]
    public void Parse_NotExpression_ReturnsNotExpression()
    {
        var result = _parser.Parse("!player.banned == true");

        var not = Assert.IsType<NotExpression>(result);
        Assert.IsType<ConditionExpression>(not.Inner);
    }

    [Fact]
    public void Parse_InOperator_WithArray()
    {
        var result = _parser.Parse("player.country IN [\"US\", \"UK\", \"DE\"]");

        var condition = Assert.IsType<ConditionExpression>(result);
        Assert.Equal("IN", condition.Operator);
        var array = Assert.IsType<List<object?>>(condition.Value);
        Assert.Equal(3, array.Count);
    }

    [Fact]
    public void Parse_BooleanLiteral_ParsesCorrectly()
    {
        var result = _parser.Parse("player.active == true");

        var condition = Assert.IsType<ConditionExpression>(result);
        Assert.Equal(true, condition.Value);
    }

    [Fact]
    public void Parse_NullLiteral_ParsesCorrectly()
    {
        var result = _parser.Parse("player.email != null");

        var condition = Assert.IsType<ConditionExpression>(result);
        Assert.Null(condition.Value);
    }

    [Fact]
    public void Parse_NegativeNumber_ParsesCorrectly()
    {
        var result = _parser.Parse("player.balance > -50");

        var condition = Assert.IsType<ConditionExpression>(result);
        Assert.Equal(-50.0, condition.Value);
    }

    [Fact]
    public void Parse_ContainsOperator_ParsesCorrectly()
    {
        var result = _parser.Parse("player.name CONTAINS \"test\"");

        var condition = Assert.IsType<ConditionExpression>(result);
        Assert.Equal("CONTAINS", condition.Operator);
    }

    [Fact]
    public void Parse_MultipleAndConditions_FlattensGroup()
    {
        var result = _parser.Parse("a == 1 && b == 2 && c == 3");

        var group = Assert.IsType<GroupExpression>(result);
        Assert.Equal(LogicalOperator.And, group.Op);
        Assert.Equal(3, group.Children.Count);
    }

    [Fact]
    public void Parse_EmptyExpression_Throws()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse(""));
    }

    [Fact]
    public void TryParse_InvalidExpression_ReturnsFalse()
    {
        var success = _parser.TryParse(">>>", out var result, out var error);

        Assert.False(success);
        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public void Parse_CachesResults_ReturnsSameInstance()
    {
        var expr = "player.level > 10";
        var result1 = _parser.Parse(expr);
        var result2 = _parser.Parse(expr);

        Assert.Same(result1, result2);
    }

    [Fact]
    public void Parse_ExceedsMaxLength_Throws()
    {
        var longExpr = new string('a', 4097) + " == 1";
        Assert.Throws<FormatException>(() => _parser.Parse(longExpr));
    }

    [Fact]
    public void Parse_DeeplyNestedParentheses_Throws()
    {
        // Build 65 levels of nesting — exceeds MaxRecursionDepth of 64
        var open = new string('(', 65);
        var close = new string(')', 65);
        var expr = $"{open}a == 1{close}";
        Assert.Throws<FormatException>(() => _parser.Parse(expr));
    }

    [Fact]
    public void Parse_DeeplyNestedNot_Throws()
    {
        var nots = string.Concat(Enumerable.Repeat("!", 65));
        var expr = $"{nots}a == true";
        Assert.Throws<FormatException>(() => _parser.Parse(expr));
    }

    [Fact]
    public void Parse_ThreePartDottedPath_ParsesCorrectly()
    {
        var result = _parser.Parse("player.stats.kills > 100");

        var condition = Assert.IsType<ConditionExpression>(result);
        Assert.Equal("player.stats.kills", condition.Path);
        Assert.Equal(">", condition.Operator);
        Assert.Equal(100.0, condition.Value);
    }

    [Fact]
    public void Parse_SinglePartPath_ParsesCorrectly()
    {
        var result = _parser.Parse("level > 10");

        var condition = Assert.IsType<ConditionExpression>(result);
        Assert.Equal("level", condition.Path);
    }

    [Fact]
    public void Parse_WordOperators_CaseInsensitive()
    {
        var result = _parser.Parse("x in [1, 2]");
        var condition = Assert.IsType<ConditionExpression>(result);
        Assert.Equal("in", condition.Operator);
    }
}
