using QaCommerceLab.Tests.Api;
using QaCommerceLab.Tests.Data;
using QaCommerceLab.Tests.Pages;
using QaCommerceLab.Tests.Support;
using Reqnroll;
using Shouldly;

namespace QaCommerceLab.Tests.StepDefinitions;

[Binding]
public sealed class BagSteps(Shop shop, ProductListPage list, WishlistPage wishlist, ShopApi api)
{
    /// <summary>Finds the product by searching for its name, the way a customer would, and adds it.</summary>
    [When("they add {string} to the bag from the product list")]
    public void WhenTheyAddFromTheList(string product)
    {
        if (!list.Grid.Names().Contains(product))
        {
            list.Open($"q={Uri.EscapeDataString(product)}");
        }

        list.Grid.AddToBag(product);
    }

    [When("they open the bag")]
    public void WhenTheyOpenTheBag() => shop.Header.OpenBag();

    [When("they increase {string} by one in the bag")]
    public void WhenTheyIncrease(string product)
    {
        shop.Header.OpenBag();
        shop.Bag.Increase(product);
    }

    [When("they decrease {string} by one in the bag")]
    public void WhenTheyDecrease(string product)
    {
        shop.Header.OpenBag();
        shop.Bag.Decrease(product);
    }

    [When("they continue from the bag to checkout")]
    public void WhenTheyContinueToCheckout()
    {
        shop.Header.OpenBag();
        shop.Bag.ContinueToCheckoutPage();
    }

    [Then("the bag count shows {int}")]
    public void ThenTheBagCountShows(int count) => shop.Header.BagCountShouldBe(count);

    [Then("the bag lists {int} × {string}")]
    public void ThenTheBagLists(int quantity, string product)
    {
        if (!shop.Bag.IsOpen) shop.Header.OpenBag();
        shop.Bag.QuantityShouldBe(product, quantity);
    }

    [Then("the bag subtotal is {word}")]
    public void ThenTheBagSubtotalIs(string amount) => shop.Bag.Totals.ShouldShow(UiText.Subtotal, amount);

    [Then("the bag is empty")]
    public void ThenTheBagIsEmpty()
    {
        shop.Bag.ShouldBeEmpty();
        shop.Header.BagCountShouldBe(0);
    }

    [Then("the saved bag still has {int} × {string}")]
    public async Task ThenTheSavedBagStillHas(int quantity, string product) =>
        (await api.Cart()).ShouldBe([new CartItem(Catalog.ByName(product).Id, quantity)]);

    [When("they save {string} to the wishlist from the product list")]
    [When("they try to save {string} to the wishlist from the product list")]
    public void WhenTheySaveToTheWishlist(string product)
    {
        list.Open($"q={Uri.EscapeDataString(product)}");
        list.Grid.ToggleWishlist(product);
    }

    [When("they add {string} to the bag from the wishlist page")]
    public void WhenTheyAddFromTheWishlist(string product)
    {
        wishlist.Open();
        wishlist.Grid.AddToBag(product);
    }

    [Then("the wishlist page lists {string}")]
    public void ThenTheWishlistPageLists(string product)
    {
        list.Grid.WishlistHeartShouldBeFilled(product);
        wishlist.Open();
        wishlist.ShouldList(product);
    }

    [Then("the wishlist count shows {int}")]
    public void ThenTheWishlistCountShows(int count) => shop.Header.WishlistCountShouldBe(count);
}
