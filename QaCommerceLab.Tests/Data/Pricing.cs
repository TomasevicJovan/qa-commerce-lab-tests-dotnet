namespace QaCommerceLab.Tests.Data;

public sealed record Totals(int Subtotal, int CouponDiscount, int PaymentDiscount, int Shipping)
{
    public int Discount => CouponDiscount + PaymentDiscount;

    public int Total => Subtotal - Discount + Shipping;
}

/// <summary>
/// The shop's pricing rules written again, independently of the shop, from docs/BUSINESS.md:
/// coupon first, Mastercard second, each rounded half up on its own; shipping last, after discounts.
/// Used to send the expected total when an order is prepared through the API.
/// </summary>
public static class Pricing
{
    public static Totals Calculate(IEnumerable<(ProductInfo Product, int Quantity)> lines, string? coupon, string? cardBrand)
    {
        var bag = lines.ToList();
        var subtotal = bag.Sum(l => l.Product.Price * l.Quantity);
        var couponDiscount = coupon == Coupons.Save10 ? RoundHalfUp(subtotal * 0.10m) : 0;
        var paymentDiscount = cardBrand == "mastercard" ? RoundHalfUp((subtotal - couponDiscount) * 0.10m) : 0;
        var free = subtotal - couponDiscount - paymentDiscount >= Shipping.FreeFrom || bag.All(l => l.Product.FreeShipping);
        return new Totals(subtotal, couponDiscount, paymentDiscount, free ? 0 : Shipping.FlatFee);
    }

    /// <summary>Card brand by number prefix: Visa 4, Mastercard 51–55 or 2221–2720, American Express 34 or 37.</summary>
    public static string? BrandOf(string number)
    {
        var digits = new string(number.Where(char.IsDigit).ToArray());
        if (digits.StartsWith('4')) return "visa";
        if (digits.Length >= 4 && (digits[..2] is "51" or "52" or "53" or "54" or "55" || int.Parse(digits[..4]) is >= 2221 and <= 2720)) return "mastercard";
        if (digits.StartsWith("34") || digits.StartsWith("37")) return "amex";
        return null;
    }

    private static int RoundHalfUp(decimal value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
