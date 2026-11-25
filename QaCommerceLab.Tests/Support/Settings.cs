using Microsoft.Extensions.Configuration;

namespace QaCommerceLab.Tests.Support;

/// <summary>
/// Test run settings. Defaults live in appsettings.json; secrets never do.
/// Order of precedence: appsettings.json, then user-secrets (local machine), then environment variables (CI).
/// Environment variables use a double underscore for nesting, for example Demo__Email.
/// </summary>
public sealed class Settings
{
    private static readonly Lazy<Settings> Instance = new(Load);

    public static Settings Current => Instance.Value;

    public required Uri BaseUrl { get; init; }
    public required TimeSpan OrderTimeout { get; init; }
    public required bool Headless { get; init; }
    public required int WindowWidth { get; init; }
    public required int WindowHeight { get; init; }
    public required TimeSpan Wait { get; init; }

    private readonly IConfiguration _configuration;

    private Settings(IConfiguration configuration) => _configuration = configuration;

    /// <summary>The seeded demo account of qa-commerce-lab. Required only by scenarios that use it.</summary>
    public string DemoEmail => Required("Demo:Email");

    public string DemoPassword => Required("Demo:Password");

    private static Settings Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<Settings>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        return new Settings(configuration)
        {
            BaseUrl = new Uri(configuration["Lab:BaseUrl"] ?? "http://localhost:3000"),
            OrderTimeout = TimeSpan.FromSeconds(configuration.GetValue("Lab:OrderTimeoutSeconds", 15)),
            Headless = configuration.GetValue("Browser:Headless", true),
            WindowWidth = configuration.GetValue("Browser:Width", 1280),
            WindowHeight = configuration.GetValue("Browser:Height", 900),
            Wait = TimeSpan.FromSeconds(configuration.GetValue("Browser:WaitSeconds", 10)),
        };
    }

    private string Required(string key)
    {
        var value = _configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Setting '{key}' is missing. Locally: dotnet user-secrets set \"{key}\" <value> " +
                $"(see README). In CI: the workflow reads it from the environment variable {key.Replace(":", "__")}, " +
                "filled from the repository secrets DEMO_EMAIL and DEMO_PASSWORD.");
        }

        return value;
    }
}
