using Microsoft.Playwright;
using Xunit;

namespace StudentExamRegistration.PlaywrightTests;

public class BrowserFixture : IAsyncLifetime
{
    public IPlaywright Playwright = null!;
    public IBrowser Browser = null!;

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }
}