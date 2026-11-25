using QaCommerceLab.Tests.Api;
using QaCommerceLab.Tests.Data;
using QaCommerceLab.Tests.Pages;
using QaCommerceLab.Tests.Support;
using Reqnroll;
using Shouldly;

namespace QaCommerceLab.Tests.StepDefinitions;

[Binding]
public sealed class CheckoutSteps(CheckoutPage checkout, OrderPage orderPage, ShopApi api, Browser browser, RandomData random, ScenarioState state)
{
    [When("they check out with a new Visa card and keep \"Save this card\" ticked")]
    [When("they check out with a new Visa card")]
    public void WhenTheyCheckOutWithANewVisa() => PayWithNewCard(Cards.Visa);

    [When("they check out with the declined test card")]
    public void WhenTheyCheckOutWithTheDeclinedCard() => PayWithNewCard(Cards.Declined, save: false);

    [When("they check out with the saved Mastercard")]
    public void WhenTheyCheckOutWithTheSavedMastercard()
    {
        OpenWithAddress();
        checkout.ChooseSavedCard(Cards.Mastercard.Last4);
        PlaceOrder();
    }

    [When("they check out with the test wallet")]
    public void WhenTheyCheckOutWithTheWallet()
    {
        OpenWithAddress();
        checkout.ChooseWallet();
        PlaceOrder();
    }

    [When(@"they go to checkout and pay with the saved (Visa|Mastercard) using coupon ""(.*)""")]
    public void WhenTheyGoToCheckoutWithASavedCard(string card, string coupon)
    {
        OpenWithAddress();
        checkout.ChooseSavedCard(Cards.ByName(card).Last4);
        if (coupon.Length > 0) checkout.EnterCoupon(coupon);
    }

    [When("they place the order")]
    public void WhenTheyPlaceTheOrder() => PlaceOrder();

    [When("they try to check out with a new card where {}")]
    public void WhenTheyTryANewCardWhere(string problem)
    {
        var card = Cards.Visa;
        var (holder, number, expiry) = problem switch
        {
            "the number fails the check" => (random.Cardholder(), card.Digits[..^1] + (card.Digits[^1] == '0' ? '1' : '0'), Cards.ValidExpiry),
            "the card expired last month" => (random.Cardholder(), card.Digits, Cards.ExpiryIn(-1)),
            "the cardholder name is empty" => (string.Empty, card.Digits, Cards.ValidExpiry),
            _ => throw new ArgumentException($"Unknown card problem '{problem}'."),
        };

        OpenWithAddress();
        checkout.ChooseNewCard();
        checkout.FillNewCard(holder, number, expiry, card.Cvc);
        checkout.PlaceOrder();
    }

    [When("they enter the coupon {string} at checkout")]
    public void WhenTheyEnterACoupon(string code)
    {
        OpenWithAddress();
        checkout.EnterCoupon(code);
    }

    [When("they try to check out with an empty delivery address")]
    public void WhenTheyTryWithAnEmptyAddress()
    {
        checkout.Open();
        checkout.ClearAddress();
        checkout.ChooseWallet();
        checkout.PlaceOrder();
    }

    [Then("the coupon message says {string}")]
    public void ThenTheCouponMessageSays(string message) => checkout.CouponMessageShouldBe(message);

    [Then("the delivery address fields show:")]
    public void ThenTheAddressFieldsShow(DataTable errors)
    {
        foreach (var row in errors.Rows) checkout.FieldErrorShouldBe(row["field"], row["message"]);
    }

    [Then(@"the order summary shows subtotal (\S+), discount (\S+), shipping (.+) and total (\S+)")]
    public void ThenTheOrderSummaryShows(string subtotal, string discount, string shipping, string total)
    {
        checkout.Totals.ShouldShow(UiText.Subtotal, subtotal);
        checkout.Totals.ShouldShow(UiText.Discount, $"−{discount}");
        checkout.Totals.ShouldShow(UiText.Shipping, shipping);
        checkout.Totals.ShouldShow(UiText.Total, total);
    }

    [Then("no order is placed")]
    public async Task ThenNoOrderIsPlaced()
    {
        browser.CurrentHash.ShouldBe("#checkout");
        (await api.Orders()).ShouldBeEmpty();
    }

    [Then("the order is confirmed and paid with {string}")]
    public async Task ThenTheOrderIsConfirmedAndPaidWith(string payment)
    {
        orderPage.BannerShouldStartWith(UiText.PaymentSuccessful);
        orderPage.Fact(UiText.PaymentFact).ShouldBe(payment);
        (await api.Order(state.LastOrderId)).Status.ShouldBe(ApiValues.Confirmed);
    }

    /// <summary>The banner shows the amount; the API confirms it is the amount the order was charged.</summary>
    [Then("the order is confirmed for {word}")]
    public async Task ThenTheOrderIsConfirmedFor(string total)
    {
        orderPage.BannerShouldStartWith(UiText.PaymentSuccessful);
        var order = await api.Order(state.LastOrderId);
        order.Status.ShouldBe(ApiValues.Confirmed);
        Money.Format(order.Total).ShouldBe(total);
        (await api.Payment(order.Id)).Amount.ShouldBe(order.Total);
    }

    [Then("the Visa card is saved for next time")]
    public async Task ThenTheVisaCardIsSaved() =>
        (await api.Cards()).Select(c => (c.Brand, c.Last4)).ShouldContain((Cards.Visa.Brand, Cards.Visa.Last4));

    [Then("the Visa card is offered as a saved card at the next checkout")]
    public async Task ThenTheVisaIsOfferedNextTime()
    {
        var product = Catalog.TrailBottle;
        await api.SetCart(new CartItem(product.Id, 1));
        checkout.Open();
        checkout.SavedCardShouldBeOffered(Cards.Visa.Last4);
    }

    private void OpenWithAddress()
    {
        if (!checkout.IsOpen) checkout.Open();
        checkout.WaitForForm();
        checkout.FillAddress(random.Address());
    }

    private void PayWithNewCard(TestCard card, bool save = true)
    {
        OpenWithAddress();
        checkout.ChooseNewCard();
        checkout.FillNewCard(random.Cardholder(), card.Number, Cards.ValidExpiry, card.Cvc, save);
        PlaceOrder();
    }

    /// <summary>Places the order and remembers its id once the order page opens.</summary>
    private void PlaceOrder()
    {
        checkout.PlaceOrder();
        state.OrderIds.Add(orderPage.IdFromUrl());
    }
}
