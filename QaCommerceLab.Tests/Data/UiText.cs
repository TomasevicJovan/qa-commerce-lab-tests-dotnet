namespace QaCommerceLab.Tests.Data;

/// <summary>
/// Fixed texts of the storefront that step definitions check. Texts a scenario is about are written in
/// the feature files; this file holds the ones the steps need on their own. Source: docs/BUSINESS.md.
/// </summary>
public static class UiText
{
    public const string SignIn = "Sign in";
    public const string NoProductsFound = "No products found. Try another search or category.";
    public const string PaymentSuccessful = "Payment successful";
    public const string PaymentFact = "Payment";
    public const string DeliverToFact = "Deliver to";
    /// <summary>Order status as My orders shows it.</summary>
    public const string OrderConfirmedStatus = "confirmed";

    // Rows of the totals box (bag, checkout and order page).
    public const string Subtotal = "Subtotal";
    public const string Discount = "Discount";
    public const string Shipping = "Shipping";
    public const string Total = "Total";

    /// <summary>The account button of a signed-in customer.</summary>
    public static string AccountButton(string name) => $"{name} ▾";

    /// <summary>The order banner after a successful payment.</summary>
    public static string PaymentWentThrough(int totalCents) =>
        $"Your payment of {Support.Money.Format(totalCents)} went through and the order is confirmed.";

    /// <summary>A line on the order page, for example "2 × Still Ceramic Mug".</summary>
    public static string OrderLine(int quantity, string productName) => $"{quantity} × {productName}";

    /// <summary>The delivery address as the order page shows it, one part per line.</summary>
    public static string DeliveryAddress(Api.Address a) => $"{a.Name}\n{a.Line1}\n{a.PostalCode} {a.City}, {a.Country}";
}
