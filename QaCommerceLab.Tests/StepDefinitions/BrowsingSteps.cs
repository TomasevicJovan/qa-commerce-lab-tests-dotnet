using QaCommerceLab.Tests.Data;
using QaCommerceLab.Tests.Pages;
using Reqnroll;
using Shouldly;

namespace QaCommerceLab.Tests.StepDefinitions;

[Binding]
public sealed class BrowsingSteps(Shop shop, ProductListPage list)
{
    [When("they search for {string}")]
    public void WhenTheySearchFor(string text)
    {
        shop.Header.Search(text);
        list.WaitForProducts();
    }

    [When("they open the {string} subcategory from the navigation")]
    public void WhenTheyOpenASubcategory(string subcategory)
    {
        shop.Header.OpenPopularSubcategory(subcategory);
        list.WaitForProducts();
    }

    [When("they open the Black Friday deals")]
    public void WhenTheyOpenBlackFriday()
    {
        shop.Header.OpenBlackFriday();
        list.WaitForProducts();
    }

    [When("they open the Free shipping products")]
    public void WhenTheyOpenFreeShipping()
    {
        shop.Header.OpenFreeShipping();
        list.WaitForProducts();
    }

    [When("they open all products sorted by {string}")]
    public void WhenTheyOpenAllProductsSortedBy(string sort)
    {
        list.Open();
        list.SortBy(sort);
    }

    [Then("the only result is {string}")]
    public void ThenTheOnlyResultIs(string product) => list.AllCards().Select(c => c.Name).ShouldBe([product]);

    [Then("they see that no products were found")]
    public void ThenNoProductsWereFound() => list.EmptyStateShouldRead(UiText.NoProductsFound);

    [Then("every product in the list belongs to {string}")]
    public void ThenEveryProductBelongsTo(string subcategory)
    {
        var cards = list.AllCards();
        cards.ShouldNotBeEmpty();
        cards.Where(c => c.Subcategory != subcategory).Select(c => $"{c.Name} ({c.Subcategory})").ShouldBeEmpty();
    }

    [Then("every product in the list shows a Black Friday price and its original price")]
    public void ThenEveryProductIsOnADeal()
    {
        var cards = list.AllCards();
        cards.ShouldNotBeEmpty();
        cards.Where(c => !c.IsSalePrice || c.ComparePrice is null || c.ComparePrice <= c.Price).Select(c => c.Name).ShouldBeEmpty();
    }

    [Then("every product in the list is marked {string}")]
    public void ThenEveryProductIsMarked(string mark)
    {
        var cards = list.AllCards();
        cards.ShouldNotBeEmpty();
        cards.Where(c => c.FreeShippingMark != mark).Select(c => c.Name).ShouldBeEmpty();
    }

    [Then("the prices on the page go from low to high")]
    public void ThenThePricesGoUp()
    {
        var prices = list.Grid.Facts().Select(c => c.Price).ToList();
        prices.ShouldNotBeEmpty();
        prices.ShouldBe(prices.Order().ToList());
    }
}
