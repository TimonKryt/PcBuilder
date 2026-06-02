namespace PcBuilder.Core.Entities;

public class CompatibilityRule
{
    public int Id { get; set; }
    public string RuleType { get; set; } = string.Empty; // "Requires", "Excludes", "ChecksSpec"
    public string SourceSpecKey { get; set; } = string.Empty;
    public string SourceSpecValue { get; set; } = string.Empty;
    public string TargetSpecKey { get; set; } = string.Empty;
    public string TargetSpecValue { get; set; } = string.Empty;
    public string? Condition { get; set; } // "Equals", "GreaterThan", "Contains"

    // Внешние ключи (опционально, для прямых связей между продуктами)
    public int? SourceProductId { get; set; }
    public int? TargetProductId { get; set; }

    // Навигационные свойства
    public Product? SourceProduct { get; set; }
    public Product? TargetProduct { get; set; }
}