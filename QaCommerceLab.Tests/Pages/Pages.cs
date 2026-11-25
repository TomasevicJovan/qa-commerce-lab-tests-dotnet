using OpenQA.Selenium;
using QaCommerceLab.Tests.Api;
using QaCommerceLab.Tests.Pages.Components;
using QaCommerceLab.Tests.Support;

namespace QaCommerceLab.Tests.Pages;

/// <summary>Parts of the page that every route shares.</summary>
public sealed class Shop(Browser browser)
{
    public Header Header { get; } = new(browser);
    public AuthDialog AuthDialog { get; } = new(browser);
    public Notifications Notifications { get; } = new(browser);
    public BagDialog Bag { get; } = new(browser);

    public void OpenHome() => browser.Open("#home");

    /// <summary>The empty-state heading of a signed-out page, for example "Your next good thing starts here.".</summary>
    public void SignedOutMessageShouldBe(string heading) => browser.ShouldHaveText(By.CssSelector("#main .empty h2"), heading);
}

public sealed class ProductListPage(Browser browser)
{
    private static readonly By Products = By.Id("products");
    private static readonly By EmptyState = By.CssSelector("#products .empty");
    private static readonly By NextPage = By.Id("next");
    private static readonly By PageIndicator = By.Id("page-indicator");

    public ProductGrid Grid { get; } = new(browser, "products");

    public void Open(string query = "")
    {
        browser.Open(query.Length > 0 ? $"#shop?{query}" : "#shop");
        WaitForProducts();
    }

    public void WaitForProducts()
    {
        browser.Eventually(
            () => browser.WaitFor(Products).GetAttribute("aria-busy") != "true" && (browser.IsShown(By.CssSelector("#products article.product")) || browser.IsShown(EmptyState)),
            ready => ready,
            "the product list to load");
    }

    public void SortBy(string label)
    {
        browser.Select(By.Id("sort"), label);
        browser.Eventually(() => browser.CurrentHash, hash => hash.Contains("sort="), "the sort to be applied");
        WaitForProducts();
    }

    public void EmptyStateShouldRead(string text) => browser.ShouldHaveText(EmptyState, text);

    /// <summary>Card facts from every page of the list, following the Next button.</summary>
    public IReadOnlyList<CardFacts> AllCards()
    {
        WaitForProducts();
        var cards = new List<CardFacts>(Grid.Facts());
        while (browser.Read(() => browser.All(NextPage).FirstOrDefault() is { } next && next.GetAttribute("aria-disabled") != "true" && next.Enabled))
        {
            var indicator = browser.Text(PageIndicator);
            browser.Click(NextPage);
            browser.Eventually(() => browser.Text(PageIndicator), text => text != indicator, "the next page");
            WaitForProducts();
            cards.AddRange(Grid.Facts());
        }

        return cards;
    }
}

public sealed class WishlistPage(Browser browser)
{
    public ProductGrid Grid { get; } = new(browser, "wishlist-grid");

    public void Open() => browser.Open("#wishlist");

    public void ShouldList(string productName) =>
        browser.Eventually(() => Grid.Names(), names => names.Contains(productName), $"the wishlist to list {productName}");
}

public sealed class CheckoutPage(Browser browser)
{
    private static readonly By Form = By.Id("checkout-form");
    private static readonly By PlaceOrderButton = By.CssSelector("#checkout-form button.place-order");
    private static readonly By CouponStatus = By.Id("coupon-status");

    public Totals Totals { get; } = new(browser, "//*[@id='checkout-total']");

    public void Open()
    {
        browser.Open("#checkout");
        WaitForForm();
    }

    public void WaitForForm() => browser.WaitFor(PlaceOrderButton);

    public void FillAddress(Address address)
    {
        browser.Type(Input("name"), address.Name);
        browser.Type(Input("line1"), address.Line1);
        browser.Type(Input("city"), address.City);
        browser.Type(Input("postalCode"), address.PostalCode);
    }

    public void ClearAddress()
    {
        foreach (var field in new[] { "name", "line1", "city", "postalCode" }) browser.Type(Input(field), string.Empty);
    }

