using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using NUnit.Framework;
using Allure.Net.Commons; // AllureApi için

namespace WebTests.Core;

public abstract class TestBase
{
    protected IConfiguration   Config  = default!;
    protected IPlaywright      PW      = default!;
    protected IBrowser         Browser = default!;
    protected IBrowserContext  Ctx     = default!;
    protected IPage            Page    = default!;

    // Allure çıktısı nereye yazılacak? (ENV > appsettings > varsayılan)
    protected string AllureOut =>
        Environment.GetEnvironmentVariable("ALLURE_RESULTS_DIRECTORY")
        ?? Config.GetValue<string>("OutputDir")
        ?? Path.Combine("bin", "Debug", "net9.0", "allure-results");

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        // 1) Konfig yükle
        Config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("Config/appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // 2) Playwright başlat
        (PW, Browser, Ctx, Page) = await PlaywrightFactory.CreateAsync(Config);

        // 3) Tracing aç (varsa görmezden gel)
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

        Directory.CreateDirectory(AllureOut);
    }

    [TearDown]
    public async Task AfterEach()
    {
        // Test FAIL ise ekran görüntüsü al ve Allure'a ekle
        if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            try
            {
                if (Page is not null && !Page.IsClosed)
                {
                    var pngPath = Path.Combine(AllureOut, $"{Guid.NewGuid()}-failure.png");
                    var bytes = await Page.ScreenshotAsync(new() { FullPage = true });
                    await File.WriteAllBytesAsync(pngPath, bytes);

                    // NUnit attachment (IDE'ler için)
                    TestContext.AddTestAttachment(pngPath, "Screenshot on Failure");

                    // Allure attachment (rapor için)
                    AllureApi.AddAttachment("screenshot", "image/png", bytes);

                    // İsteğe bağlı: sayfa HTML’i
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
        // Tracing’i durdur ve Allure’a ekle (best-effort)
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

        // Kaynakları kapat
        try { if (Ctx      is not null) await Ctx.CloseAsync(); } catch { /* ignore */ }
        try { if (Browser  is not null) await Browser.CloseAsync(); } catch { /* ignore */ }
        try { PW?.Dispose(); } catch { /* ignore */ }
    }
}