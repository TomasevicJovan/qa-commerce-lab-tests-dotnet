using OpenQA.Selenium;
using QaCommerceLab.Tests.Support;

namespace QaCommerceLab.Tests.Pages.Components;

/// <summary>Header: search, account, wishlist and bag links, deal links and popular subcategories.</summary>
public sealed class Header(Browser browser)
{
    private static readonly By SearchInput = By.Id("search");
    private static readonly By AccountButton = By.Id("account");
    private static readonly By CreateAccountButton = By.Id("register-open");
    private static readonly By SignOutButton = By.Id("sign-out");
    private static readonly By BagButton = By.Id("cart-open");
    private static readonly By BagCountPill = By.Id("cart-count");
    private static readonly By WishlistCountPill = By.Id("wishlist-count");

    public void Search(string text)
    {
        browser.Type(SearchInput, text);
        browser.WaitFor(SearchInput).SendKeys(Keys.Enter);
    }

    public void OpenSignIn() => browser.Click(AccountButton);

    public void OpenRegistration() => browser.Click(CreateAccountButton);

    public void SignOut()
    {
        browser.Click(AccountButton);
        browser.Click(SignOutButton);
    }

    public void OpenBag() => browser.Click(BagButton);

    public void OpenBlackFriday() => browser.Click(By.CssSelector("[data-nav-link='black-friday']"));

    public void OpenFreeShipping() => browser.Click(By.CssSelector("[data-nav-link='free-shipping']"));

    public void OpenPopularSubcategory(string name) =>
        browser.Click(By.XPath($"//*[@id='popular-categories']//a[normalize-space()={XPath.Literal(name)}]"));

    /// <summary>"Sign in" when signed out, "Name ▾" when signed in.</summary>
    public void AccountShouldRead(string text) => browser.ShouldHaveText(AccountButton, text);

    public void BagCountShouldBe(int count) => browser.ShouldHaveText(BagCountPill, count.ToString());

    public void WishlistCountShouldBe(int count) => browser.ShouldHaveText(WishlistCountPill, count.ToString());
}

/// <summary>The sign-in and registration dialog.</summary>
public sealed class AuthDialog(Browser browser)
{
    private static readonly By Root = By.CssSelector("dialog#dialog[open] #auth-form");
    private static readonly By FormError = By.CssSelector("#auth-form .form-error");
    private static readonly By Submit = By.CssSelector("#auth-form button[type='submit']");

    public void ShouldBeOpen() => browser.WaitFor(Root);

    public void ShouldBeClosed() => browser.ShouldNotExist(Root);

    public void Register(string name, string email, string password, string confirm)
    {
        browser.Type(Field("name"), name);
        browser.Type(Field("email"), email);
        browser.Type(Field("password"), password);
        browser.Type(Field("confirm"), confirm);
        browser.Click(Submit);
    }

    public void SignIn(string email, string password)
    {
        browser.Type(Field("email"), email);
        browser.Type(Field("password"), password);
        browser.Click(Submit);
    }

    public void FieldErrorShouldBe(string field, string message) => browser.ShouldHaveText(By.Id($"error-{field}"), message);

    public void FormErrorShouldBe(string message) => browser.ShouldHaveText(FormError, message);

    private static By Field(string name) => By.CssSelector($"#auth-form [name='{name}']");
}

/// <summary>Toast notifications in the corner of the page.</summary>
public sealed class Notifications(Browser browser)
{
    private static readonly By All = By.CssSelector("#notifications .notification");

    /// <summary>Waits for a notification whose title or message contains <paramref name="text"/>.</summary>
    public void ShouldShow(string text) =>
        browser.Eventually(
            () => browser.All(All).Select(n => Browser.Clean(n.Text).Replace("\n", " ").Replace("×", string.Empty).Trim()).ToList(),
            shown => shown.Any(n => n.Contains(text)),
            $"a notification containing \"{text}\"");
}

