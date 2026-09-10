using System.Text.Json;

namespace Rulebook.Serialization;

public static class DecisionJsonOptions
{
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        options.Converters.Add(new RuleExpressionJsonConverter());
        return options;
    }
}
