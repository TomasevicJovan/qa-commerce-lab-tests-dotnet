using QaCommerceLab.Tests.Api;

namespace QaCommerceLab.Tests.Support;

/// <summary>
/// What one scenario knows about its own customer and orders. Reqnroll creates one per scenario and
/// passes it to every step class that asks for it, so scenarios running in parallel never share state.
/// </summary>
public sealed class ScenarioState
{
    public Credentials? Credentials { get; set; }

    public List<string> OrderIds { get; } = [];

    public string LastOrderId => OrderIds.Count > 0 ? OrderIds[^1] : throw new InvalidOperationException("No order was placed in this scenario.");

    /// <summary>An order that belongs to a different customer.</summary>
    public string? OtherCustomersOrderId { get; set; }

    public Dictionary<string, int> StockBefore { get; } = [];
}
