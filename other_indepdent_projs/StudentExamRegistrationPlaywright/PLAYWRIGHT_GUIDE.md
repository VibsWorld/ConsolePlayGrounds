# Professional Playwright Notes

## Why Playwright?
- Auto waiting
- Cross browser
- Great CI support

## Test Pyramid
Unit -> Integration -> UI E2E

## ASP.NET Integration
Use WebApplicationFactory for in-memory hosting.
Use seeded test data.
Prefer page object model.

## Fixtures
Fixtures centralize browser startup and cleanup.

## Recommended Structure
Pages/
Fixtures/
Tests/
TestData/

## REST API Example
GET /api/courses
Returns available exam courses.

---

# Installing Playwright Manually

See the full guide: **[docs/PLAYWRIGHT_INSTALL_GUIDE.md](docs/PLAYWRIGHT_INSTALL_GUIDE.md)**

## Quick version

The `Microsoft.Playwright` package copies a `playwright.ps1` shim into the **test project's
output folder**. You must build the test project first, then run that script:

````powershell
cd tests\StudentExamRegistration.Tests
dotnet build
.\bin\Debug\net10.0\playwright.ps1 install
````

| Command | Effect |
|---|---|
| `playwright.ps1 install` | Download all browsers (chromium, firefox, webkit, ffmpeg) |
| `playwright.ps1 install chromium` | Download Chromium only — fastest |
| `playwright.ps1 uninstall` | Remove all Playwright-managed browsers |
| `playwright.ps1 codegen <url>` | Record a session and generate C# code |
| `playwright.ps1 open <url>` | Open the Playwright inspector |

