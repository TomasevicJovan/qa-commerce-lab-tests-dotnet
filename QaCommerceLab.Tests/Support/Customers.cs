using QaCommerceLab.Tests.Api;
using QaCommerceLab.Tests.Data;

namespace QaCommerceLab.Tests.Support;

/// <summary>
/// Prepares customers and their state through the API, then hands the browser over signed in.
/// Every scenario gets its own new customer, so scenarios do not depend on each other and can run in parallel.
/// </summary>
public sealed class Customers(ShopApi api, Browser browser, RandomData random, ScenarioState state)
{
    public Credentials NewCredentials()
    {
        var name = random.FullName();
        return new Credentials(name, random.Email(name), random.Password());
    }

    /// <summary>Registers a new customer through the API and signs the browser in as that customer.</summary>
    public async Task SignedIn()
    {
        state.Credentials = NewCredentials();
        var session = await api.Register(state.Credentials);
        browser.SignInWithToken(session.Token);
    }

    /// <summary>Replaces the bag through the API and reloads, so the storefront shows it.</summary>
    public async Task WithBag(params (int Quantity, ProductInfo Product)[] lines)
    {
        foreach (var (quantity, product) in lines)
        {
            await EnsureStock(product, quantity);
            state.StockBefore[product.Id] = await api.Stock(product.Id);
        }

        await api.SetCart(lines.Select(l => new CartItem(l.Product.Id, l.Quantity)).ToArray());
        browser.Reload();
    }

    public async Task Wishlist(ProductInfo product)
    {
        await api.AddToWishlist(product.Id);
        browser.Reload();
    }

    public async Task<SavedCard> WithSavedCard(TestCard card) =>
        await api.SaveCard(new NewCard(random.Cardholder(), card.Digits, Cards.ValidExpiry, card.Cvc));

    /// <summary>Places an order through the API, waits until it settles and remembers it.</summary>
    public async Task<Order> PaidOrder(string paymentMethod, params (int Quantity, ProductInfo Product)[] lines)
    {
        foreach (var (quantity, product) in lines) await EnsureStock(product, quantity);
        await api.SetCart(lines.Select(l => new CartItem(l.Product.Id, l.Quantity)).ToArray());
        var order = await PlaceAndSettle(api, paymentMethod, lines);
        state.OrderIds.Add(order.Id);
        return order;
    }

    /// <summary>A second, unrelated customer with an API client of their own.</summary>
    public async Task<ShopApi> Another()
    {
        var other = new ShopApi();
        await other.Register(NewCredentials());
        return other;
    }

    /// <summary>
    /// The lab cannot restock: once a product is sold out on a local lab, only a reset brings it back.
    /// This stops the scenario with that advice instead of failing later for an unrelated reason.
    /// </summary>
    public async Task EnsureStock(ProductInfo product, int quantity)
    {
        var stock = await api.Stock(product.Id);
        if (stock < quantity)
        {
            throw new InvalidOperationException(
                $"The lab has {stock} unit(s) of {product.Name}, this scenario needs {quantity}. " +
                "Earlier runs bought the stock; reset the lab data (see README) and run again.");
        }
    }

    public async Task<Order> PlaceAndSettle(ShopApi client, string paymentMethod, (int Quantity, ProductInfo Product)[] lines, NewCard? card = null)
    {
        var totals = Pricing.Calculate(lines.Select(l => (l.Product, l.Quantity)), coupon: null, cardBrand: card is null ? null : Pricing.BrandOf(card.Number));
        var placed = await client.PlaceOrder(new CheckoutRequest
        {
            Address = random.Address(),
            PaymentMethod = paymentMethod,
            Card = card,
            SaveCard = card is null ? null : false,
            ExpectedTotal = totals.Total,
        });
        return await client.SettledOrder(placed.Id);
    }
}
