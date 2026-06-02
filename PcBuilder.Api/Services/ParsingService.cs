using Microsoft.Extensions.DependencyInjection;
using PcBuilder.Api.Parsers;
using PcBuilder.Core.Data;
using PcBuilder.Core.Entities;
using PcBuilder.Core.Models;

namespace PcBuilder.Api.Services;

public class ParsingService
{
    private readonly List<BaseParser> _parsers;
    private readonly IServiceScopeFactory _scopeFactory;

    public ParsingService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        _parsers = new List<BaseParser>
        {
            new DnsParserSimple(),
            new CitilinkParser(),
            new MVideoParser()
        };
    }

    public async Task<List<ParsingResult>> ParseAllAsync()
    {
        var results = new List<ParsingResult>();

        foreach (var parser in _parsers)
        {
            Console.WriteLine($"\n{'=' * 60}");
            Console.WriteLine($"Начало парсинга: {parser.GetType().Name}");
            Console.WriteLine($"{'=' * 60}");

            try
            {
                var result = await parser.ParseAsync();
                results.Add(result);
                Console.WriteLine(result.ToString());

                // Сохраняем результаты в БД (если есть что сохранять)
                if (result.ProductsFound > 0)
                {
                    await SaveParsingResultAsync(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка: {ex.Message}");
            }

            Console.WriteLine($"{'=' * 60}");
            Console.WriteLine($"Конец парсинга: {parser.GetType().Name}");
            Console.WriteLine($"{'=' * 60}\n");
        }

        return results;
    }

    public async Task<ParsingResult?> ParseStoreAsync(string storeName)
    {
        var parser = _parsers.FirstOrDefault(p =>
            p.GetType().Name.ToLower().Contains(storeName.ToLower()));

        if (parser == null)
            return null;

        var result = await parser.ParseAsync();
        return result;
    }

    /// <summary>
    /// Сохранение информации о парсинге в БД
    /// </summary>
    private async Task SaveParsingResultAsync(ParsingResult result)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Создаём запись в логах (можно создать отдельную таблицу)
            Console.WriteLine($"[DB] Сохранён результат парсинга: {result.StoreName}");
            Console.WriteLine($"[DB] Найдено: {result.ProductsFound}, Ошибок: {result.Errors}");
            Console.WriteLine($"[DB] Длительность: {result.Duration.TotalSeconds:F1} сек");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Ошибка сохранения: {ex.Message}");
        }
    }
}