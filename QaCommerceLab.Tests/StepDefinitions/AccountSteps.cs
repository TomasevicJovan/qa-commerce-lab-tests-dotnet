using OpenQA.Selenium;
using QaCommerceLab.Tests.Api;
using QaCommerceLab.Tests.Data;
using QaCommerceLab.Tests.Pages;
using QaCommerceLab.Tests.Support;
using Reqnroll;

namespace QaCommerceLab.Tests.StepDefinitions;

[Binding]
public sealed class AccountSteps(Shop shop, Browser browser, Customers customers, ShopApi api, RandomData random, ScenarioState state)
{
    private string? _demoName;

    /// <summary>Registers through the form; the API client then signs in too, so later steps can check the account's data.</summary>
    [When("they create an account with a new email address")]
    public async Task WhenTheyCreateAnAccount()
    {
        state.Credentials = customers.NewCredentials();
        shop.Header.OpenRegistration();
        shop.AuthDialog.Register(state.Credentials.Name, state.Credentials.Email, state.Credentials.Password, state.Credentials.Password);
        shop.AuthDialog.ShouldBeClosed();
        await api.SignIn(state.Credentials.Email, state.Credentials.Password);
    }

    /// <summary>One invalid input at a time; the rest of the form is valid.</summary>
    [When("they try to create an account with {}")]
    public async Task WhenTheyTryToCreateAnAccountWith(string problem)
    {
        var valid = customers.NewCredentials();
        var (email, password, confirm) = problem switch
        {
            "a password of 7 characters" => (valid.Email, random.Password(7), (string?)null),
            "passwords that do not match" => (valid.Email, valid.Password, random.Password()),
            "an email without a domain" => (valid.Email.Split('@')[0] + "@", valid.Password, null),
            "an email that is registered" => ((await customers.Another()).Session!.User.Email, valid.Password, null),
            _ => throw new ArgumentException($"Unknown registration problem '{problem}'."),
        };

        shop.Header.OpenRegistration();
        shop.AuthDialog.Register(valid.Name, email, password, confirm ?? password);
    }

    [When("they sign in with the demo account")]
    public async Task WhenTheySignInWithTheDemoAccount()
    {
        _demoName = (await api.SignIn(Settings.Current.DemoEmail, Settings.Current.DemoPassword)).User.Name;
        shop.Header.OpenSignIn();
        shop.AuthDialog.SignIn(Settings.Current.DemoEmail, Settings.Current.DemoPassword);
    }

    [When("they sign in to the demo account with a wrong password")]
    public void WhenTheySignInWithAWrongPassword()
    {
        shop.Header.OpenSignIn();
        shop.AuthDialog.SignIn(Settings.Current.DemoEmail, random.Password());
    }

    [When("they sign out")]
    public void WhenTheySignOut() => shop.Header.SignOut();

    [When("they sign out and sign in again")]
    public void WhenTheySignOutAndInAgain()
    {
        var credentials = state.Credentials!;
        shop.Header.SignOut();
        shop.Header.AccountShouldRead(UiText.SignIn);
        shop.Header.OpenSignIn();
        shop.AuthDialog.SignIn(credentials.Email, credentials.Password);
        shop.Header.AccountShouldRead(UiText.AccountButton(credentials.Name));
    }

    [Then("they are signed in under their name")]
    public void ThenSignedInUnderTheirName() => shop.Header.AccountShouldRead(UiText.AccountButton(state.Credentials!.Name));

    [Then("they are signed in under the demo account name")]
    public void ThenSignedInAsDemo() => shop.Header.AccountShouldRead(UiText.AccountButton(_demoName!));

    [Then("they are signed out")]
    [Then("they are still signed out")]
    public void ThenSignedOut() => shop.Header.AccountShouldRead(UiText.SignIn);

    [Then("the sign-in form shows {string}")]
    public void ThenTheSignInFormShows(string message) => shop.AuthDialog.FormErrorShouldBe(message);

    [Then("the sign-in form is open")]
    public void ThenTheSignInFormIsOpen() => shop.AuthDialog.ShouldBeOpen();

    /// <summary>Field errors share one pattern (#error-&lt;field&gt;) in the sign-in, registration and checkout forms.</summary>
    [Then("the {word} field shows {string}")]
    public void ThenTheFieldShows(string field, string message) => browser.ShouldHaveText(By.Id($"error-{field}"), message);

    [Then("they see the notification {string}")]
    public void ThenTheySeeTheNotification(string text) => shop.Notifications.ShouldShow(text);
}
