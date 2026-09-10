using NSubstitute;
using Rulebook.Context;

namespace Rulebook.UnitTests;

public class CompositeDecisionContextTests
{
    [Fact]
    public void TryGetValue_FirstContextHasValue_ReturnsIt()
    {
        var ctx1 = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["a"] = 1
        });
        var ctx2 = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["a"] = 2,
            ["b"] = 3
        });

        var composite = new CompositeDecisionContext(ctx1, ctx2);

        Assert.True(composite.TryGetValue("a", out var value));
        Assert.Equal(1, value);
    }

    [Fact]
    public void TryGetValue_FallsThrough_ToSecondContext()
    {
        var ctx1 = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["a"] = 1
        });
        var ctx2 = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["b"] = 2
        });

        var composite = new CompositeDecisionContext(ctx1, ctx2);

        Assert.True(composite.TryGetValue("b", out var value));
        Assert.Equal(2, value);
    }

    [Fact]
    public void TryGetValue_NoMatch_ReturnsFalse()
    {
        var ctx1 = new DictionaryDecisionContext(new Dictionary<string, object?>());
        var composite = new CompositeDecisionContext(ctx1);

        Assert.False(composite.TryGetValue("missing", out _));
    }

    [Fact]
    public async Task GetValueAsync_FallsBackToAsync()
    {
        var mockCtx = Substitute.For<IDecisionContext>();
        mockCtx.TryGetValue("key", out Arg.Any<object?>()).Returns(false);
        mockCtx.GetValueAsync("key", Arg.Any<CancellationToken>()).Returns(new ValueTask<object?>("async_value"));

        var composite = new CompositeDecisionContext(mockCtx);

        var result = await composite.GetValueAsync("key");
        Assert.Equal("async_value", result);
    }
}
