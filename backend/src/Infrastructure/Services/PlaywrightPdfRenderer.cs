using Boekhouding.Application.Interfaces;
using Microsoft.Playwright;

namespace Boekhouding.Infrastructure.Services;

/// <summary>
/// PDF renderer implementatie met Playwright
/// </summary>
public class PlaywrightPdfRenderer : IPdfRenderer
{
    private static bool _isPlaywrightInitialized = false;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public async Task<byte[]> GeneratePdfAsync(string htmlContent, string? cssContent = null)
    {
        await EnsurePlaywrightInitializedAsync();

        var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var page = await browser.NewPageAsync();

        // Combineer HTML en CSS
        var fullHtml = htmlContent;
        if (!string.IsNullOrEmpty(cssContent))
        {
            fullHtml = $"<style>{cssContent}</style>{htmlContent}";
        }

        await page.SetContentAsync(fullHtml);

        // Genereer PDF
        var pdfBytes = await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            PrintBackground = true,
            Margin = new Margin
            {
                Top = "1cm",
                Right = "1cm",
                Bottom = "1cm",
                Left = "1cm"
            }
        });

        await browser.CloseAsync();
        playwright.Dispose();

        return pdfBytes;
    }

    private static async Task EnsurePlaywrightInitializedAsync()
    {
        if (_isPlaywrightInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isPlaywrightInitialized) return;

            // Playwright browsers worden geïnstalleerd via:
            // pwsh bin/Debug/net8.0/playwright.ps1 install chromium
            // Dit moet handmatig gedaan worden bij deployment
            
            _isPlaywrightInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
