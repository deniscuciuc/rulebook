using Rulebook.Context;

namespace Rulebook.UnitTests;

public class DictionaryDecisionContextTests
{
    [Fact]
    public void TryGetValue_DirectKey_ReturnsValue()
    {
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["level"] = 42
        });

        Assert.True(ctx.TryGetValue("level", out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetValue_DotNotation_ResolvesNestedDictionary()
    {
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player"] = new Dictionary<string, object?>
            {
                ["level"] = 42,
                ["stats"] = new Dictionary<string, object?>
                {
                    ["kills"] = 100
                }
            }
        });

        Assert.True(ctx.TryGetValue("player.level", out var level));
        Assert.Equal(42, level);

        Assert.True(ctx.TryGetValue("player.stats.kills", out var kills));
        Assert.Equal(100, kills);
    }

    [Fact]
    public void TryGetValue_MissingKey_ReturnsFalse()
    {
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["level"] = 42
        });

        Assert.False(ctx.TryGetValue("nonexistent", out _));
    }

    [Fact]
    public void TryGetValue_MissingNestedKey_ReturnsFalse()
    {
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player"] = new Dictionary<string, object?>
            {
                ["level"] = 42
            }
        });

        Assert.False(ctx.TryGetValue("player.name", out _));
    }

    [Fact]
    public void TryGetValue_DirectKeyPrioritized_OverDotNotation()
    {
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.level"] = 99,
            ["player"] = new Dictionary<string, object?>
            {
                ["level"] = 42
            }
        });

        Assert.True(ctx.TryGetValue("player.level", out var value));
        Assert.Equal(99, value);
    }

    [Fact]
    public async Task GetValueAsync_ReturnsValue()
    {
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.level"] = 42
        });

        var value = await ctx.GetValueAsync("player.level");
        Assert.Equal(42, value);
    }
}
