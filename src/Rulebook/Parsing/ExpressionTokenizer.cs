namespace Rulebook.Parsing;

internal enum TokenKind
{
    Identifier,
    StringLiteral,
    NumberLiteral,
    BooleanLiteral,
    NullLiteral,
    Operator,
    And,
    Or,
    Not,
    LeftParen,
    RightParen,
    LeftBracket,
    RightBracket,
    Comma,
    End
}

internal readonly record struct Token(TokenKind Kind, string Value, int Position);

internal ref struct ExpressionTokenizer(ReadOnlySpan<char> source)
{
    private readonly ReadOnlySpan<char> _source = source;
    private int _pos = 0;

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (_pos < _source.Length)
        {
            SkipWhitespace();
            if (_pos >= _source.Length) break;

            var ch = _source[_pos];

            switch (ch)
            {
                case '(':
                    tokens.Add(new Token(TokenKind.LeftParen, "(", _pos++));
                    continue;
                case ')':
                    tokens.Add(new Token(TokenKind.RightParen, ")", _pos++));
                    continue;
                case '[':
                    tokens.Add(new Token(TokenKind.LeftBracket, "[", _pos++));
                    continue;
                case ']':
                    tokens.Add(new Token(TokenKind.RightBracket, "]", _pos++));
                    continue;
                case ',':
                    tokens.Add(new Token(TokenKind.Comma, ",", _pos++));
                    continue;
                case '&' when Peek(1) == '&':
                    tokens.Add(new Token(TokenKind.And, "&&", _pos));
                    _pos += 2;
                    continue;
                case '|' when Peek(1) == '|':
                    tokens.Add(new Token(TokenKind.Or, "||", _pos));
                    _pos += 2;
                    continue;
                case '!' when Peek(1) != '=':
                    tokens.Add(new Token(TokenKind.Not, "!", _pos++));
                    continue;
                case '=' or '!' or '>' or '<':
                    {
                        var op = ReadOperatorSymbol();
                        tokens.Add(new Token(TokenKind.Operator, op, _pos - op.Length));
                        continue;
                    }
                case '"':
                case '\'':
                    {
                        var str = ReadString(ch);
                        tokens.Add(new Token(TokenKind.StringLiteral, str, _pos - str.Length - 2));
                        continue;
                    }
            }

            if (char.IsDigit(ch) || (ch == '-' && _pos + 1 < _source.Length && char.IsDigit(_source[_pos + 1])))
            {
                var num = ReadNumber();
                tokens.Add(new Token(TokenKind.NumberLiteral, num, _pos - num.Length));
                continue;
            }

            if (char.IsLetter(ch) || ch == '_')
            {
                var word = ReadWord();
                var kind = ClassifyWord(word);
                tokens.Add(new Token(kind, word, _pos - word.Length));
                continue;
            }

            throw new FormatException($"Unexpected character '{ch}' at position {_pos}.");
        }

        tokens.Add(new Token(TokenKind.End, "", _pos));
        return tokens;
    }

    private void SkipWhitespace()
    {
        while (_pos < _source.Length && char.IsWhiteSpace(_source[_pos]))
            _pos++;
    }

    private char Peek(int offset)
    {
        var idx = _pos + offset;
        return idx < _source.Length ? _source[idx] : '\0';
    }

    private string ReadOperatorSymbol()
    {
        var start = _pos;
        var ch = _source[_pos++];

        if (_pos >= _source.Length || _source[_pos] != '=') return ch.ToString();

        _pos++;
        return _source[start.._pos].ToString();
    }

    private string ReadString(char quote)
    {
        _pos++; // skip opening quote
        var start = _pos;

        while (_pos < _source.Length && _source[_pos] != quote)
        {
            if (_source[_pos] == '\\' && _pos + 1 < _source.Length)
                _pos++; // skip escaped char
            _pos++;
        }

        if (_pos >= _source.Length)
            throw new FormatException($"Unterminated string starting at position {start - 1}.");

        var value = _source[start.._pos].ToString();
        _pos++; // skip closing quote
        return value;
    }

    private string ReadNumber()
    {
        var start = _pos;
        if (_source[_pos] == '-') _pos++;

        while (_pos < _source.Length && (char.IsDigit(_source[_pos]) || _source[_pos] == '.'))
            _pos++;

        return _source[start.._pos].ToString();
    }

    private string ReadWord()
    {
        var start = _pos;
        while (_pos < _source.Length &&
               (char.IsLetterOrDigit(_source[_pos]) || _source[_pos] == '_' || _source[_pos] == '.'))
        {
            // Don't end an identifier with a trailing dot
            if (_source[_pos] == '.' && (_pos + 1 >= _source.Length || !char.IsLetterOrDigit(_source[_pos + 1])))
                break;
            _pos++;
        }

        return _source[start.._pos].ToString();
    }

    private static TokenKind ClassifyWord(string word)
    {
        // Use ordinal case-insensitive comparison to avoid ToUpperInvariant allocation
        if (word.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            word.Equals("false", StringComparison.OrdinalIgnoreCase))
            return TokenKind.BooleanLiteral;

        if (word.Equals("null", StringComparison.OrdinalIgnoreCase))
            return TokenKind.NullLiteral;

        if (word.Equals("AND", StringComparison.OrdinalIgnoreCase))
            return TokenKind.And;

        if (word.Equals("OR", StringComparison.OrdinalIgnoreCase))
            return TokenKind.Or;

        if (word.Equals("NOT", StringComparison.OrdinalIgnoreCase))
            return TokenKind.Not;

        if (word.Equals("IN", StringComparison.OrdinalIgnoreCase) ||
            word.Equals("NOT_IN", StringComparison.OrdinalIgnoreCase) ||
            word.Equals("CONTAINS", StringComparison.OrdinalIgnoreCase) ||
            word.Equals("STARTS_WITH", StringComparison.OrdinalIgnoreCase) ||
            word.Equals("ENDS_WITH", StringComparison.OrdinalIgnoreCase))
            return TokenKind.Operator;

        return TokenKind.Identifier;
    }
}
