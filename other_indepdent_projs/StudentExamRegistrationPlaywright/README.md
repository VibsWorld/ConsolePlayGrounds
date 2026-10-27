# ASP.NET Core 10 + Playwright Learning Project

Student Exam Registration Wizard with 3 steps and Playwright xUnit tests.

## Wizard
1. Personal Details
2. Choose Courses
3. Fees + Mock Payment

## Project Layout

```
src/StudentExamRegistration/          ASP.NET Core 10 MVC app (the system under test)
├── Controllers/RegistrationController.cs
├── Controllers/CoursesApiController.cs
└── Views/Registration/{Step1,Step2,Step3,Success}.cshtml

tests/StudentExamRegistration.Tests/   Playwright + xUnit E2E tests
└── RegistrationWizardTests.cs
```

## Run the app

````powershell
cd src\StudentExamRegistration
dotnet run --urls http://localhost:5000
````

Then open <http://localhost:5000/> in a browser.

## Install Playwright browsers (one time)

> Full details: **[docs/PLAYWRIGHT_INSTALL_GUIDE.md](docs/PLAYWRIGHT_INSTALL_GUIDE.md)**

````powershell
cd tests\StudentExamRegistration.Tests
dotnet build                                        # copies playwright.ps1 into bin
.\bin\Debug\net10.0\playwright.ps1 install           # downloads chromium, firefox, webkit
````

Install Chromium only if you want a smaller download:

````powershell
.\bin\Debug\net10.0\playwright.ps1 install chromium chromium-headless-shell
````

## Run the tests

Keep the app running on port 5000 in a separate terminal, then:

````powershell
cd tests\StudentExamRegistration.Tests
dotnet test
````

## REST API Example

`GET /api/courses` returns the available exam courses as JSON.

## Notes

- `src/StudentExamRegistration/Views.txt` was a placeholder for the Razor views — the real
  views now live in `src/StudentExamRegistration/Views/`.
- This repo inherits **Central Package Management** from `..\..\Directory.Packages.props`,
  so `PackageReference` entries must **not** carry a `Version` attribute.
