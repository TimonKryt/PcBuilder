using HtmlAgilityPack;
using PcBuilder.Core.Models;
using PuppeteerSharp;

namespace PcBuilder.Api.Parsers;

public class DnsParser : BaseParser
{
    protected override string StoreName => "DNS";
    protected override string BaseUrl => "https://www.dns-shop.ru";

    protected override string[] CategoryUrls => new[]
    {
        "https://www.dns-shop.ru/catalog/17a89aab16404e77/processory/",
        "https://www.dns-shop.ru/catalog/17a89a0416404e77/videokarty/"
    };

    public override async Task<ParsingResult> ParseAsync()
    {
        var result = new ParsingResult
        {
            StoreName = StoreName,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            await using var browser = await CreateBrowserAsync();
            var page = await CreatePageAsync(browser);

            // Сначала заходим на главную страницу DNS для получения cookies
            Console.WriteLine($"[DNS] Заходим на главную страницу...");
            await page.GoToAsync(BaseUrl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                Timeout = 30000
            });

            // Имитируем поведение человека
            await Task.Delay(Random.Shared.Next(2000, 4000));

            foreach (var categoryUrl in CategoryUrls)
            {
                try
                {
                    var products = await ParseCategoryAsync(page, categoryUrl);

                    foreach (var product in products)
                    {
                        result.ProductsFound++;
                        Console.WriteLine($"[DNS] {product.Name} - {product.Price} ₽");
                    }

                    // Пауза между категориями
                    await Task.Delay(Random.Shared.Next(2000, 5000));
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorMessages.Add($"Ошибка категории {categoryUrl}: {ex.Message}");
                    Console.WriteLine($"[DNS] Ошибка: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors++;
            result.ErrorMessages.Add($"Общая ошибка DNS: {ex.Message}");
            Console.WriteLine($"[DNS] Критическая ошибка: {ex.Message}");
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    private async Task<List<ParsedProduct>> ParseCategoryAsync(IPage page, string categoryUrl)
    {
        var products = new List<ParsedProduct>();

        try
        {
            Console.WriteLine($"[DNS] Загружаем: {categoryUrl}");

            // Переходим на страницу категории
            var response = await page.GoToAsync(categoryUrl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                Timeout = 30000
            });

            Console.WriteLine($"[DNS] Статус ответа: {response.Status}");

            if (response.Status == System.Net.HttpStatusCode.Forbidden)
            {
                Console.WriteLine($"[DNS] 403 Forbidden — сайт заблокировал запрос");
                return products;
            }

            // Ждём загрузки контента
            await Task.Delay(Random.Shared.Next(3000, 5000));

            // Прокручиваем страницу как человек
            await HumanLikeScroll(page);

            var html = await page.GetContentAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Ищем ссылки на товары (реальные селекторы DNS)
            var productLinks = doc.DocumentNode.SelectNodes(
                "//a[contains(@class, 'catalog-product__name') and contains(@href, '/product/')]");

            if (productLinks == null || !productLinks.Any())
            {
                // Сохраняем HTML для отладки
                var snippet = html.Length > 2000 ? html.Substring(0, 2000) : html;
                Console.WriteLine($"[DNS] Ссылки не найдены. Фрагмент HTML:\n{snippet}");
                return products;
            }

            Console.WriteLine($"[DNS] Найдено ссылок: {productLinks.Count}");

            foreach (var link in productLinks.Take(5)) // Ограничиваем для теста
            {
                try
                {
                    var href = link.GetAttributeValue("href", "");
                    if (string.IsNullOrEmpty(href)) continue;

                    if (!href.StartsWith("http"))
                        href = BaseUrl + href;

                    var name = NormalizeText(link.InnerText);
                    Console.WriteLine($"[DNS] Товар: {name}");
                    Console.WriteLine($"[DNS] URL: {href}");

                    products.Add(new ParsedProduct
                    {
                        Name = name,
                        Price = 0, // Цену будем брать со страницы товара
                        StoreName = StoreName,
                        StoreUrl = href,
                        Category = "Комплектующие",
                        InStock = true
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DNS] Ошибка ссылки: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DNS] Ошибка категории: {ex.Message}");
        }

        return products;
    }

    /// <summary>
    /// Человеческая прокрутка (не мгновенная, с паузами)
    /// </summary>
    private async Task HumanLikeScroll(IPage page)
    {
        try
        {
            await page.EvaluateExpressionAsync(@"
                new Promise(async (resolve) => {
                    const delay = ms => new Promise(r => setTimeout(r, ms));
                    let scrolled = 0;
                    const step = 200 + Math.random() * 300;
                    
                    while (scrolled < 3000) {
                        window.scrollBy(0, step);
                        scrolled += step;
                        await delay(500 + Math.random() * 1000);
                    }
                    resolve();
                });
            ");
            await Task.Delay(1000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DNS] Ошибка прокрутки: {ex.Message}");
        }
    }
}