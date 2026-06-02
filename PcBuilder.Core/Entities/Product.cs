using System.ComponentModel.DataAnnotations.Schema;
using PcBuilder.Core.Enums;

namespace PcBuilder.Core.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string StoreUrl { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public ComponentType ComponentType { get; set; }

    // Внешние ключи
    public int CategoryId { get; set; }

    // Навигационные свойства
    public Category Category { get; set; } = null!;
    public ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();

    // Связи для правил совместимости (как источник)
    [InverseProperty("SourceProduct")]
    public ICollection<CompatibilityRule> SourceRules { get; set; } = new List<CompatibilityRule>();

    // Связи для правил совместимости (как цель)
    [InverseProperty("TargetProduct")]
    public ICollection<CompatibilityRule> TargetRules { get; set; } = new List<CompatibilityRule>();

    // Связи для сборок ПК
    public ICollection<PcBuildComponent> PcBuildComponents { get; set; } = new List<PcBuildComponent>();
}