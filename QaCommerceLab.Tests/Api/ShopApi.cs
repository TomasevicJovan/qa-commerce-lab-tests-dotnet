using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using QaCommerceLab.Tests.Support;

namespace QaCommerceLab.Tests.Api;

/// <summary>
/// Client for the lab's REST API, used to prepare state and to check results behind the UI.
/// One instance per scenario; it holds the signed-in customer's token once <see cref="SignIn"/> is called.
/// Methods that prepare state fail fast with the response body, so a broken precondition is not
/// reported as a failure of the behaviour under test.
/// </summary>
public sealed class ShopApi : IDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;

    public ShopApi()
    {
        _http = new HttpClient { BaseAddress = new Uri(Settings.Current.BaseUrl, "/api/"), Timeout = TimeSpan.FromSeconds(30) };
    }

    public Session? Session { get; private set; }

    // --- Account -------------------------------------------------------------------------------

    public async Task<Session> Register(Credentials credentials)
    {
        var session = await Send<Session>(HttpMethod.Post, "auth/register", credentials, HttpStatusCode.Created, HttpStatusCode.OK);
        Use(session);
        return session;
    }

    public async Task<Session> SignIn(string email, string password)
    {
        var session = await Send<Session>(HttpMethod.Post, "auth/login", new { email, password }, HttpStatusCode.OK);
        Use(session);
        return session;
    }

    // --- Stock ---------------------------------------------------------------------

    public async Task<int> Stock(string productId) =>
        (await Send<Dictionary<string, int>>(HttpMethod.Get, "inventory", null, HttpStatusCode.OK)).GetValueOrDefault(productId);

    // --- Bag, wishlist and cards ---------------------------------------------------------------

    public Task<List<CartItem>> Cart() => Send<List<CartItem>>(HttpMethod.Get, "cart", null, HttpStatusCode.OK);

    public Task SetCart(params CartItem[] items) => Send<JsonElement>(HttpMethod.Put, "cart", new { items }, HttpStatusCode.OK);

    public Task AddToWishlist(string productId) => Send<JsonElement>(HttpMethod.Put, $"wishlist/{productId}", null, HttpStatusCode.OK);

    public Task<List<SavedCard>> Cards() => Send<List<SavedCard>>(HttpMethod.Get, "cards", null, HttpStatusCode.OK);

    public Task<SavedCard> SaveCard(NewCard card) => Send<SavedCard>(HttpMethod.Post, "cards", card, HttpStatusCode.Created, HttpStatusCode.OK);

    // --- Orders ---------------------------------------------------------------------------------

    public Task<Order> PlaceOrder(CheckoutRequest request) =>
        Send<Order>(HttpMethod.Post, "orders", request, [HttpStatusCode.Accepted], ("Idempotency-Key", Guid.NewGuid().ToString()));

    public Task<Order> Order(string orderId) => Send<Order>(HttpMethod.Get, $"orders/{orderId}", null, HttpStatusCode.OK);

    public Task<List<Order>> Orders() => Send<List<Order>>(HttpMethod.Get, "orders", null, HttpStatusCode.OK);

    public Task<Payment> Payment(string orderId) => Send<Payment>(HttpMethod.Get, $"payments/{orderId}", null, HttpStatusCode.OK);

    /// <summary>Polls an order until it reaches a final state (orders are processed in the background).</summary>
    public async Task<Order> SettledOrder(string orderId)
    {
        var deadline = DateTime.UtcNow + Settings.Current.OrderTimeout;
        while (true)
        {
            var order = await Order(orderId);
            if (order.IsSettled) return order;
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Order {orderId} is still '{order.Status}' after {Settings.Current.OrderTimeout.TotalSeconds}s.");
            }

            await Task.Delay(250);
        }
    }

    // --- Lab switches (global, unauthenticated by design) ----------------------------------------

    public Task ConfigureLab(LabConfig config) => Send<JsonElement>(HttpMethod.Post, "lab/config", config, HttpStatusCode.OK);

    public Task RestoreLabDefaults() => Send<JsonElement>(HttpMethod.Post, "lab/defaults", new { }, HttpStatusCode.OK);

    public Task<JsonElement> LabStatus() => Send<JsonElement>(HttpMethod.Get, "lab/status", null, HttpStatusCode.OK);

    // --- Plumbing --------------------------------------------------------------------------------

    public void Dispose() => _http.Dispose();

    private void Use(Session session)
    {
        Session = session;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
    }

    private Task<T> Send<T>(HttpMethod method, string path, object? body, params HttpStatusCode[] expected) =>
        Send<T>(method, path, body, expected, []);

    private async Task<T> Send<T>(HttpMethod method, string path, object? body, HttpStatusCode[] expected, params (string Name, string Value)[] headers)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        foreach (var (name, value) in headers) request.Headers.Add(name, value);

        using var response = await _http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        if (!expected.Contains(response.StatusCode))
        {
            throw new InvalidOperationException(
                $"{method} /api/{path} returned {(int)response.StatusCode}, expected {string.Join(" or ", expected.Select(s => (int)s))}: {text}");
        }

        return string.IsNullOrEmpty(text) ? default! : JsonSerializer.Deserialize<T>(text, Json)!;
    }
}
