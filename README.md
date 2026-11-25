# qa-commerce-lab-tests-dotnet

[![Tests](https://github.com/TomasevicJovan/qa-commerce-lab-tests-dotnet/actions/workflows/tests.yml/badge.svg)](https://github.com/TomasevicJovan/qa-commerce-lab-tests-dotnet/actions/workflows/tests.yml)

BDD test automation in C# for **FORMA**, a practice online shop ([qa-commerce-lab](https://github.com/TomasevicJovan/qa-commerce-lab)): Selenium WebDriver, Reqnroll and NUnit on .NET 10.

The same shop is also tested in TypeScript with Playwright in [qa-commerce-lab-tests](https://github.com/TomasevicJovan/qa-commerce-lab-tests). That repository holds the detailed suite (456 tests, every field and message). This one covers the business rules and the main shopping journeys in Gherkin, written so that someone who is not a developer can read them and check them against the shop's [business rules](https://github.com/TomasevicJovan/qa-commerce-lab/blob/main/docs/BUSINESS.md).

**Latest report:** [tomasevicjovan.github.io/qa-commerce-lab-tests-dotnet](https://tomasevicjovan.github.io/qa-commerce-lab-tests-dotnet/)

## What is covered

51 scenarios in 8 feature files:

| Feature | Scenarios | What it checks |
|---|---:|---|
| [Account](QaCommerceLab.Tests/Features/Account.feature) | 8 | Registration, form errors, sign in, wrong password, sign out |
| [Finding products](QaCommerceLab.Tests/Features/FindingProducts.feature) | 6 | Search, no results, subcategory, Black Friday, Free shipping, sorting |
| [Shopping bag](QaCommerceLab.Tests/Features/Bag.feature) | 6 | Adding, quantities, the limit of 10, emptying, bag kept after sign-in, signed-out visitor |
| [Wishlist](QaCommerceLab.Tests/Features/Wishlist.feature) | 3 | Saving with the heart, moving to the bag, signed-out visitor |
| [Prices, discounts and shipping](QaCommerceLab.Tests/Features/Pricing.feature) | 10 | Every example of the pricing table in the business rules: SAVE10, Mastercard 10%, Black Friday, the $150 limit, free-shipping bags, rounding |
| [Checkout and payment](QaCommerceLab.Tests/Features/Checkout.feature) | 9 | New, saved and wallet payments, declined card, card form errors, expired coupon, required address |
| [Orders after checkout](QaCommerceLab.Tests/Features/Orders.feature) | 6 | Order page, My orders, losing the last unit to another customer, restoring the bag, another customer's order, bank decline releasing stock |
| [End-to-end purchases](QaCommerceLab.Tests/Features/EndToEnd.feature) | 3 | New customer from registration to My orders; Black Friday + SAVE10 + Mastercard; declined card, then a successful payment |

Example:

```gherkin
Scenario Outline: The order summary and the charged amount follow the pricing rules
  Given a signed-in customer with saved Visa and Mastercard cards
  And their bag holds <bag>
  When they go to checkout and pay with the saved <card> using coupon "<coupon>"
  Then the order summary shows subtotal <subtotal>, discount <discount>, shipping <shipping> and total <total>
  When they place the order
  Then the order is confirmed for <total>
```

## How the tests are built

- **Gherkin describes behaviour, not clicks.** Steps say what the customer does ("they check out with the saved Mastercard"), never which button is pressed. Selectors live only in page objects, so a changed screen means one change in one class.
- **Every scenario prepares its own state through the API.** Each scenario registers its own customer, and the bag, saved cards and earlier orders are set up through the shop's API. The browser is used only for the behaviour under test. Scenarios do not depend on each other, and feature files run in parallel.
- **The UI result is confirmed behind the UI.** After a payment, the API confirms the order status and the amount actually charged, not only the banner on the page.
- **An order accepted is not an order paid.** The shop processes stock and payment in the background. Steps wait for the final state (confirmed, rejected or cancelled) instead of waiting a fixed time. There is no `Thread.Sleep` anywhere; every read retries until the expected state appears and, on a timeout, reports what the page actually showed.
- **Settings that affect everyone run alone.** The scenario that makes the bank decline every payment changes the whole shop, so it is tagged `@serial`, runs on its own, and the lab is restored afterwards.
- **A failed scenario leaves a screenshot**, attached to the NUnit result and to the HTML report.

## Test data

No test data is written inside the step definitions. Data comes from four places, each with its own rule:

| Kind | Where | Example |
|---|---|---|
| Secrets and environment | `dotnet user-secrets` locally, repository secrets in CI; defaults in `appsettings.json` | Demo account email and password |
| Expected values (what the shop is supposed to do) | `Data/` in the repository, and the examples in the feature files | Products and prices, test cards, coupons, shop texts |
| Values that only have to be valid | `Support/RandomData.cs` (Bogus) | Customer names, emails, passwords, addresses, cardholders |
| State a scenario needs | `Support/Customers.cs`, through the API | A new customer, a bag, saved cards, a placed order |

Random data is seeded. The run seed is printed at the start of every run, and each scenario's seed (run seed + scenario title) is written to the report, so a scenario gets the same data on any worker. `TEST_SEED=<seed>` repeats a run with the same data. Invalid and boundary values (a 7-character password, a card that expired last month) are part of the test design and are written in the scenarios.

`Data/Catalog.cs` was checked against the shop's seed data. `Data/Pricing.cs` is an independent copy of the pricing rules, used to send the expected total when an order is prepared through the API.

## Project structure

```text
QaCommerceLab.Tests/
  Features/         Gherkin: one .feature file per area
  StepDefinitions/  Thin steps: they call page objects, the API and Customers, and hold no selectors
  Pages/            Page objects (product list, wishlist, checkout, order, orders) and components (header, sign-in dialog, bag, totals, notifications)
  Api/              REST client for preparing state and checking results, and the API models
  Data/             Expected values: catalog, test cards, coupons, shop texts, the pricing rules
  Support/          Browser and waiting, settings, random data, customers, hooks, scenario state
  appsettings.json  Non-secret defaults (lab address, browser size, timeouts)
  reqnroll.json     Reqnroll settings: the @serial tag, the HTML report
```

## Run locally

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download), Google Chrome, and the [shop](https://github.com/TomasevicJovan/qa-commerce-lab) running on http://localhost:3000: either with Docker (`docker compose up --build -d --wait`; `--build` matters, otherwise Docker reuses images built from older code) or with `npm start`, not both. Selenium Manager downloads the matching ChromeDriver by itself.

Store the demo account once (the values are in the shop's README); they stay on your machine, outside the repository:

```powershell
cd QaCommerceLab.Tests
dotnet user-secrets set "Demo:Email" "<demo email>"
dotnet user-secrets set "Demo:Password" "<demo password>"
cd ..
```

Then:

```powershell
dotnet test                                      # everything
dotnet test --filter "Category=smoke"            # the critical path
dotnet test --filter "Category=pricing"          # one feature, by its tag
$env:Browser__Headless = "false"; dotnet test    # in a visible browser window
```

The HTML report is written to `QaCommerceLab.Tests/bin/Debug/net10.0/reports/reqnroll_report.html`.

**Reset the shop between full runs.** A full run buys products, and the shop cannot restock. Scenarios that need stock stop with a clear message when it has run out. Before running everything again, reset the shop's data:

```powershell
# in the qa-commerce-lab folder
docker compose down --volumes; docker compose up --build -d --wait   # with Docker
npm run reset                                                # without Docker, with the shop stopped
```

CI always starts from fresh data.

## Continuous integration

[`.github/workflows/tests.yml`](.github/workflows/tests.yml) runs on every push to `main`, on pull requests, nightly and manually. It starts the shop with Docker (all services and Kafka), builds, runs the scenarios with the demo account taken from repository secrets, uploads the HTML report and the test results, and publishes the report to GitHub Pages from `main`. A branch tests the shop branch with the same name, if one exists.

## Tech stack

.NET 10 · C# · Selenium WebDriver 4 · Reqnroll 3 · NUnit 4 · Shouldly · Bogus · Microsoft.Extensions.Configuration · GitHub Actions · GitHub Pages