    public void ChooseSavedCard(string last4)
    {
        var radio = By.XPath($"//*[@id='checkout-form']//div[contains(@class,'payment-option')][.//strong[normalize-space()={XPath.Literal($"•••• {last4}")}]]//input[@type='radio']");
        browser.Click(radio);
    }

    public void ChooseNewCard() => browser.Click(By.CssSelector("#checkout-form input[name='payment'][value='new']"));

    public void ChooseWallet() => browser.Click(By.CssSelector("#checkout-form input[name='payment'][value='wallet']"));

    /// <summary>Types a new card the way a customer does: digits only, the form adds spaces and the slash.</summary>
    public void FillNewCard(string holder, string number, string expiry, string cvc, bool save = true)
    {
        browser.Type(Input("holder"), holder);
        browser.Type(Input("number"), new string(number.Where(char.IsDigit).ToArray()));
        browser.Type(Input("expiry"), new string(expiry.Where(char.IsDigit).ToArray()));
        browser.Type(Input("cvc"), cvc);
        var saveCard = browser.WaitFor(Input("saveCard"));
        if (saveCard.Selected != save) saveCard.Click();
    }

    public void EnterCoupon(string code)
    {
        browser.Type(Input("coupon"), code);
        browser.WaitFor(Input("coupon")).SendKeys(Keys.Tab);
    }

    public void CouponMessageShouldBe(string message) => browser.ShouldHaveText(CouponStatus, message);

    public void FieldErrorShouldBe(string field, string message) => browser.ShouldHaveText(By.CssSelector($"#checkout-form #error-{field}"), message);

    public void PlaceOrder() => browser.Click(PlaceOrderButton);

    public bool IsOpen => browser.IsShown(Form);

    public void SavedCardShouldBeOffered(string last4) =>
        browser.WaitFor(By.XPath($"//*[@id='checkout-form']//div[contains(@class,'payment-option')]//strong[normalize-space()={XPath.Literal($"•••• {last4}")}]"));

    private static By Input(string name) => By.CssSelector($"#checkout-form [name='{name}']");
}

public sealed class OrderPage(Browser browser)
{
    private static readonly By Banner = By.CssSelector("#order-banner [data-status]");

    public Totals Totals { get; } = new(browser, "//*[contains(@class,'order-layout')]//aside");

    public void Open(string orderId) => browser.Open($"#order/{orderId}");

    /// <summary>The order id from the address bar (#order/&lt;id&gt;), once the page is open.</summary>
    public string IdFromUrl() =>
        browser.Eventually(() => browser.CurrentHash, hash => hash.StartsWith("#order/"), "the order page to open")["#order/".Length..];

    /// <summary>Waits until the order has settled and the banner shows its final message.</summary>
    public void BannerShouldStartWith(string title) =>
        browser.Eventually(() => Browser.Clean(browser.All(Banner).FirstOrDefault()?.Text), text => text?.StartsWith(title) == true, $"the order banner to start with \"{title}\"", Settings.Current.OrderTimeout);

    public string Fact(string label) => browser.Text(By.XPath($"//*[contains(@class,'order-facts')]//dt[normalize-space()={XPath.Literal(label)}]/following-sibling::dd[1]"));

    public string Items() => browser.Text(By.CssSelector("#main .order-items"));

    public void PutProductsBackInBag() => browser.Click(By.Id("retry-order"));

    public void NotFoundMessageShouldBe(string text) => browser.ShouldHaveText(By.CssSelector("#main .inline-error"), text);
}

public sealed class OrdersPage(Browser browser)
{
    private static readonly By Orders = By.CssSelector("#main article[data-order-id]");

    public void Open() => browser.Open("#orders");

    public IReadOnlyList<string> OrderIds() =>
        browser.Eventually(() => browser.All(Orders).Select(o => o.GetAttribute("data-order-id")!).ToList(), ids => ids.Count > 0, "orders to be listed");

    public void StatusShouldBe(string orderId, string status) =>
        browser.ShouldHaveText(By.CssSelector($"#main [data-order-id='{orderId}'] [data-testid='order-status']"), status);
}
