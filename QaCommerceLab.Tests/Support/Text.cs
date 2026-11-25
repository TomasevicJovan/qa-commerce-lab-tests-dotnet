using System.Globalization;

namespace QaCommerceLab.Tests.Support;

/// <summary>Money as the shop shows it: "$1,234.56". Amounts in the tests are integer cents.</summary>
public static class Money
{
    public static string Format(int cents) => (cents / 100m).ToString("$#,##0.00", CultureInfo.InvariantCulture);

    public static int Parse(string text)
    {
        var digits = new string(text.Where(c => char.IsDigit(c) || c == '.').ToArray());
        return digits.Length == 0 ? 0 : (int)Math.Round(decimal.Parse(digits, CultureInfo.InvariantCulture) * 100);
    }
}

public static class XPath
{
    /// <summary>A string literal for XPath 1.0, safe for text with quotes or apostrophes.</summary>
    public static string Literal(string text) =>
        !text.Contains('\'') ? $"'{text}'"
        : !text.Contains('"') ? $"\"{text}\""
        : $"concat('{text.Replace("'", "',\"'\",'")}')";
}

public static class Css
{
    /// <summary>A quoted CSS attribute value.</summary>
    public static string Quote(string text) => $"\"{text.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
}
