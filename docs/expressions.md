# Expression Syntax

## Grammar

```
expression  = or_expr
or_expr     = and_expr ("||" and_expr)*
and_expr    = unary ("&&" unary)*
unary       = "!" unary | primary
primary     = "(" expression ")" | condition
condition   = path operator value
```

## Paths

Dot-notation identifiers:

```
player.level
session.daysSinceLastLogin
user.profile.country
active
```

## Operators

| Operator | Syntax | Example |
|----------|--------|---------|
| Equal | `==` | `player.level == 10` |
| Not Equal | `!=` | `player.country != "US"` |
| Greater Than | `>` | `player.level > 10` |
| Less Than | `<` | `player.level < 5` |
| Greater Or Equal | `>=` | `player.spent >= 100` |
| Less Or Equal | `<=` | `session.days <= 7` |
| In | `IN` | `player.country IN ["US", "UK"]` |
| Not In | `NOT_IN` | `player.country NOT_IN ["CN", "RU"]` |
| Contains | `CONTAINS` | `player.name CONTAINS "test"` |
| Starts With | `STARTS_WITH` | `player.email STARTS_WITH "admin"` |
| Ends With | `ENDS_WITH` | `player.email ENDS_WITH ".com"` |

## Values

| Type | Examples |
|------|---------|
| String | `"hello"`, `'world'` |
| Number | `42`, `3.14`, `-10` |
| Boolean | `true`, `false` |
| Null | `null` |
| Array | `["US", "UK", "DE"]`, `[1, 2, 3]` |

## Logical Operators

| Operator | Syntax | Behavior |
|----------|--------|----------|
| AND | `&&` | Short-circuits on first false |
| OR | `\|\|` | Short-circuits on first true |
| NOT | `!` | Negates the following expression |

## Grouping

Use parentheses for precedence:

```
(player.level > 10 || player.vip == true) && player.country == "US"
```

## Examples

```
player.level > 10
player.spent > 100 && player.country == "US"
session.daysSinceLastLogin > 7
(player.vip == true || player.spent > 500) && player.active == true
player.country IN ["US", "UK", "DE"]
!player.banned == true
player.name CONTAINS "test"
```

## Type Coercion

- Numeric comparisons: values are coerced to `double`
- String comparisons: ordinal, case-insensitive
- `null == null` is `true`; `null == anything_else` is `false`
- Strings that look like numbers are coerced for numeric operators
