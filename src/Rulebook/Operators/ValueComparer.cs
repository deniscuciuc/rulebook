using System.Globalization;

namespace Rulebook.Operators;

internal static class ValueComparer
{
    public static bool AreEqual(object? left, object? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;

        if (TryCompareNumeric(left, right, out var cmp))
            return cmp == 0;

        return string.Equals(
            Convert.ToString(left, CultureInfo.InvariantCulture),
            Convert.ToString(right, CultureInfo.InvariantCulture),
            StringComparison.OrdinalIgnoreCase);
    }

    public static int? CompareOrdered(object? left, object? right)
    {
        if (left is null || right is null) return null;

        if (TryCompareNumeric(left, right, out var cmp))
            return cmp;

        var ls = Convert.ToString(left, CultureInfo.InvariantCulture);
        var rs = Convert.ToString(right, CultureInfo.InvariantCulture);

        return string.Compare(ls, rs, StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryCompareNumeric(object left, object right, out int result)
    {
        result = 0;

        if (!TryToDouble(left, out var ld) || !TryToDouble(right, out var rd))
            return false;

        result = ld.CompareTo(rd);
        return true;
    }

    public static bool TryToDouble(object value, out double result)
    {
        result = 0;
        switch (value)
        {
            case double d:
                result = d;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case float f:
                result = f;
                return true;
            case decimal m:
                result = (double)m;
                return true;
            case short s:
                result = s;
                return true;
            case byte b:
                result = b;
                return true;
            case string str:
                return double.TryParse(str, CultureInfo.InvariantCulture, out result);
            default:
                return false;
        }
    }
}
