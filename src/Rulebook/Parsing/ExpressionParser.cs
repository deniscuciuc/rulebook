using System.Collections.Concurrent;
using System.Globalization;

namespace Rulebook.Parsing;

public sealed class ExpressionParser(int maxCacheSize = 10_000) : IExpressionParser
{
    private const int MaxExpressionLength = 4096;
    private const int MaxRecursionDepth = 64;

    private readonly ConcurrentDictionary<string, RuleExpression> _cache = new();

    public RuleExpression Parse(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        return expression.Length > MaxExpressionLength
            ? throw new FormatException($"Expression exceeds maximum length of {MaxExpressionLength} characters.")
            : _cache.GetOrAdd(expression, static (expr, self) => self.ParseCore(expr), this);
    }

    public bool TryParse(string expression, out RuleExpression? result, out string? errorMessage)
    {
        try
        {
            result = Parse(expression);
            errorMessage = null;
            return true;
        }
        catch (FormatException ex)
        {
            result = null;
            errorMessage = ex.Message;
            return false;
        }
    }

    private RuleExpression ParseCore(string expression)
    {
        var tokenizer = new ExpressionTokenizer(expression.AsSpan());
        var tokens = tokenizer.Tokenize();
        var parser = new TokenParser(tokens);
        var result = parser.ParseOrExpression();

        if (parser.Current.Kind != TokenKind.End)
            throw new FormatException(
                $"Unexpected token '{parser.Current.Value}' at position {parser.Current.Position}.");

        // Evict oldest entries instead of clearing entire cache
        if (_cache.Count > maxCacheSize)
            EvictCache();

        return result;
    }

    private void EvictCache()
    {
        // Remove ~25% of entries to avoid frequent eviction
        var toRemove = _cache.Count / 4;
        var removed = 0;
        foreach (var key in _cache.Keys)
        {
            if (removed >= toRemove) break;
            if (_cache.TryRemove(key, out _))
                removed++;
        }
    }

    private ref struct TokenParser(List<Token> tokens)
    {
        private int _pos = 0;
        private int _depth = 0;

        public Token Current => tokens[_pos];

        public RuleExpression ParseOrExpression()
        {
            var left = ParseAndExpression();

            while (Current.Kind == TokenKind.Or)
            {
                Advance();
                var right = ParseAndExpression();

                left = left is GroupExpression { Op: LogicalOperator.Or } existing
                    ? new GroupExpression(LogicalOperator.Or, [.. existing.Children, right])
                    : new GroupExpression(LogicalOperator.Or, [left, right]);
            }

            return left;
        }

        private RuleExpression ParseAndExpression()
        {
            var left = ParseUnary();

            while (Current.Kind == TokenKind.And)
            {
                Advance();
                var right = ParseUnary();

                left = left is GroupExpression { Op: LogicalOperator.And } existing
                    ? new GroupExpression(LogicalOperator.And, [.. existing.Children, right])
                    : new GroupExpression(LogicalOperator.And, [left, right]);
            }

            return left;
        }

        private RuleExpression ParseUnary()
        {
            if (Current.Kind != TokenKind.Not) return ParsePrimary();

            Advance();
            EnterRecursion();

            var inner = ParseUnary();

            ExitRecursion();

            return new NotExpression(inner);
        }

        private RuleExpression ParsePrimary()
        {
            if (Current.Kind != TokenKind.LeftParen) return ParseCondition();

            Advance();
            EnterRecursion();

            var expr = ParseOrExpression();

            ExitRecursion();
            Expect(TokenKind.RightParen, "Expected ')'");

            return expr;
        }

        private ConditionExpression ParseCondition()
        {
            var path = ParsePath();
            var op = ParseOperator();
            var value = ParseValue();

            return new ConditionExpression(path, op, value);
        }

        private string ParsePath()
        {
            if (Current.Kind != TokenKind.Identifier)
                throw new FormatException(
                    $"Expected identifier at position {Current.Position}, got '{Current.Value}'.");

            // Dotted paths are tokenized as single identifiers (e.g. "player.level")
            var path = Current.Value;
            Advance();
            return path;
        }

        private string ParseOperator()
        {
            if (Current.Kind != TokenKind.Operator)
                throw new FormatException($"Expected operator at position {Current.Position}, got '{Current.Value}'.");

            var op = Current.Value;
            Advance();

            return op;
        }

        private object? ParseValue()
        {
            switch (Current.Kind)
            {
                case TokenKind.StringLiteral:
                    var str = Current.Value;
                    Advance();
                    return str;

                case TokenKind.NumberLiteral:
                    var num = double.Parse(Current.Value, CultureInfo.InvariantCulture);
                    Advance();
                    return num;

                case TokenKind.BooleanLiteral:
                    var b = Current.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    Advance();
                    return b;

                case TokenKind.NullLiteral:
                    Advance();
                    return null;

                case TokenKind.LeftBracket:
                    return ParseArray();

                case TokenKind.Identifier:
                case TokenKind.Operator:
                case TokenKind.And:
                case TokenKind.Or:
                case TokenKind.Not:
                case TokenKind.LeftParen:
                case TokenKind.RightParen:
                case TokenKind.RightBracket:
                case TokenKind.Comma:
                case TokenKind.End:
                default:
                    throw new FormatException($"Expected value at position {Current.Position}, got '{Current.Value}'.");
            }
        }

        private List<object?> ParseArray()
        {
            // skip [
            Advance();

            var items = new List<object?>();
            if (Current.Kind != TokenKind.RightBracket)
            {
                items.Add(ParseValue());
                while (Current.Kind == TokenKind.Comma)
                {
                    Advance();
                    items.Add(ParseValue());
                }
            }

            Expect(TokenKind.RightBracket, "Expected ']'");
            return items;
        }

        private void Expect(TokenKind kind, string message)
        {
            if (Current.Kind != kind)
                throw new FormatException($"{message} at position {Current.Position}, got '{Current.Value}'.");
            Advance();
        }

        private void Advance() => _pos++;

        private void EnterRecursion()
        {
            if (++_depth > MaxRecursionDepth)
                throw new FormatException($"Expression exceeds maximum nesting depth of {MaxRecursionDepth}.");
        }

        private void ExitRecursion() => _depth--;
    }
}
