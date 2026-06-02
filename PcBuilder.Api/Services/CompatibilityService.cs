using Microsoft.EntityFrameworkCore;
using PcBuilder.Core.Data;
using PcBuilder.Core.Entities;
using PcBuilder.Core.Enums;

namespace PcBuilder.Api.Services;

public class CompatibilityService
{
    private readonly AppDbContext _context;

    public CompatibilityService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Получает список продуктов, совместимых с выбранным продуктом
    /// </summary>
    public async Task<List<Product>> GetCompatibleProducts(int productId, ComponentType targetType)
    {
        var sourceProduct = await _context.Products
            .Include(p => p.Specifications)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (sourceProduct == null)
            return new List<Product>();

        // Получаем все продукты нужного типа
        var candidates = await _context.Products
            .Include(p => p.Specifications)
            .Where(p => p.ComponentType == targetType)
            .ToListAsync();

        // Получаем правила совместимости
        var rules = await _context.CompatibilityRules
            .Where(r => r.SourceProductId == productId)
            .ToListAsync();

        // Фильтруем кандидатов
        var compatible = new List<Product>();

        foreach (var candidate in candidates)
        {
            if (IsCompatible(sourceProduct, candidate, rules))
            {
                compatible.Add(candidate);
            }
        }

        return compatible;
    }

    private bool IsCompatible(Product source, Product target, List<CompatibilityRule> rules)
    {
        // Если нет правил - считаем совместимым
        if (!rules.Any())
            return true;

        foreach (var rule in rules)
        {
            var sourceSpec = source.Specifications
                .FirstOrDefault(s => s.Key == rule.SourceSpecKey);
            var targetSpec = target.Specifications
                .FirstOrDefault(s => s.Key == rule.TargetSpecKey);

            if (sourceSpec == null || targetSpec == null)
                continue;

            // Проверяем правило
            var isRuleSatisfied = rule.Condition switch
            {
                "Equals" => targetSpec.Value.Equals(sourceSpec.Value, StringComparison.OrdinalIgnoreCase),
                "Contains" => targetSpec.Value.Contains(sourceSpec.Value, StringComparison.OrdinalIgnoreCase),
                "GreaterThan" => decimal.TryParse(targetSpec.Value, out var tVal) &&
                                decimal.TryParse(sourceSpec.Value, out var sVal) &&
                                tVal > sVal,
                "LessThan" => decimal.TryParse(targetSpec.Value, out var tVal2) &&
                               decimal.TryParse(sourceSpec.Value, out var sVal2) &&
                               tVal2 < sVal2,
                _ => true
            };

            // Если правило "Requires" - проверяем удовлетворение
            if (rule.RuleType == "Requires" && !isRuleSatisfied)
                return false;

            // Если правило "Excludes" - проверяем отсутствие
            if (rule.RuleType == "Excludes" && isRuleSatisfied)
                return false;
        }

        return true;
    }
}