using QaCommerceLab.Tests.Api;
using QaCommerceLab.Tests.Data;
using QaCommerceLab.Tests.Pages;
using QaCommerceLab.Tests.Support;
using Reqnroll;
using Shouldly;

namespace QaCommerceLab.Tests.StepDefinitions;

[Binding]
public sealed class OrderSteps(Customers customers, CheckoutPage checkout, OrderPage orderPage, OrdersPage ordersPage, ShopApi api, RandomData random, ScenarioState state)
{
    [Given("a signed-in customer who paid for {int} × {string} with the test wallet")]
    public async Task GivenACustomerWhoPaid(int quantity, string product)
    {
        await customers.SignedIn();
        await customers.PaidOrder(ApiValues.TestWallet, (quantity, Catalog.ByName(product)));
    }

    [Given("then paid for {int} × {string} with the test wallet")]
    public Task GivenThenPaidFor(int quantity, string product) => customers.PaidOrder(ApiValues.TestWallet, (quantity, Catalog.ByName(product)));

    [Given("a signed-in customer at checkout with the last {string}")]
    public async Task GivenACustomerAtCheckoutWithTheLast(string productName)
    {
        var product = Catalog.ByName(productName);
        await customers.SignedIn();
        await customers.WithSavedCard(Cards.Visa);
        state.StockBefore[product.Id] = await api.Stock(product.Id);
        state.StockBefore[product.Id].ShouldBe(1,
            $"{product.Name} must have exactly one unit left. Earlier runs changed the stock; reset the lab data (see README).");
        await customers.WithBag((1, product));
        checkout.Open();
    }

    [Given("another customer buys the last {string} first")]
    public async Task GivenAnotherCustomerBuysTheLastOne(string productName)
    {
        using var other = await customers.Another();
        var product = Catalog.ByName(productName);
        await other.SetCart(new CartItem(product.Id, 1));
        var order = await customers.PlaceAndSettle(other, ApiValues.TestWallet, [(1, product)]);
        order.Status.ShouldBe(ApiValues.Confirmed);
    }

    [When("they place the order with their saved Visa")]
    public void WhenTheyPlaceWithTheSavedVisa()
    {
        checkout.ChooseSavedCard(Cards.Visa.Last4);
        checkout.PlaceOrder();
        state.OrderIds.Add(orderPage.IdFromUrl());
    }

    [Given("a signed-in customer whose order for {int} × {string} was declined")]
    public async Task GivenADeclinedOrder(int quantity, string productName)
    {
        await customers.SignedIn();
        var product = Catalog.ByName(productName);
        await customers.EnsureStock(product, quantity);
        await api.SetCart(new CartItem(product.Id, quantity));
        var order = await customers.PlaceAndSettle(api, ApiValues.Card, [(quantity, product)],
            new NewCard(random.Cardholder(), Cards.Declined.Digits, Cards.ValidExpiry, Cards.Declined.Cvc));
        order.Status.ShouldBe(ApiValues.Cancelled);
        state.OrderIds.Add(order.Id);
    }

    [Given("another customer has placed an order")]
    public async Task GivenAnotherCustomersOrder()
    {
        using var other = await customers.Another();
        var product = Catalog.PureToothbrush;
        await customers.EnsureStock(product, 1);
        await other.SetCart(new CartItem(product.Id, 1));
        state.OtherCustomersOrderId = (await customers.PlaceAndSettle(other, ApiValues.TestWallet, [(1, product)])).Id;
    }

    [Given("the bank declines every payment")]
    public Task GivenTheBankDeclines() => api.ConfigureLab(new LabConfig { PaymentMode = ApiValues.BankDeclines });

    [When("they open the order")]
    public void WhenTheyOpenTheOrder() => orderPage.Open(state.LastOrderId);

    [When("they open My orders")]
    public void WhenTheyOpenMyOrders() => ordersPage.Open();

    [When("they open the other customer's order")]
    public void WhenTheyOpenTheOtherCustomersOrder() => orderPage.Open(state.OtherCustomersOrderId!);

    [When("they open the order and put the products back in the bag")]
    public void WhenTheyOpenTheOrderAndRestoreTheBag()
    {
        orderPage.Open(state.LastOrderId);
        orderPage.PutProductsBackInBag();
    }

    [When("they put the products back in the bag")]
    public void WhenTheyPutTheProductsBack() => orderPage.PutProductsBackInBag();

    [Then("the order page shows {string}")]
    public void ThenTheOrderPageShows(string title) => orderPage.BannerShouldStartWith(title);

    [Then("it lists {int} × {string}")]
    public void ThenItLists(int quantity, string product) => orderPage.Items().ShouldContain(UiText.OrderLine(quantity, product));

    [Then("it shows the delivery address and {string} as the payment")]
    public async Task ThenItShowsTheAddressAndPayment(string payment)
    {
        var order = await api.Order(state.LastOrderId);
        orderPage.Fact(UiText.DeliverToFact).ShouldBe(UiText.DeliveryAddress(order.Address));
        orderPage.Fact(UiText.PaymentFact).ShouldBe(payment);
    }

    [Then("the orders are listed newest first")]
    public void ThenTheOrdersAreNewestFirst() => ordersPage.OrderIds().ShouldBe(Enumerable.Reverse(state.OrderIds).ToList());

    [Then("My orders shows the order as confirmed")]
    public void ThenMyOrdersShowsConfirmed()
    {
        ordersPage.Open();
        ordersPage.StatusShouldBe(state.LastOrderId, UiText.OrderConfirmedStatus);
    }

    [Then("the payment for the order has failed")]
    public async Task ThenThePaymentFailed() => (await api.Payment(state.LastOrderId)).Status.ShouldBe(ApiValues.PaymentFailed);

    /// <summary>An order that lost the stock race is rejected before payment: no payment is ever attempted.</summary>
    [Then("the payment for the order was never taken")]
    public async Task ThenThePaymentWasNeverTaken()
    {
        (await api.Order(state.LastOrderId)).Status.ShouldBe(ApiValues.Rejected);
        (await api.Payment(state.LastOrderId)).Status.ShouldBe(ApiValues.PaymentPending);
    }

    [Then("they see {string}")]
    public void ThenTheySee(string message) => orderPage.NotFoundMessageShouldBe(message);

    [Then("the stock of {string} is the same as before the order")]
    public async Task ThenTheStockIsTheSame(string productName)
    {
        var product = Catalog.ByName(productName);
        var expected = state.StockBefore[product.Id];
        var deadline = DateTime.UtcNow + Settings.Current.OrderTimeout;
        var stock = await api.Stock(product.Id);
        while (stock != expected && DateTime.UtcNow < deadline)
        {
            await Task.Delay(250);
            stock = await api.Stock(product.Id);
        }

        stock.ShouldBe(expected, $"stock of {product.Name} after the failed payment released its reservation");
    }
}
