namespace PcBuilder.Core.Models;

/// <summary>
/// Результат парсинга одного магазина
/// </summary>
public class ParsingResult
{
    public string StoreName { get; set; } = string.Empty;
    public int ProductsFound { get; set; }
    public int ProductsAdded { get; set; }
    public int ProductsUpdated { get; set; }
    public int Errors { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public List<string> ErrorMessages { get; set; } = new();

    public TimeSpan Duration => CompletedAt - StartedAt;

    public override string ToString()
    {
        return $"{StoreName}: найдено {ProductsFound}, добавлено {ProductsAdded}, " +
               $"обновлено {ProductsUpdated}, ошибок {Errors} (за {Duration.TotalSeconds:F1} сек)";
    }
}