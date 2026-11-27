using NUnit.Framework;
using QaCommerceLab.Tests.Api;
using Reqnroll;
using Reqnroll.BoDi;

namespace QaCommerceLab.Tests.Support;

[Binding]
public sealed class Hooks(IObjectContainer container, ScenarioContext scenario, IReqnrollOutputHelper output)
{
    /// <summary>Fails the whole run early, with one clear message, when the lab is not up.</summary>
    [BeforeTestRun]
    public static async Task CheckLabAndPrintSeed()
    {
        await TestContext.Progress.WriteLineAsync($"Test data seed: {RandomData.RunSeed} (set TEST_SEED={RandomData.RunSeed} to repeat this run's data)");
        using var api = new ShopApi();
        try
        {
            await api.LabStatus();
            await api.RestoreLabDefaults();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                $"qa-commerce-lab is not reachable at {Settings.Current.BaseUrl}. Start it first (see README). {e.Message}", e);
        }
    }

    /// <summary>Every scenario gets its own repeatable random data, seeded from the run seed and its title.</summary>
    [BeforeScenario(Order = 0)]
    public void SeedRandomData()
    {
        var data = new RandomData(scenario.ScenarioInfo.Title + string.Join(",", scenario.ScenarioInfo.Arguments.Values.Cast<object>()));
        container.RegisterInstanceAs(data);
        output.WriteLine($"Test data seed: run {RandomData.RunSeed}, scenario {data.Seed}");
    }

    /// <summary>Every scenario drives the storefront, so each one starts its own browser window.</summary>
    [BeforeScenario(Order = 1)]
    public void StartBrowser() => container.RegisterInstanceAs(new Browser(), dispose: true);

    /// <summary>Attaches a screenshot of the browser window to a failed scenario.</summary>
    [AfterScenario(Order = 0)]
    public void ScreenshotOnFailure()
    {
        if (scenario.TestError is null || !container.IsRegistered<Browser>()) return;
        var browser = container.Resolve<Browser>();
        var file = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots", $"{Safe(scenario.ScenarioInfo.Title)}-{Guid.NewGuid():N}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllBytes(file, browser.Screenshot());
        TestContext.AddTestAttachment(file, "Browser window when the scenario failed");
        output.AddAttachment(file);
    }

    /// <summary>Scenarios tagged @serial change global lab switches; the baseline comes back after each one.</summary>
    [AfterScenario("serial", Order = 1)]
    public async Task RestoreLab() => await container.Resolve<ShopApi>().RestoreLabDefaults();

    private static string Safe(string text) => string.Concat(text.Select(c => char.IsLetterOrDigit(c) ? c : '-'));
}
