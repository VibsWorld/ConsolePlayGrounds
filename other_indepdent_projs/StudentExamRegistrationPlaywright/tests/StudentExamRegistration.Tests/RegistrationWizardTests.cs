using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Xunit;

// Heavily commented learning example.
public class RegistrationWizardTests:IAsyncLifetime
{
 // ── Visual observation knobs ────────────────────────────────────────────────
 // Set SLOW_MO_MS to 0 for a normal (fast) CI run, or e.g. 1500 to watch every
 // action happen in slow motion. Read from an env var so you can change it
 // without recompiling:  $env:SLOW_MO_MS=1500; dotnet test
 private static readonly int SlowMoMs =
     int.TryParse(Environment.GetEnvironmentVariable("SLOW_MO_MS"), out var ms) ? ms : 1500;

 // When true the run is fully visible: real browser window + a "Recording" video.
 private static readonly bool RecordVideo = true;
    //  Environment.GetEnvironmentVariable("RECORD_VIDEO") == "true";

 // Base address of the app under test.
 private const string BaseUrl = "http://localhost:5000";

 private IPlaywright _pw=null!;
 private IBrowser _browser=null!;

 public async Task InitializeAsync(){
   // Launch real browser. Headless=false lets you watch execution.
   _pw=await Playwright.CreateAsync();
   _browser=await _pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions{
     Headless=false,          // show the actual browser window
     SlowMo=SlowMoMs          // built-in slow motion: every action is delayed
   });
 }

 public async Task DisposeAsync(){await _browser.DisposeAsync();_pw.Dispose();}

 [Fact]
 public async Task Complete_Wizard(){
   // Video is recorded per-context (not per-browser), so we create the page
   // through a context when RECORD_VIDEO=true.
   var videoPath = Path.Combine(AppContext.BaseDirectory, "wizard-run.webm");

   var context = await _browser.NewContextAsync(new BrowserNewContextOptions{
     RecordVideoDir = RecordVideo ? AppContext.BaseDirectory : null,
     RecordVideoSize = new RecordVideoSize{ Width = 1280, Height = 720 }
   });

   var page=await context.NewPageAsync();

   try{
     await page.GotoAsync(BaseUrl);

     // Step 1
     await page.FillAsync("#name","Vaibhav Student");
     await page.FillAsync("#email","student@test.com");
     await page.ClickAsync("#next1");

     // Step 2
     await page.CheckAsync("#math");
     await page.CheckAsync("#physics");
     await page.ClickAsync("#next2");

     // Step 3
     await page.FillAsync("#receiptId","RCPT-1001");
     await page.ClickAsync("#finish");

     await Assertions.Expect(page).ToHaveTitleAsync(new Regex(".*"));

     // Give the eye a moment on the final page before the browser closes.
     if(SlowMoMs>0) await page.WaitForTimeoutAsync(SlowMoMs);
   }
   finally{
     // Video is only finalised once the context is closed.
     await context.CloseAsync();
   }
 }
}
