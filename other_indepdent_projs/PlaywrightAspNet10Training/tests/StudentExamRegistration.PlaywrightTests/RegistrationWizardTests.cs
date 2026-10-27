using Microsoft.Playwright;
using Xunit;

namespace StudentExamRegistration.PlaywrightTests;

public class RegistrationWizardTests
{
    [Fact]
    public async Task LearningExample()
    {
        using var pw = await Playwright.CreateAsync();
        var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
        var page = await browser.NewPageAsync();

        // Replace URL with running MVC app.
        await page.GotoAsync("http://localhost:5000");

        await browser.CloseAsync();
    }
}