Browsers land in `%LOCALAPPDATA%\ms-playwright\`.

> The copy of `playwright.ps1` inside the **NuGet cache** does not work — it cannot find
> `Microsoft.Playwright.dll`. Always run the one in `bin`.

---

# Introducing a Delay Between Two Page Requests

## The Golden Rule

Playwright's design is **auto-waiting**: `ClickAsync`, `FillAsync`, `CheckAsync` etc. already
wait for the element to be actionable and the navigation to settle. So a fixed sleep is almost
always the wrong tool — it either wastes time or is still flaky.

| You want to… | Use |
|---|---|
| Wait for the **next page** to load | `WaitForURLAsync(...)` / `WaitForLoadStateAsync(...)` |
| Wait for a **specific API call** | `WaitForResponseAsync(...)` |
| Wait for a **spinner** to disappear | `WaitForSelectorAsync(..., new() { State = WaitForSelectorState.Hidden })` |
| Simulate a **slow network** | Route interception with `Task.Delay` |
| Just **pause** for a fixed time | `WaitForTimeoutAsync(ms)` — last resort |

---

## 1. `WaitForTimeoutAsync` — the blunt fixed delay

````csharp
await page.ClickAsync("#next1");
await page.WaitForTimeoutAsync(2000);   // hard stop for 2 seconds
````

Works, but it is **always exactly 2s** — faster than needed wastes time, slower breaks the test.
Use it only for teaching/debugging, never as a sync mechanism.

---

## 2. `WaitForURLAsync` — wait for the next navigation (recommended)

Best for a multi-step wizard where each click triggers a full page post.

````csharp
// Wait for the URL to change to Step 2
await page.ClickAsync("#next1");
await page.WaitForURLAsync("**/Registration/Step2");

// Or wait for a URL pattern
await page.WaitForURLAsync(new Regex("/Registration/Step\\d+"));

// Or wait for a specific URL plus a load state
await page.WaitForURLAsync("**/Success", new PageWaitForURLOptions
{
    WaitUntil = WaitUntilState.Load
});
````

| Parameter | Values |
|---|---|
| `WaitUntil` | `Load`, `DOMContentLoaded`, `NetworkIdle` |

---

## 3. `WaitForLoadStateAsync` — wait for the network to go quiet

````csharp
await page.ClickAsync("#next2");
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);   // no requests for 500ms
````

| `LoadState` | Meaning |
|---|---|
| `Load` | `load` event fired (default for `GotoAsync`) |
| `DOMContentLoaded` | HTML parsed, sub-resources may still be loading |
| `NetworkIdle` | No network activity for 500 ms — **use for SPAs** |

> ⚠️ `NetworkIdle` is discouraged for pages that poll (dashboards, SignalR, chat). It can hang.
> Prefer `WaitForURLAsync` or a specific element wait.

---

## 4. `WaitForResponseAsync` — wait for one specific request

````csharp
// Start the action and wait for the API call it triggers
var response = await page.RunAndWaitForResponseAsync(
    async () => await page.ClickAsync("#next2"),
    "**/api/courses");

Console.WriteLine($"Status: {response.Status}");
````

Also available: `WaitForRequestAsync`, `WaitForRequestFinishedAsync`, `RunAndWaitForRequestAsync`.

---

## 5. Route interception — simulate a **slow server** (real network delay)

This is the true "delay between requests" technique. It delays the actual HTTP round-trip
instead of just idling the test thread.

````csharp
// Delay every request by 2 seconds
await using var route = await page.RouteAsync("**/*", async route =>
{
    await Task.Delay(2000);
    await route.ContinueAsync();
});

// Delay only the API calls
await using var apiRoute = await page.RouteAsync("**/api/**", async route =>
{
    await Task.Delay(1500);
    await route.ContinueAsync();
});
````

> `RouteAsync` returns `IAsyncDisposable` — use `await using` so the route is removed
> when the block ends.

To **block** a request entirely (simulate a timeout), call `AbortAsync` instead:

````csharp
await page.RouteAsync("**/api/slow", route => route.AbortAsync("timedout"));
````

---

## 6. Global timeout / default navigation timeout

Applies to every Playwright action on the page, so you never have to repeat the value:

````csharp
page.SetDefaultTimeout(30_000);   // ms — default action timeout
page.SetDefaultNavigationTimeout(60_000);
````

> `SetDefaultTimeout` is a **timeout ceiling**, not a delay. It only matters if a step is
> genuinely slow (cold start, big DB query).

---

## 7. Waiting for an element to appear / disappear

````csharp
// Appears
await page.WaitForSelectorAsync("#successBanner");

// Disappears (spinner)
await page.WaitForSelectorAsync(".spinner", new PageWaitForSelectorOptions
{
    State = WaitForSelectorState.Hidden
});
````

---

## Applied to this project's wizard

````csharp
[Fact]
public async Task Complete_Wizard()
{
    var page = await _browser.NewPageAsync();

    // Optional: simulate a slow server for every request
    await using var route = await page.RouteAsync("**/*", async r =>
    {
        await Task.Delay(1000);
        await r.ContinueAsync();
    });

    await page.GotoAsync("http://localhost:5000");

    // Step 1
    await page.FillAsync("#name", "Vaibhav Student");
    await page.FillAsync("#email", "student@test.com");
    await page.ClickAsync("#next1");
    await page.WaitForURLAsync("**/Registration/Step2");   // wait for the next page

    // Step 2
    await page.CheckAsync("#math");
    await page.CheckAsync("#physics");
    await page.ClickAsync("#next2");
    await page.WaitForURLAsync("**/Registration/Step3");

    // Step 3
    await page.FillAsync("#receiptId", "RCPT-1001");
    await page.ClickAsync("#finish");
    await page.WaitForURLAsync("**/Registration/Success");

    await Assertions.Expect(page).ToHaveTitleAsync(new Regex(".*"));
}
````

---

## Quick Reference

| API | Blocks until | Use for |
|---|---|---|
| `WaitForTimeoutAsync(ms)` | Time passes | Debugging only |
| `WaitForURLAsync(url)` | URL matches | Page navigation |
| `WaitForLoadStateAsync(state)` | Load event / network idle | SPA settling |
| `WaitForResponseAsync(url)` | Matching response received | API-backed UI |
| `WaitForSelectorAsync(sel)` | Element visible/hidden | Content appears |
| `WaitForFunctionAsync(js)` | JS returns truthy | Custom condition |
| `RouteAsync` + `Task.Delay` | — | Simulating slow network |
| `SetDefaultTimeout(ms)` | — | Raising the timeout ceiling |

---

# Watching the UI Run in Slow Motion

> Goal: **watch** the browser drive the app step by step, so you can see how the page
> behaves. This is a debugging/learning activity — not something you ship to CI.

## The best strategy: use Playwright's built-in `SlowMo`

Playwright has a **first-class slow-motion option** on the browser launch. It delays
*every* action (click, fill, check, navigate) so you can literally watch the UI react.

````csharp
_browser = await _pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false,     // real, visible browser window
    SlowMo = 1500          // milliseconds of delay before each action
});
````

| `SlowMo` value | Effect |
|---|---|
| `0` (default) | Normal speed — use for CI |
| `500` | Comfortable — you can still follow it |
| `1500` | Good for watching form fills and clicks |
| `3000` | Glacial — for tracing a subtle UI issue |

> ✅ `SlowMo` is the right tool here. It is applied by the driver **before every
> action**, so you observe the real interaction, not a synthetic sleep.
> ❌ Do **not** sprinkle `WaitForTimeoutAsync` between steps to achieve the same thing —
> it only pauses *after* an action, so the click itself still appears instantaneous.

---

## Step 1 — Make the delay configurable (no recompile)

Read it from an environment variable so you can tune it per run:

````csharp
private static readonly int SlowMoMs =
    int.TryParse(Environment.GetEnvironmentVariable("SLOW_MO_MS"), out var ms) ? ms : 0;
````

Then:

````powershell
# Normal run (CI default)
dotnet test

# Watch at 1.5 seconds per action
$env:SLOW_MO_MS=1500; dotnet test

# Extreme slow motion
$env:SLOW_MO_MS=3000; dotnet test

# Back to normal
$env:SLOW_MO_MS=0; dotnet test
````

---

## Step 2 — Record a video so you can re-watch it

A live window is gone the moment the test ends. Video gives you a replayable artefact.

**Video is recorded per *context*, not per browser** — so you must create the page through
a `BrowserContext`:

````csharp
var context = await _browser.NewContextAsync(new BrowserNewContextOptions
{
    RecordVideoDir = AppContext.BaseDirectory,
    RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 }
});

var page = await context.NewPageAsync();
try
{
    // ... your steps ...
}
finally
{
    await context.CloseAsync();   // the video is only finalised on close
}
````

The file lands in the output folder as `page@<guid>.webm`.

> ⚠️ The type is `RecordVideoSize`, **not** `VideoSize`.

---

## Step 3 — Playwright Inspector (the most powerful option)

For interactive debugging, the Inspector gives you a step debugger, live DOM snapshot,
network log, and a "Pick element" button.

> ⚠️ In Playwright **1.64.1** there is **no** `PlaywrightCreateOptions` — `Playwright.CreateAsync()`
> takes **no arguments**. Slow motion is set **only** on the browser launch options
> (`BrowserTypeLaunchOptions.SlowMo`), and the Inspector is enabled via the
> `PWDEBUG=1` environment variable, not an option object.

````powershell
# Launch under the Inspector: step through the test, inspect the DOM live
$env:PWDEBUG=1
$env:SLOW_MO_MS=1500
dotnet test
````

Or use the CLI in headed mode:

````powershell
.\bin\Debug\net10.0\playwright.ps1 open http://localhost:5000
.\bin\Debug\net10.0\playwright.ps1 codegen http://localhost:5000
````

`codegen` records your manual clicks and generates the matching C# code — very handy
for building the first version of a test.

---

## Step 4 — Trace viewer (see every action, step by step)

Traces record a timeline of **every** action, plus DOM snapshots at each step.

> ⚠️ `StartAsync` returns an `IAsyncDisposable` handle — it does **not** have `StopAsync`.
> Call `StopAsync` on **`context.Tracing`**, not on the returned handle.

````csharp
await using var trace = await context.Tracing.StartAsync(new TracingStartOptions
{
    Screenshots = true,
    Snapshots  = true,
    Sources    = true
});

try
{
    // ... your steps ...
}
finally
{
    await context.Tracing.StopAsync(new TracingStopOptions
    {
        Path = "trace.zip"
    });
}
````

Open it with:

````powershell
.\bin\Debug\net10.0\playwright.ps1 show-trace trace.zip
````

The trace viewer has a **time-travel scrubber** — step backwards and forwards through
the run and watch the DOM at each point. This is the best tool for "how is the page
behaving?" questions.

---

## Step 5 — Pause anywhere on demand

To freeze the run at an interesting moment, add a conditional pause:

````csharp
await page.ClickAsync("#next1");

// Freeze here so you can inspect the page in the browser
if (Environment.GetEnvironmentVariable("PAUSE") == "true")
{
    Console.WriteLine("Paused - press Enter in the test console to continue...");
    Console.ReadLine();
}
````

---

## Recommended combination

| What you want | Use |
|---|---|
| Watch the UI live, once | `Headless=false` + `SlowMo=1500` |
| Watch it live **and** keep a replay | + `RecordVideoDir` |
| Understand *why* it behaved that way | + `Tracing` → `show-trace` |
| Write the test by clicking | `playwright.ps1 codegen <url>` |
| Stop at one exact moment | `Console.ReadLine()` pause |

---

## Full example (this repo's test)

````csharp
public class RegistrationWizardTests : IAsyncLifetime
{
    private static readonly int SlowMoMs =
        int.TryParse(Environment.GetEnvironmentVariable("SLOW_MO_MS"), out var ms) ? ms : 0;

    private static readonly bool RecordVideo =
        Environment.GetEnvironmentVariable("RECORD_VIDEO") == "true";

    private const string BaseUrl = "http://localhost:5000";

    private IPlaywright _pw = null!;
    private IBrowser _browser = null!;

    public async Task InitializeAsync()
    {
        _pw = await Playwright.CreateAsync();
        _browser = await _pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,        // show the actual browser window
            SlowMo = SlowMoMs        // built-in slow motion
        });
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _pw.Dispose();
    }

    [Fact]
    public async Task Complete_Wizard()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            RecordVideoDir = RecordVideo ? AppContext.BaseDirectory : null,
            RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 }
        });

        var page = await context.NewPageAsync();
        try
        {
            await page.GotoAsync(BaseUrl);

            await page.FillAsync("#name", "Vaibhav Student");
            await page.FillAsync("#email", "student@test.com");
            await page.ClickAsync("#next1");

            await page.CheckAsync("#math");
            await page.CheckAsync("#physics");
            await page.ClickAsync("#next2");

            await page.FillAsync("#receiptId", "RCPT-1001");
            await page.ClickAsync("#finish");

            await Assertions.Expect(page).ToHaveTitleAsync(new Regex(".*"));

            if (SlowMoMs > 0) await page.WaitForTimeoutAsync(SlowMoMs);
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
````

Run it:

````powershell
# Terminal 1 — the app under test
cd src\StudentExamRegistration
dotnet run --urls http://localhost:5000

# Terminal 2 — watch it happen
cd tests\StudentExamRegistration.Tests
$env:SLOW_MO_MS=1500; $env:RECORD_VIDEO="true"; dotnet test
````
