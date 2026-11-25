namespace QaCommerceLab.Tests.Api;

// Shapes of the lab API. Money is always integer USD cents.

public sealed record Credentials(string Name, string Email, string Password);

public sealed record User(string Id, string Email, string Name);

public sealed record Session(string Token, User User, long ExpiresAt);

public sealed record CartItem(string ProductId, int Quantity);

/// <summary>Card details as typed by a customer. Never stored or returned by the lab.</summary>
public sealed record NewCard(string Holder, string Number, string Expiry, string Cvc);

/// <summary>A saved card: never the full number or the security code.</summary>
public sealed record SavedCard(string Id, string Holder, string Brand, string Last4, int ExpMonth, int ExpYear, bool Expired);

public sealed record Address(string Name, string Line1, string City, string PostalCode, string Country);

public sealed record CheckoutRequest
{
    public required Address Address { get; init; }
    public required string PaymentMethod { get; init; }
    public string? SavedCardId { get; init; }
    public NewCard? Card { get; init; }
    public bool? SaveCard { get; init; }
    public string? Coupon { get; init; }
    public required int ExpectedTotal { get; init; }
}

public sealed record OrderLine(string ProductId, int Quantity, string Name, int UnitPrice, bool FreeShipping);

public sealed record OrderPayment(string Method, string? Brand, string? Last4);

public sealed record OrderHistoryEntry(string Status);

public sealed record Order(
    string Id,
    IReadOnlyList<OrderLine> Items,
    Address Address,
    OrderPayment Payment,
    string Coupon,
    int Subtotal,
    int CouponDiscount,
    int PaymentDiscount,
    int Discount,
    int Shipping,
    int Total,
    string Status,
    string? Reason,
    IReadOnlyList<OrderHistoryEntry> History)
{
    public static readonly string[] TerminalStatuses = [ApiValues.Confirmed, ApiValues.Rejected, ApiValues.Cancelled];

    public bool IsSettled => TerminalStatuses.Contains(Status);
}

public sealed record Payment(string OrderId, string Status, int? Amount, string? Reason);

/// <summary>Global lab switches. Only the ones the scenarios use are listed; the rest keep their values.</summary>
public sealed record LabConfig
{
    public string? PaymentMode { get; init; }
}

/// <summary>Values the API uses for payment methods, order and payment states and lab switches (docs/BUSINESS.md).</summary>
public static class ApiValues
{
    public const string Card = "card";
    public const string TestWallet = "test-wallet";

    public const string Confirmed = "confirmed";
    public const string Rejected = "rejected";
    public const string Cancelled = "cancelled";

    public const string PaymentPending = "pending";
    public const string PaymentFailed = "failed";

    public const string BankDeclines = "decline";
}
