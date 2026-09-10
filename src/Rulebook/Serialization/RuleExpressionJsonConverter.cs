using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rulebook.Serialization;

public sealed class RuleExpressionJsonConverter : JsonConverter<RuleExpression>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(RuleExpression).IsAssignableFrom(typeToConvert);
    }

    public override RuleExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return ReadExpression(doc.RootElement, options);
    }

    public override void Write(Utf8JsonWriter writer, RuleExpression value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        WriteExpression(writer, value, options);
    }

    private static RuleExpression ReadExpression(JsonElement element, JsonSerializerOptions options)
    {
        var type = element.GetProperty("type").GetString() ??
                   throw new JsonException("Missing 'type' discriminator on rule expression.");

        return type.ToLowerInvariant() switch
        {
            "condition" => ReadCondition(element),
            "group" => ReadGroup(element, options),
            "not" => ReadNot(element, options),
            _ => throw new JsonException($"Unknown expression type '{type}'.")
        };
    }

    private static ConditionExpression ReadCondition(JsonElement element)
    {
        var path = element.GetProperty("path").GetString() ??
                   throw new JsonException("Missing 'path' on condition expression.");
        var op = element.GetProperty("op").GetString() ??
                 throw new JsonException("Missing 'op' on condition expression.");
        var value = ReadValue(element.GetProperty("value"));

        return new ConditionExpression(path, op, value);
    }

    private static GroupExpression ReadGroup(JsonElement element, JsonSerializerOptions options)
    {
        var opStr = element.GetProperty("op").GetString() ??
                    throw new JsonException("Missing 'op' on group expression.");

        var op = opStr.ToUpperInvariant() switch
        {
            "AND" => LogicalOperator.And,
            "OR" => LogicalOperator.Or,
            _ => throw new JsonException($"Unknown logical operator '{opStr}'.")
        };

        var children = element.GetProperty("children").EnumerateArray()
            .Select(child => ReadExpression(child, options))
            .ToList();

        return new GroupExpression(op, children);
    }

    private static NotExpression ReadNot(JsonElement element, JsonSerializerOptions options)
    {
        var inner = ReadExpression(element.GetProperty("inner"), options);
        return new NotExpression(inner);
    }

    private static object? ReadValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => ReadArray(element),
            _ => throw new JsonException($"Unsupported value kind '{element.ValueKind}'.")
        };
    }

    private static List<object?> ReadArray(JsonElement element)
    {
        return element.EnumerateArray().Select(ReadValue).ToList();
    }

    private static void WriteExpression(Utf8JsonWriter writer, RuleExpression expression, JsonSerializerOptions options)
    {
        switch (expression)
        {
            case ConditionExpression c:
                writer.WriteStartObject();
                writer.WriteString("type", "condition");
                writer.WriteString("path", c.Path);
                writer.WriteString("op", c.Operator);
                writer.WritePropertyName("value");
                WriteValue(writer, c.Value);
                break;

            case GroupExpression g:
                writer.WriteStartObject();
                writer.WriteString("type", "group");
                writer.WriteString("op", g.Op == LogicalOperator.And ? "AND" : "OR");
                writer.WriteStartArray("children");
                foreach (var child in g.Children)
                    WriteExpression(writer, child, options);
                writer.WriteEndArray();
                break;

            case NotExpression n:
                writer.WriteStartObject();
                writer.WriteString("type", "not");
                writer.WritePropertyName("inner");
                WriteExpression(writer, n.Inner, options);
                break;

            default:
                throw new JsonException($"Unknown expression type: {expression.GetType().Name}");
        }

        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case IEnumerable<object?> arr:
                writer.WriteStartArray();
                foreach (var item in arr)
                    WriteValue(writer, item);
                writer.WriteEndArray();
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}