/// <summary>Subtotal, discount, shipping and total, as shown in the bag, at checkout and on the order page.</summary>
public sealed class Totals(Browser browser, string scope)
{
    public string Value(string row) =>
        browser.Text(By.XPath($"{scope}//div[contains(@class,'totals')]/div[span[1][normalize-space()={XPath.Literal(row)}]]/span[last()]"));

    public void ShouldShow(string row, string value) =>
        browser.Eventually(() => Value(row), actual => actual == value, $"{row} to be {value}");
}

/// <summary>The bag dialog.</summary>
public sealed class BagDialog(Browser browser)
{
    private static readonly By Lines = By.CssSelector("dialog#dialog[open] .cart-item");
    private static readonly By Empty = By.CssSelector("dialog#dialog[open] .modal-body > .empty");
    private static readonly By ContinueToCheckout = By.Id("checkout-open");

    public Totals Totals { get; } = new(browser, "//dialog[@id='dialog']");

    public void ShouldBeOpen() => browser.WaitFor(By.CssSelector("dialog#dialog[open] .modal-head"));

    public bool IsOpen => browser.IsShown(By.XPath("//dialog[@id='dialog' and @open]//h2[normalize-space()='Your shopping bag']"));

    public void QuantityShouldBe(string productName, int quantity) =>
        browser.ShouldHaveText(By.XPath($"{Line(productName)}//div[@class='qty']/span"), quantity.ToString());

    public void LineCountShouldBe(int count) =>
        browser.Eventually(() => browser.All(Lines).Count, actual => actual == count, $"{count} line(s) in the bag");

    public void Increase(string productName) => browser.Click(By.CssSelector($"dialog#dialog button[aria-label={Css.Quote($"Add one {productName}")}]"));

    public void Decrease(string productName) => browser.Click(By.CssSelector($"dialog#dialog button[aria-label={Css.Quote($"Remove one {productName}")}]"));

    public void ShouldBeEmpty() => browser.WaitFor(Empty);

    public void ContinueToCheckoutPage() => browser.Click(ContinueToCheckout);

    private static string Line(string productName) =>
        $"//dialog[@id='dialog']//div[contains(@class,'cart-item')][.//h3[normalize-space()={XPath.Literal(productName)}]]";
}

/// <summary>A grid of product cards (product list, home rows, wishlist).</summary>
public sealed class ProductGrid(Browser browser, string gridId)
{
    private By Cards => By.CssSelector($"#{gridId} article.product");

    public void AddToBag(string productName) => browser.Click(CardPart(productName, "button.add-button"));

    public void ToggleWishlist(string productName) => browser.Click(CardPart(productName, "button.wish-button"));

    public void WishlistHeartShouldBeFilled(string productName) =>
        browser.Eventually(() => browser.WaitFor(CardPart(productName, "button.wish-button")).GetAttribute("aria-pressed"), v => v == "true", $"{productName} to be on the wishlist");

    public IReadOnlyList<CardFacts> Facts() => browser.Read(() =>
        browser.All(Cards).Select(card => new CardFacts(
            Name: card.FindElement(By.CssSelector("h3")).Text.Trim(),
            Subcategory: card.FindElement(By.CssSelector("a.category")).Text.Trim(),
            Price: Money.Parse(card.FindElement(By.CssSelector(".price")).Text),
            IsSalePrice: card.FindElement(By.CssSelector(".price")).GetAttribute("class")!.Contains("sale"),
            ComparePrice: card.FindElements(By.CssSelector(".compare-price")).Select(e => Money.Parse(e.Text)).FirstOrDefault() is var c and > 0 ? c : null,
            FreeShippingMark: card.FindElements(By.CssSelector(".free-shipping")).Select(e => e.Text.Trim()).FirstOrDefault())).ToList());

    public IReadOnlyList<string> Names() => Facts().Select(f => f.Name).ToList();

    private By CardPart(string productName, string part) => By.CssSelector($"#{gridId} article.product[aria-label={Css.Quote(productName)}] {part}");
}

public sealed record CardFacts(string Name, string Subcategory, int Price, bool IsSalePrice, int? ComparePrice, string? FreeShippingMark);
