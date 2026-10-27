# Complete Training Solution
Runnable starter architecture for ASP.NET Core 10 MVC + Playwright + xUnit.

## Project Layout

```
src/StudentExamRegistration.Web/      ASP.NET Core 10 MVC app
├── Controllers/RegistrationController.cs
├── Models/RegistrationViewModel.cs
└── Views/Registration/{Step1,Step2,Step3,Success}.cshtml

tests/StudentExamRegistration.PlaywrightTests/   Playwright + xUnit E2E tests
├── BrowserFixture.cs
└── RegistrationWizardTests.cs
```

## Run the app

````powershell
cd src\StudentExamRegistration.Web
dotnet run --urls http://localhost:5000
````

## Install Playwright browsers (one time)

> Full details: **[docs/PLAYWRIGHT_MASTER_GUIDE.md](docs/PLAYWRIGHT_MASTER_GUIDE.md)**

````powershell
cd tests\StudentExamRegistration.PlaywrightTests
dotnet build                                        # copies playwright.ps1 into bin
.\bin\Debug\net10.0\playwright.ps1 install           # downloads chromium, firefox, webkit
````

Chromium only (much smaller download):

````powershell
.\bin\Debug\net10.0\playwright.ps1 install chromium chromium-headless-shell
````

## Run the tests

````powershell
cd tests\StudentExamRegistration.PlaywrightTests
dotnet test
````

## Notes

- This repo inherits **Central Package Management** from the repo-root `Directory.Packages.props`,
  so `PackageReference` entries must **not** carry a `Version` attribute.
- The wizard is served at `http://localhost:5000/` via the convention route
  `{controller=Registration}/{action=Step1}/{id?}`.
