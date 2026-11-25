namespace QaCommerceLab.Tests.Data;

/// <summary>A published test card. <see cref="Number"/> is printed the way it is on the card, with spaces.</summary>
public sealed record TestCard(string Brand, string BrandName, string Number)
{
    public string Digits => Number.Replace(" ", string.Empty);

    public string Last4 => Digits[^4..];

    public string Cvc => Brand == "amex" ? "1234" : "123";

    /// <summary>How the shop shows the card after payment, for example "Visa •••• 4242".</summary>
    public string Display => $"{BrandName} •••• {Last4}";
}

/// <summary>Test cards from qa-commerce-lab docs/BUSINESS.md. Feature files refer to them by name.</summary>
public static class Cards
{
    public static readonly TestCard Visa = new("visa", "Visa", "4242 4242 4242 4242");
    public static readonly TestCard Mastercard = new("mastercard", "Mastercard", "5555 5555 5555 4444");

    /// <summary>A valid number that the simulated bank always declines.</summary>
    public static readonly TestCard Declined = new("visa", "Visa", "4000 0000 0000 0002");

    private static readonly Dictionary<string, TestCard> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Visa"] = Visa,
        ["Mastercard"] = Mastercard,
        ["declined"] = Declined,
    };

    public static TestCard ByName(string name) =>
        Named.TryGetValue(name, out var card) ? card : throw new ArgumentException($"No test card named '{name}' in Data/Cards.cs.");

    /// <summary>Expiry in MM/YY, the given number of months from now.</summary>
    public static string ExpiryIn(int months)
    {
        var date = DateTime.UtcNow.AddMonths(months);
        return $"{date:MM}/{date:yy}";
    }

    public static string ValidExpiry => ExpiryIn(24);
}
