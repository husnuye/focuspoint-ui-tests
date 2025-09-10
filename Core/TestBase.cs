using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using NUnit.Framework;
using Allure.Net.Commons; // For AllureApi

namespace WebTests.Core;

public abstract class TestBase
{
    protected IConfiguration   Config  = default!;
    protected IPlaywright      PW      = default!;
    protected IBrowser         Browser = default!;
    protected IBrowserContext  Ctx     = default!;
    protected IPage            Page    = default!;

    // Where to write Allure results (resolution order: ENV -> appsettings -> fallback).
    protected string AllureOut =>
        Environment.GetEnvironmentVariable("ALLURE_RESULTS_DIRECTORY")
        ?? Config.GetValue<string>("OutputDir")
        ?? Path.Combine("bin", "Debug", "net9.0", "allure-results");

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        // 1) Load configuration
        Config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("Config/appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // 2) Start Playwright (browser/context/page)
        (PW, Browser, Ctx, Page) = await PlaywrightFactory.CreateAsync(Config);

        // 3) Enable Playwright tracing (ignore if already started)
        try
        {
            await Ctx.Tracing.StartAsync(new()
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

        // Ensure the Allure output directory exists
        Directory.CreateDirectory(AllureOut);
    }

    [TearDown]
    public async Task AfterEach()
    {
        // If the test failed, capture artifacts and attach them to NUnit + Allure
        if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            try
            {
                if (Page is not null && !Page.IsClosed)
                {
                    var pngPath = Path.Combine(AllureOut, $"{Guid.NewGuid()}-failure.png");
                    var bytes = await Page.ScreenshotAsync(new() { FullPage = true });
                    await File.WriteAllBytesAsync(pngPath, bytes);

                    // Attach to NUnit (helpful in IDE / CI logs)
                    TestContext.AddTestAttachment(pngPath, "Screenshot on Failure");

                    // Attach to Allure report
                    AllureApi.AddAttachment("screenshot", "image/png", bytes);

                    // (Optional) Attach current page HTML for faster root-cause analysis
                    var html = await Page.ContentAsync();
                    AllureApi.AddAttachment("page.html", "text/html", System.Text.Encoding.UTF8.GetBytes(html));
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[ERROR] Could not capture failure artifacts: {ex.Message}");
            }
        }
    }

    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        // Stop tracing and attach the trace archive (best-effort)
        try
        {
            Directory.CreateDirectory(AllureOut);
            var traceZip = Path.Combine(AllureOut, $"trace-{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip");
            await Ctx.Tracing.StopAsync(new() { Path = traceZip });

            if (File.Exists(traceZip))
            {
                TestContext.AddTestAttachment(traceZip, "Playwright Trace");
                AllureApi.AddAttachment("trace.zip", "application/zip", await File.ReadAllBytesAsync(traceZip));
            }
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("not been started", StringComparison.OrdinalIgnoreCase))
        {
            TestContext.WriteLine("[TRACE] Tracing was not active, nothing to stop.");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"[TRACE] Stop/attach failed: {ex.Message}");
        }

        // Dispose resources gracefully
        try { if (Ctx     is not null) await Ctx.CloseAsync(); } catch { /* ignore */ }
        try { if (Browser is not null) await Browser.CloseAsync(); } catch { /* ignore */ }
        try { PW?.Dispose(); } catch { /* ignore */ }
    }
}