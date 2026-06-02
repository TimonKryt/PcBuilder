namespace PcBuilder.Core.Models;

/// <summary>
/// Модель для хранения спарсенных данных о товаре
/// </summary>
public class ParsedProduct
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreUrl { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Dictionary<string, string> Specifications { get; set; } = new();
    public bool InStock { get; set; }
    public DateTime ParsedAt { get; set; } = DateTime.UtcNow;
}