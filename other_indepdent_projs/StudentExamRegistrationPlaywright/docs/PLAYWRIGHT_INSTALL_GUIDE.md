# Installing Playwright Manually (Windows)

Everything below assumes **Windows 10/11**, **PowerShell**, and the **.NET 10 SDK**.

---

## 1. How Playwright finds its CLI

The `Microsoft.Playwright` NuGet package ships a **PowerShell shim** that is copied into your
test project's output folder during build:

```
tests\StudentExamRegistration.Tests\bin\Debug\net10.0\playwright.ps1
```

That shim is the **only** supported way to install browsers on Windows. It loads
`Microsoft.Playwright.dll` from the same folder, so **you must build the test project first** —
otherwise the DLL is not there and the script fails with:

```
Microsoft.Playwright assembly was found, but is missing required assets.
Please ensure to build your project before running Playwright tool.
```

> ⚠️ The script in the **NuGet cache** (`~/.nuget\packages\microsoft.playwright\<ver>\build\playwright.ps1`)
> does **not** work — it looks for the DLL next to itself, where it does not exist.
> Always use the copy in `bin`.

---

## 2. Standard install (recommended)

From the **test project** folder:

````powershell
cd tests\StudentExamRegistration.Tests

# 1. Restore + build first (this is what copies playwright.ps1 into bin)
dotnet build

# 2. Install the browsers
.\bin\Debug\net10.0\playwright.ps1 install
````

Expected output (revision numbers depend on the package version):

```
Downloading Chrome for Testing 156.0.8078.4 (playwright chromium v1248) ...
197.5 MiB [====================] 100%
Downloading Chrome Headless Shell 156.0.8078.4 ...
118.1 MiB [====================] 100%
Downloading Firefox 157.0 (playwright firefox v1555) ...
123.8 MiB [====================] 100%
Downloading WebKit 27.2 (playwright webkit v2370) ...
60.2 MiB [====================] 100%
Downloading FFmpeg (playwright ffmpeg v1013) ...
```

### Install only the browsers you need

````powershell
# Chromium only — much faster (~200 MB saved)
.\bin\Debug\net10.0\playwright.ps1 install chromium

# Chromium + headless shell (minimum for `Headless = true`)
.\bin\Debug\net10.0\playwright.ps1 install chromium chromium-headless-shell
````

| Browser | Needed when |
|---|---|
| `chromium` | Always — the default in every example here |
| `chromium-headless-shell` | `Headless = true` (CI) |
| `firefox` | You launch Firefox in a test |
| `webkit` | You launch WebKit / test Safari behaviour |
| `ffmpeg` | You record video (`RecordVideoAsync`) |

---

## 3. Where browsers are stored

````
%LOCALAPPDATA%\ms-playwright\
├── chromium-1248\
├── chromium_headless_shell-1248\
├── firefox-1555\
├── webkit-2370\
├── ffmpeg-1013\
└── .links\            <- ties the folder to the package version that downloaded it
````

To see what is currently installed:

````powershell
Get-ChildItem "$env:LOCALAPPDATA\ms-playwright" -Directory | Select-Object Name
````

---

## 4. Revision mismatch — the most common failure

Playwright pins an **exact browser revision** per package version. If the cached revision
does not match what the package expects, tests fail at launch with:

```
Executable doesn't exist at C:\Users\<you>\AppData\Local\ms-playwright\chromium-1148\chrome-win\chrome.exe
Please run the following command to download new browsers:
playwright install
```

**Fix** — install the browsers for the version you are currently referencing:

````powershell
# Re-run install; it downloads the missing revision
.\bin\Debug\net10.0\playwright.ps1 install

# Or pin the package to a version whose revision you already have
dotnet add package Microsoft.Playwright --version 1.5x.x
````

> Tip: the expected revision lives in the package itself:
> ````
> $j = Get-Content "$env:USERPROFILE\.nuget\packages\microsoft.playwright\<ver>\.playwright\package\browsers.json" -Raw | ConvertFrom-Json
> $j.browsers | Select-Object name, revision
> ````

---

## 5. Use it from the .NET SDK (MSBuild integration)

Add this to the **test project's** `.csproj` to install browsers automatically on every build:

````xml
<PropertyGroup>
  <PlaywrightInstall>true</PlaywrightInstall>
</PropertyGroup>
````

Or run it as an explicit MSBuild target:

````powershell
dotnet build -t:PlaywrightInstall
````

---

## 6. Offline / air-gapped machines

Browsers are downloaded from `https://cdn.playwright.dev`. If you cannot reach it:

1. On a networked machine, run `playwright.ps1 install`.
2. Copy the **entire** `%LOCALAPPDATA%\ms-playwright` folder to the offline machine, at the same path.
3. Set `PLAYWRIGHT_BROWSERS_PATH` to point at it if the location differs:

````powershell
$env:PLAYWRIGHT_BROWSERS_PATH = "D:\tools\ms-playwright"
````

---

## 7. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `assembly was found, but is missing required assets` | `playwright.ps1` run before `dotnet build` | Build the test project first |
| `Executable doesn't exist at ...chromium-XXXX` | Stale browser revision | Re-run `playwright.ps1 install` |
| `playwright.ps1` not found | Project not built | `dotnet build` from the test project folder |
| `Access denied` writing to `ms-playwright` | Folder locked by another process | Close running Playwright tests, retry |
| Browser launches but tests hang | `Headless = false` with no display | Set `Headless = true` for CI |
| `net::ERR_CONNECTION_REFUSED` | Web app not running | Start it: `dotnet run --urls http://localhost:5000` |

---

## 8. Full end-to-end run (this repo)

````powershell
# --- Terminal 1: start the web app ---
cd src\StudentExamRegistration
dotnet run --urls http://localhost:5000

# --- Terminal 2: install browsers once, then test ---
cd tests\StudentExamRegistration.Tests
dotnet build
.\bin\Debug\net10.0\playwright.ps1 install
dotnet test
````

> The wizard is served at `http://localhost:5000/` and follows the convention-based route
> `/Registration/Step1` → `/Registration/Step2` → `/Registration/Step3` → `/Registration/Success`.

---

## 9. Useful CLI commands

````powershell
# Record a video of a session
.\bin\Debug\net10.0\playwright.ps1 codegen http://localhost:5000

# Open the inspector for manual exploration
.\bin\Debug\net10.0\playwright.ps1 open http://localhost:5000

# Take a one-off screenshot
.\bin\Debug\net10.0\playwright.ps1 screenshot http://localhost:5000 shot.png

# Remove all Playwright-managed browsers
.\bin\Debug\net10.0\playwright.ps1 uninstall
````
