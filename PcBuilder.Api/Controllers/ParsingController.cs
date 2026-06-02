using Microsoft.AspNetCore.Mvc;
using PcBuilder.Api.Services;

namespace PcBuilder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParsingController : ControllerBase
{
    private readonly ParsingService _parsingService;

    public ParsingController(ParsingService parsingService)
    {
        _parsingService = parsingService;
    }

    /// <summary>
    /// Запуск парсинга всех магазинов
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartParsing()
    {
        var results = await _parsingService.ParseAllAsync();

        var summary = results.Select(r => new
        {
            r.StoreName,
            r.ProductsFound,
            r.ProductsAdded,
            r.ProductsUpdated,
            r.Errors,
            DurationSeconds = r.Duration.TotalSeconds,
            r.ErrorMessages
        });

        return Ok(new
        {
            Message = "Парсинг завершён",
            TotalStores = results.Count,
            TotalProducts = results.Sum(r => r.ProductsFound),
            Results = summary
        });
    }

    /// <summary>
    /// Запуск парсинга конкретного магазина
    /// </summary>
    [HttpPost("start/{storeName}")]
    public async Task<IActionResult> StartStoreParsing(string storeName)
    {
        var result = await _parsingService.ParseStoreAsync(storeName);

        if (result == null)
            return BadRequest($"Магазин '{storeName}' не найден");

        return Ok(new
        {
            result.StoreName,
            result.ProductsFound,
            result.Duration.TotalSeconds
        });
    }

    /// <summary>
    /// Получение статуса парсинга (заглушка)
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            IsRunning = false,
            LastRun = DateTime.UtcNow.AddHours(-1),
            Message = "Парсинг не запущен"
        });
    }
}