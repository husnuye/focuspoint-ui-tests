using Microsoft.Playwright;
using Microsoft.Extensions.Configuration;

namespace WebTests.Core;

public static class PlaywrightFactory
{
    public static async Task<(IPlaywright pw, IBrowser browser, IBrowserContext ctx, IPage page)> CreateAsync(IConfiguration config)
    {
        var pw = await Playwright.CreateAsync();

        // ---- Configuration toggles with pragmatic defaults ----
        // Headed mode reduces bot/CAPTCHA risk and makes local debugging easier.
        bool headless              = config.GetValue("Headless", false);
        // SlowMo helps mimic human interaction and stabilizes brittle UIs.
        int  slowMoMs              = config.GetValue("SlowMoMs", 200);
        // Prefer the Chrome channel to mirror real-user behavior over stock Chromium.
        bool useChromeChannel      = config.GetValue("UseChromeChannel", true);
        // Desktop UA + locale and viewport to resemble a standard user setup.
        string ua                  = config.GetValue("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124 Safari/537.36");
        string locale              = config.GetValue("Locale", "en-US");
        int vpW                    = config.GetValue("Viewport:Width", 1366);
        int vpH                    = config.GetValue("Viewport:Height", 768);
        // Default action/navigation timeouts: strict enough to catch hangs, lenient enough for CI latency.
        int actionTimeoutMs        = config.GetValue("TimeoutMs", 30000);
        int navigationTimeoutMs    = config.GetValue("NavigationTimeoutMs", 45000);
        // Diagnostics: capture traces/video on demand.
        bool trace                 = config.GetValue("Trace", true);
        bool video                 = config.GetValue("Video", false);
        // Artifacts: screenshots, traces, and optional videos.
        string outputDir           = config.GetValue("OutputDir", "TestArtifacts");
        // Reuse a signed-in session when available to speed up flows.
        string storageStatePath    = config.GetValue("StorageStatePath", "storageState.json");

        Directory.CreateDirectory(outputDir);

        // ---- Browser ----
        // Launch with Chrome channel when requested; otherwise use bundled Chromium.
        var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            SlowMo   = slowMoMs,
            Channel  = useChromeChannel ? "chrome" : null
        });

        // ---- Context (desktop-like) ----
        // Keep this context close to a typical user: viewport, locale, UA, HTTPS tolerance for test envs.
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

        // Optional tracing for post-failure investigation (safe to ignore if already running).
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

        // ---- Page ----
        // Set conservative defaults to surface genuine slowness without false-positives in CI.
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(actionTimeoutMs);
        page.SetDefaultNavigationTimeout(navigationTimeoutMs);

        return (pw, browser, ctx, page);
    }
}