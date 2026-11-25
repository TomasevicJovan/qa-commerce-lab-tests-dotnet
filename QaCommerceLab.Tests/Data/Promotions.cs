namespace QaCommerceLab.Tests.Data;

public static class Coupons
{
    /// <summary>10% off the subtotal.</summary>
    public const string Save10 = "SAVE10";

    /// <summary>A real code whose validity has ended.</summary>
    public const string Expired = "EXPIRED";
}

public static class Shipping
{
    /// <summary>Free shipping from this amount, after all discounts (cents).</summary>
    public const int FreeFrom = 15_000;

    public const int FlatFee = 700;
}
