using QaCommerceLab.Tests.Data;
using QaCommerceLab.Tests.Pages;
using QaCommerceLab.Tests.Support;
using Reqnroll;

namespace QaCommerceLab.Tests.StepDefinitions;

/// <summary>Who the scenario is about and what they already have: prepared through the API, not the UI.</summary>
[Binding]
public sealed class CustomerSteps(Customers customers, Shop shop)
{
    [Given("a visitor on the home page")]
    public void GivenAVisitor() => shop.OpenHome();

    [Given("a signed-in customer")]
    [Given("a signed-in customer on the home page")]
    public Task GivenASignedInCustomer() => customers.SignedIn();

    [Given("a signed-in customer with {int} × {string} in the bag")]
    public async Task GivenACustomerWithABag(int quantity, string product)
    {
        await customers.SignedIn();
        await customers.WithBag((quantity, Catalog.ByName(product)));
    }

    [Given("a signed-in customer with saved Visa and Mastercard cards")]
    public async Task GivenACustomerWithSavedCards()
    {
        await customers.SignedIn();
        await customers.WithSavedCard(Cards.Visa);
        await customers.WithSavedCard(Cards.Mastercard);
    }

    [Given("a signed-in customer with {string} on the wishlist")]
    public async Task GivenACustomerWithAWishlist(string product)
    {
        await customers.SignedIn();
        await customers.Wishlist(Catalog.ByName(product));
    }

    /// <summary>A bag written the way people write one: "2 × Shell Phone Case, 1 × Flow Yoga Mat".</summary>
    [Given(@"their bag holds (.+)")]
    public Task GivenTheirBagHolds(string bag) => customers.WithBag(ParseBag(bag));

    [Given("they have a saved Mastercard")]
    public Task GivenASavedMastercard() => customers.WithSavedCard(Cards.Mastercard);

    public static (int Quantity, ProductInfo Product)[] ParseBag(string bag) =>
        bag.Split(',', StringSplitOptions.TrimEntries)
            .Select(line => line.Split('×', 2, StringSplitOptions.TrimEntries))
            .Select(parts => (int.Parse(parts[0]), Catalog.ByName(parts[1])))
            .ToArray();
}
