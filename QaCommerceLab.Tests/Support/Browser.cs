using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace QaCommerceLab.Tests.Support;

/// <summary>
/// One Chrome window per scenario, plus the waiting helpers every page object uses.
/// The shop renders asynchronously, so nothing reads the page once: every read and every assertion
/// polls until the expected state appears or the wait runs out (no Thread.Sleep anywhere).
/// </summary>
public sealed class Browser : IDisposable
{
    private static readonly Type[] Transient = [typeof(NoSuchElementException), typeof(StaleElementReferenceException), typeof(ElementNotInteractableException), typeof(ElementClickInterceptedException)];

    public Browser()
    {
        var settings = Settings.Current;
        var options = new ChromeOptions();
        if (settings.Headless) options.AddArgument("--headless=new");
        options.AddArguments(
            $"--window-size={settings.WindowWidth},{settings.WindowHeight}",
            "--no-sandbox",
            "--disable-dev-shm-usage",
            "--disable-search-engine-choice-screen");
        Driver = new ChromeDriver(options);
    }

    public IWebDriver Driver { get; }

    /// <summary>
    /// Opens a route of the single-page shop with a fresh page load. Going to another #hash on the same
    /// document would only re-render with the data the page already holds, not what the API just prepared.
    /// </summary>
    public void Open(string hash)
    {
        var target = new Uri(Settings.Current.BaseUrl, "/" + hash);
        var sameDocument = Uri.TryCreate(Driver.Url, UriKind.Absolute, out var current) &&
                           current.GetLeftPart(UriPartial.Path) == target.GetLeftPart(UriPartial.Path);
        Driver.Navigate().GoToUrl(target);
        if (sameDocument) Driver.Navigate().Refresh();
        WaitUntilReady();
    }

    /// <summary>
    /// Signs the browser in with an API token. The shop keeps its token in sessionStorage,
    /// so the token is stored there and the page is loaded again.
    /// </summary>
    public void SignInWithToken(string token)
    {
        Driver.Navigate().GoToUrl(Settings.Current.BaseUrl);
        ((IJavaScriptExecutor)Driver).ExecuteScript("sessionStorage.setItem('qa-token', arguments[0]);", token);
        Driver.Navigate().Refresh();
        WaitUntilReady();
    }

    public void Reload()
    {
        Driver.Navigate().Refresh();
        WaitUntilReady();
    }

    public string CurrentHash => new Uri(Driver.Url).Fragment;

    /// <summary>The app has finished booting (session, bag and wishlist loaded) when the promotion bar starts.</summary>
    public void WaitUntilReady() => WaitFor(By.CssSelector("#promo-message .promo-count"), TimeSpan.FromSeconds(20));

    public IWebElement WaitFor(By by, TimeSpan? timeout = null) =>
        Until(d => d.FindElements(by).FirstOrDefault(e => e.Displayed), $"{by} to be visible", timeout);

    public bool IsShown(By by) => Read(() => Driver.FindElements(by).Any(e => e.Displayed));

    /// <summary>
    /// Reads the page once, starting again if the shop re-renders the part being read in the middle
    /// of reading it (a stale element). For reads that should not wait for a particular value.
    /// </summary>
    public T Read<T>(Func<T> read) => Until(_ => new Box<T>(read()), "the page to stop re-rendering").Value;

    public IReadOnlyList<IWebElement> All(By by) => Driver.FindElements(by);

    public void Click(By by) => Until(_ =>
    {
        var element = WaitFor(by);
        element.Click();
        return true;
    }, $"{by} to be clickable");

    /// <summary>Replaces the value of an input; retries while a dialog or a re-render still covers it.</summary>
    public void Type(By by, string text) => Until(_ =>
    {
        var element = WaitFor(by);
        element.Clear();
        if (text.Length > 0) element.SendKeys(text);
        return true;
    }, $"{by} to accept typing");

    public void Select(By by, string optionText) => new SelectElement(WaitFor(by)).SelectByText(optionText);

    public string Text(By by) => Clean(WaitFor(by).Text);

    /// <summary>Waits until the text of <paramref name="by"/> equals <paramref name="expected"/>; on timeout reports what was there.</summary>
    public void ShouldHaveText(By by, string expected) =>
        Eventually(() => Clean(Driver.FindElements(by).FirstOrDefault()?.Text), actual => actual == expected, $"text of {by} to be \"{expected}\"");

    public void ShouldContainText(By by, string expected) =>
        Eventually(() => Clean(Driver.FindElements(by).FirstOrDefault()?.Text), actual => actual?.Contains(expected) == true, $"text of {by} to contain \"{expected}\"");

    public void ShouldNotExist(By by) =>
        Eventually(() => Driver.FindElements(by).Count, count => count == 0, $"{by} to disappear");

    /// <summary>
    /// Polls <paramref name="read"/> until <paramref name="isExpected"/> holds. Fails with the last value read,
    /// so a timeout says what the page showed instead of only that it timed out.
    /// </summary>
    public T Eventually<T>(Func<T> read, Func<T, bool> isExpected, string description, TimeSpan? timeout = null)
    {
        T last = default!;
        try
        {
            Until(_ =>
            {
                last = read();
                return isExpected(last);
            }, description, timeout);
        }
        catch (WebDriverTimeoutException)
        {
            throw new Shouldly.ShouldAssertException($"Expected {description}, but it was: {Describe(last)}");
        }

        return last;
    }

    public object? Script(string script, params object[] args) => ((IJavaScriptExecutor)Driver).ExecuteScript(script, args);

    public byte[] Screenshot() => ((ITakesScreenshot)Driver).GetScreenshot().AsByteArray;

    public void Dispose() => Driver.Quit();

    private TResult Until<TResult>(Func<IWebDriver, TResult?> condition, string description, TimeSpan? timeout = null)
    {
        var wait = new WebDriverWait(Driver, timeout ?? Settings.Current.Wait)
        {
            PollingInterval = TimeSpan.FromMilliseconds(100),
            Message = $"Waited for {description}",
        };
        wait.IgnoreExceptionTypes(Transient);
        return wait.Until(d =>
        {
            var result = condition(d);
            return result is false ? default : result;
        })!;
    }

    /// <summary>
    /// Element text with line breaks the same on every system: ChromeDriver on Windows returns "\r\n", on Linux "\n".
    /// </summary>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(text))]
    public static string? Clean(string? text) => text?.Replace("\r\n", "\n").Trim();

    private sealed record Box<T>(T Value);

    private static string Describe(object? value) => value switch
    {
        null => "nothing",
        string s => $"\"{s}\"",
        System.Collections.IEnumerable items => $"[{string.Join(", ", items.Cast<object?>().Select(Describe))}]",
        _ => value.ToString() ?? "nothing",
    };
}
