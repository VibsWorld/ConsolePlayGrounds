# Playwright Master Guide

Topics: Locators, Fixtures, POM, Tracing, Screenshots, API Testing, Authentication, CI/CD, Azure DevOps, GitHub Actions, Parallel Execution, WebApplicationFactory.

---

# Installing Playwright Manually (Windows)

## How the CLI is located

The `Microsoft.Playwright` NuGet package copies a **`playwright.ps1`** shim into the test
project's output folder during build:

```
tests\StudentExamRegistration.PlaywrightTests\bin\Debug\net10.0\playwright.ps1
```

> ⚠️ The shim inside the **NuGet cache** (`~/.nuget\packages\microsoft.playwright\<ver>\build\`)
> does **not** work — it looks for `Microsoft.Playwright.dll` next to itself and fails with
> *"assembly was found, but is missing required assets"*. Always use the copy in `bin`.

## Standard install

````powershell
cd tests\StudentExamRegistration.PlaywrightTests

# 1. Build first — this is what copies playwright.ps1 into bin
dotnet build

# 2. Download the browsers
.\bin\Debug\net10.0\playwright.ps1 install
````

## Install a subset (faster)

````powershell
.\bin\Debug\net10.0\playwright.ps1 install chromium chromium-headless-shell
````

| Browser | Needed when |
|---|---|
| `chromium` | Always — the default browser in these examples |
| `chromium-headless-shell` | `Headless = true` (CI) |
| `firefox` | You launch Firefox in a test |
| `webkit` | You launch WebKit / test Safari behaviour |
| `ffmpeg` | You record video (`RecordVideoAsync`) |

## Where they land

````
%LOCALAPPDATA%\ms-playwright\   (e.g. chromium-1248, firefox-1555, webkit-2370)
````

Check what is installed:

````powershell
Get-ChildItem "$env:LOCALAPPDATA\ms-playwright" -Directory | Select-Object Name
````

## Revision mismatch (the most common failure)

Playwright pins an **exact browser revision** per package version. A stale cache fails with:

```
Executable doesn't exist at C:\Users\<you>\AppData\Local\ms-playwright\chromium-1148\chrome-win\chrome.exe
Please run the following command to download new browsers: playwright install
```

Fix by re-running the install for your current package version:

````powershell
.\bin\Debug\net10.0\playwright.ps1 install
````

Find the revision your package expects:

````powershell
$j = Get-Content "$env:USERPROFILE\.nuget\packages\microsoft.playwright\<ver>\.playwright\package\browsers.json" -Raw | ConvertFrom-Json
$j.browsers | Select-Object name, revision
````

## Offline machines

1. Install on a networked machine.
2. Copy the whole `%LOCALAPPDATA%\ms-playwright` folder across, same path.
3. If the path differs, set `PLAYWRIGHT_BROWSERS_PATH=D:\tools\ms-playwright`.

## Full end-to-end run

````powershell
# --- Terminal 1: start the web app ---
cd src\StudentExamRegistration.Web
dotnet run --urls http://localhost:5000

# --- Terminal 2: install browsers once, then test ---
cd tests\StudentExamRegistration.PlaywrightTests
dotnet build
.\bin\Debug\net10.0\playwright.ps1 install
dotnet test
````

## Useful commands

````powershell
.\bin\Debug\net10.0\playwright.ps1 codegen http://localhost:5000   # record & generate C#
.\bin\Debug\net10.0\playwright.ps1 open http://localhost:5000      # Playwright inspector
.\bin\Debug\net10.0\playwright.ps1 screenshot http://localhost:5000 shot.png
.\bin\Debug\net10.0\playwright.ps1 uninstall                        # remove all browsers
````

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `assembly was found, but is missing required assets` | ran `playwright.ps1` before building | `dotnet build` first |
| `Executable doesn't exist at ...chromium-XXXX` | stale browser revision | re-run `playwright.ps1 install` |
| `playwright.ps1` not found | project never built | `dotnet build` |
| `net::ERR_CONNECTION_REFUSED` | web app not running | start it on port 5000 |
| browser opens off-screen / hangs | `Headless = false` without a display | use `Headless = true` in CI |