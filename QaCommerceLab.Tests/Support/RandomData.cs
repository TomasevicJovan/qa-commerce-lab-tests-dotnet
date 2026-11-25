using Bogus;
using QaCommerceLab.Tests.Api;

namespace QaCommerceLab.Tests.Support;

/// <summary>
/// Generated test data: every name, email, password and address a scenario types or sends comes from here.
/// The data is random but repeatable. The run has one seed (printed at the start and written to the report);
/// each scenario derives its own seed from it and its title, so it gets the same data on any worker.
/// Set TEST_SEED to repeat the data of an earlier run. Only the unique part of an email changes between runs,
/// so a repeated run never collides with an account that already exists.
/// </summary>
public sealed class RandomData
{
    private readonly Faker _faker;

    public RandomData(string scenarioTitle)
    {
        Seed = SeedFor(scenarioTitle);
        _faker = new Faker("en") { Random = new Randomizer(Seed) };
    }

    public int Seed { get; }

    public static int RunSeed { get; } = ReadRunSeed();

    public string FirstName() => _faker.Name.FirstName();

    public string LastName() => _faker.Name.LastName();

    public string FullName() => $"{FirstName()} {LastName()}";

    /// <summary>Unique email: the name part is repeatable, the 8-character suffix is unique per call.</summary>
    public string Email(string? name = null)
    {
        var parts = (name ?? FullName()).Split(' ', 2);
        var local = string.Join('.', parts.Select(AsciiLetters).Where(p => p.Length > 0));
        return $"{(local.Length > 0 ? local : "qa")}.{Guid.NewGuid().ToString("N")[..8]}@example.com";
    }

    /// <summary>A password that meets the shop's rule (8 to 128 characters).</summary>
    public string Password(int length = 12) => _faker.Internet.Password(length, memorable: false);

    public string Cardholder() => FullName();

    public Address Address() => new(
        Name: FullName(),
        Line1: _faker.Address.StreetAddress(),
        City: _faker.Address.City(),
        PostalCode: _faker.Address.ZipCode("#####"),
        Country: "BA");

    private static int ReadRunSeed()
    {
        var value = Environment.GetEnvironmentVariable("TEST_SEED");
        return int.TryParse(value, out var seed) && seed > 0 ? seed : Random.Shared.Next(1, int.MaxValue);
    }

    private static int SeedFor(string scenarioTitle)
    {
        var hash = RunSeed;
        foreach (var c in scenarioTitle) hash = unchecked(hash * 31 + c);
        return hash == int.MinValue ? 1 : Math.Max(1, Math.Abs(hash));
    }

    private static string AsciiLetters(string text) =>
        new string(text.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z').ToArray()).ToLowerInvariant();
}
