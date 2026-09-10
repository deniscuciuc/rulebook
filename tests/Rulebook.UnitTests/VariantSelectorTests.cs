using Rulebook.Features.ABTesting;

namespace Rulebook.UnitTests;

public class VariantSelectorTests
{
    [Fact]
    public void Select_TwoEqualVariants_SplitsAtMidpoint()
    {
        var variants = new List<Variant>
        {
            new("A", 50),
            new("B", 50)
        };

        // Bucket 0-49 → A, 50-99 → B
        Assert.Equal("A", VariantSelector.Select(variants, 0)?.Id);
        Assert.Equal("A", VariantSelector.Select(variants, 49)?.Id);
        Assert.Equal("B", VariantSelector.Select(variants, 50)?.Id);
        Assert.Equal("B", VariantSelector.Select(variants, 99)?.Id);
    }

    [Fact]
    public void Select_ThreeVariants_DistributesByWeight()
    {
        var variants = new List<Variant>
        {
            new("A", 50),
            new("B", 30),
            new("C", 20)
        };

        Assert.Equal("A", VariantSelector.Select(variants, 0)?.Id);
        Assert.Equal("A", VariantSelector.Select(variants, 49)?.Id);
        Assert.Equal("B", VariantSelector.Select(variants, 50)?.Id);
        Assert.Equal("B", VariantSelector.Select(variants, 79)?.Id);
        Assert.Equal("C", VariantSelector.Select(variants, 80)?.Id);
        Assert.Equal("C", VariantSelector.Select(variants, 99)?.Id);
    }

    [Fact]
    public void Select_EmptyVariants_ReturnsNull()
    {
        Assert.Null(VariantSelector.Select([], 0));
    }

    [Fact]
    public void Select_SingleVariant_AlwaysReturnsSame()
    {
        var variants = new List<Variant> { new("only", 100) };

        for (var i = 0; i < 100; i++)
            Assert.Equal("only", VariantSelector.Select(variants, i)?.Id);
    }

    [Fact]
    public void Select_VariantWithPayload_PreservesPayload()
    {
        var payload = new Dictionary<string, object> { ["color"] = "red" };
        var variants = new List<Variant> { new("A", 100, payload) };

        var selected = VariantSelector.Select(variants, 0);
        Assert.NotNull(selected);
        Assert.Equal("red", selected.Payload!["color"]);
    }
}
