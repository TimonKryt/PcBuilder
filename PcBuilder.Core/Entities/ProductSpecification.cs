namespace PcBuilder.Core.Entities;

public class ProductSpecification
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    // Внешний ключ
    public int ProductId { get; set; }

    // Навигационное свойство
    public Product Product { get; set; } = null!;
}