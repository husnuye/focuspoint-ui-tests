using Microsoft.Playwright;
using Microsoft.Extensions.Configuration;

namespace WebTests.Core;

public static class PlaywrightFactory
{
    public static async Task<(IPlaywright pw, IBrowser browser, IBrowserContext ctx, IPage page)> CreateAsync(IConfiguration config)
    {
        var pw = await Playwright.CreateAsync();

        // ---- Config knobs with sensible defaults ----
        bool headless              = config.GetValue("Headless", false);          // headed helps reduce CAPTCHA triggers
        int  slowMoMs              = config.GetValue("SlowMoMs", 200);            // act more “human”
        bool useChromeChannel      = config.GetValue("UseChromeChannel", true);   // launch real Chrome instead of stock Chromium
        string ua                  = config.GetValue("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124 Safari/537.36");
        string locale              = config.GetValue("Locale", "en-US");
        int vpW                    = config.GetValue("Viewport:Width", 1366);
        int vpH                    = config.GetValue("Viewport:Height", 768);
        int actionTimeoutMs        = config.GetValue("TimeoutMs", 30000);
        int navigationTimeoutMs    = config.GetValue("NavigationTimeoutMs", 45000);
        bool trace                 = config.GetValue("Trace", true);
        bool video                 = config.GetValue("Video", false);
        string outputDir           = config.GetValue("OutputDir", "TestArtifacts");
        string storageStatePath    = config.GetValue("StorageStatePath", "storageState.json"); // if present, reuse session

        Directory.CreateDirectory(outputDir);

        // ---- Browser ----
        var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            SlowMo   = slowMoMs,
            Channel  = useChromeChannel ? "chrome" : null
        });

        // ---- Context (desktop-like, stable & debuggable) ----
        var ctx = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize     = new() { Width = vpW, Height = vpH },
            Locale           = locale,
            UserAgent        = ua,
            IgnoreHTTPSErrors= true,
            AcceptDownloads  = true,
            StorageStatePath = File.Exists(storageStatePath) ? storageStatePath : null,
            RecordVideoDir   = video ? outputDir : null,
            RecordVideoSize  = video ? new() { Width = vpW, Height = vpH } : null
        });

        // Optional tracing for later debugging
        if (trace)
        {
            try
            {
                await ctx.Tracing.StartAsync(new TracingStartOptions
                {
                    Screenshots = true,
                    Snapshots   = true,
                    Sources     = true
                });
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("already started", StringComparison.OrdinalIgnoreCase))
            {
                TestContext.WriteLine("[TRACE] Tracing already started, continuing.");
            }
        }

        // ---- Page (set default timeouts) ----
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(actionTimeoutMs);
        page.SetDefaultNavigationTimeout(navigationTimeoutMs);

        return (pw, browser, ctx, page);
    }
}