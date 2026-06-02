using HtmlAgilityPack;
using PcBuilder.Core.Models;
using PuppeteerSharp;
using System.Net;
using System.Net.Http.Headers;

namespace PcBuilder.Api.Parsers;

public abstract class BaseParser
{
    protected abstract string StoreName { get; }
    protected abstract string BaseUrl { get; }
    protected abstract string[] CategoryUrls { get; }

    protected readonly HttpClient HttpClient;

    protected BaseParser()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseCookies = true,
            AllowAutoRedirect = true
        };

        HttpClient = new HttpClient(handler);

        HttpClient.DefaultRequestHeaders.Accept.Clear();
        HttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("text/html"));
        HttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
        HttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
        HttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("*/*", 0.8));
        HttpClient.DefaultRequestHeaders.AcceptLanguage.Add(
            new StringWithQualityHeaderValue("ru-RU"));
        HttpClient.DefaultRequestHeaders.AcceptLanguage.Add(
            new StringWithQualityHeaderValue("ru", 0.9));
        HttpClient.DefaultRequestHeaders.AcceptLanguage.Add(
            new StringWithQualityHeaderValue("en-US", 0.8));
        HttpClient.DefaultRequestHeaders.AcceptLanguage.Add(
            new StringWithQualityHeaderValue("en", 0.7));
        HttpClient.DefaultRequestHeaders.UserAgent.Clear();
        HttpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        HttpClient.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
        HttpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua",
            "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"");
        HttpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
        HttpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
        HttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        HttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        HttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        HttpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        HttpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

        HttpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public abstract Task<ParsingResult> ParseAsync();

    /// <summary>
    /// Создание браузера с использованием системного Chrome
    /// </summary>
    protected async Task<IBrowser> CreateBrowserAsync()
    {
        // Путь к системному Chrome
        var chromePath = GetChromePath();

        if (string.IsNullOrEmpty(chromePath) || !File.Exists(chromePath))
        {
            // Если Chrome не найден, пробуем скачать
            Console.WriteLine($"[{StoreName}] Системный Chrome не найден. Скачиваем Chromium...");
            await new BrowserFetcher().DownloadAsync();
            chromePath = null; // Используем скачанный
        }
        else
        {
            Console.WriteLine($"[{StoreName}] Используем системный Chrome: {chromePath}");
        }

        var launchOptions = new LaunchOptions
        {
            Headless = true,
            Args = new[]
            {
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage",
                "--disable-gpu",
                "--disable-blink-features=AutomationControlled",
                "--disable-features=IsolateOrigins,site-per-process",
                "--disable-web-security",
                "--disable-features=VizDisplayCompositor",
                "--window-size=1920,1080"
            }
        };

        if (!string.IsNullOrEmpty(chromePath))
        {
            launchOptions.ExecutablePath = chromePath;
        }

        return await Puppeteer.LaunchAsync(launchOptions);
    }

    /// <summary>
    /// Поиск установленного Chrome в системе
    /// </summary>
    private string? GetChromePath()
    {
        var paths = new[]
        {
            // Windows
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe"),
            // Edge (тоже Chromium)
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
            @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    /// <summary>
    /// Настройка страницы для маскировки
    /// </summary>
    protected async Task<IPage> CreatePageAsync(IBrowser browser)
    {
        var page = await browser.NewPageAsync();

        await page.SetViewportAsync(new ViewPortOptions
        {
            Width = 1920,
            Height = 1080
        });

        await page.SetUserAgentAsync(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        await page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
        {
            { "Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7" },
            { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8" }
        });

        await page.EvaluateFunctionOnNewDocumentAsync(@"
            () => {
                Object.defineProperty(navigator, 'webdriver', {
                    get: () => false,
                });
                Object.defineProperty(navigator, 'plugins', {
                    get: () => [1, 2, 3, 4, 5],
                });
                Object.defineProperty(navigator, 'languages', {
                    get: () => ['ru-RU', 'ru'],
                });
                window.chrome = {
                    runtime: {},
                };
            }
        ");

        return page;
    }

    /// <summary>
    /// Получение HTML через HttpClient
    /// </summary>
    protected async Task<string> GetHtmlAsync(string url)
    {
        await Task.Delay(Random.Shared.Next(1000, 3000));
        var response = await HttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Парсинг цены
    /// </summary>
    protected decimal ParsePrice(string priceText)
    {
        if (string.IsNullOrWhiteSpace(priceText))
            return 0;

        var cleaned = new string(priceText
            .Where(c => char.IsDigit(c) || c == '.' || c == ',')
            .ToArray());

        cleaned = cleaned.Replace(',', '.');

        if (decimal.TryParse(cleaned, out var price))
            return price;

        return 0;
    }

    /// <summary>
    /// Нормализация текста
    /// </summary>
    protected string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return HtmlEntity.DeEntitize(text)
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Replace('\t', ' ')
            .Trim();
    }
}