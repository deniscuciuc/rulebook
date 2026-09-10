using System.Text.Json;
using Rulebook.Serialization;

namespace Rulebook.UnitTests;

public class RuleExpressionJsonConverterTests
{
    private readonly JsonSerializerOptions _options = DecisionJsonOptions.Create();

    [Fact]
    public void RoundTrip_ConditionExpression()
    {
        var expr = new ConditionExpression("player.level", ">", 10.0);
        var json = JsonSerializer.Serialize(expr, _options);
        var deserialized = JsonSerializer.Deserialize<RuleExpression>(json, _options);

        var condition = Assert.IsType<ConditionExpression>(deserialized);
        Assert.Equal("player.level", condition.Path);
        Assert.Equal(">", condition.Operator);
        Assert.Equal(10.0, condition.Value);
    }

    [Fact]
    public void RoundTrip_GroupExpression()
    {
        var expr = new GroupExpression(LogicalOperator.And, new RuleExpression[]
        {
            new ConditionExpression("a", "==", 1.0),
            new ConditionExpression("b", "!=", "x")
        });

        var json = JsonSerializer.Serialize(expr, _options);
        var deserialized = JsonSerializer.Deserialize<RuleExpression>(json, _options);

        var group = Assert.IsType<GroupExpression>(deserialized);
        Assert.Equal(LogicalOperator.And, group.Op);
        Assert.Equal(2, group.Children.Count);
    }

    [Fact]
    public void RoundTrip_NotExpression()
    {
        var expr = new NotExpression(new ConditionExpression("banned", "==", true));
        var json = JsonSerializer.Serialize(expr, _options);
        var deserialized = JsonSerializer.Deserialize<RuleExpression>(json, _options);

        var not = Assert.IsType<NotExpression>(deserialized);
        Assert.IsType<ConditionExpression>(not.Inner);
    }

    [Fact]
    public void Deserialize_ConditionFromJson()
    {
        var json = """{"type":"condition","path":"player.level","op":">","value":10}""";
        var expr = JsonSerializer.Deserialize<RuleExpression>(json, _options);

        var condition = Assert.IsType<ConditionExpression>(expr);
        Assert.Equal("player.level", condition.Path);
    }

    [Fact]
    public void Deserialize_GroupFromJson()
    {
        var json = """
                   {
                       "type": "group",
                       "op": "AND",
                       "children": [
                           {"type":"condition","path":"a","op":"==","value":1},
                           {"type":"condition","path":"b","op":"==","value":2}
                       ]
                   }
                   """;

        var expr = JsonSerializer.Deserialize<RuleExpression>(json, _options);
        var group = Assert.IsType<GroupExpression>(expr);
        Assert.Equal(LogicalOperator.And, group.Op);
        Assert.Equal(2, group.Children.Count);
    }

    [Fact]
    public void RoundTrip_NullValue()
    {
        var expr = new ConditionExpression("field", "==", null);
        var json = JsonSerializer.Serialize(expr, _options);
        var deserialized = JsonSerializer.Deserialize<RuleExpression>(json, _options);

        var condition = Assert.IsType<ConditionExpression>(deserialized);
        Assert.Null(condition.Value);
    }

    [Fact]
    public void RoundTrip_BooleanValue()
    {
        var expr = new ConditionExpression("active", "==", true);
        var json = JsonSerializer.Serialize(expr, _options);
        var deserialized = JsonSerializer.Deserialize<RuleExpression>(json, _options);

        var condition = Assert.IsType<ConditionExpression>(deserialized);
        Assert.Equal(true, condition.Value);
    }

    [Fact]
    public void RoundTrip_ArrayValue()
    {
        var expr = new ConditionExpression("country", "IN",
            new List<object?> { "US", "UK" });
        var json = JsonSerializer.Serialize(expr, _options);
        var deserialized = JsonSerializer.Deserialize<RuleExpression>(json, _options);

        var condition = Assert.IsType<ConditionExpression>(deserialized);
        Assert.IsType<List<object?>>(condition.Value);
    }
}
