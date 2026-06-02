using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PcBuilder.Core.Data;
using PcBuilder.Core.Entities;
using PcBuilder.Core.Enums;
using PcBuilder.Api.Services;
using System.Security.Claims;

namespace PcBuilder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BuilderController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CompatibilityService _compatibilityService;

    public BuilderController(AppDbContext context, CompatibilityService compatibilityService)
    {
        _context = context;
        _compatibilityService = compatibilityService;
    }

    [HttpGet("component-types")]
    public IActionResult GetComponentTypes()
    {
        var types = Enum.GetValues<ComponentType>()
            .Select(t => new { Id = (int)t, Name = t.ToString() })
            .ToList();
        return Ok(types);
    }

    [HttpGet("products/{componentType}")]
    public async Task<IActionResult> GetProducts(ComponentType componentType)
    {
        var products = await _context.Products
            .Include(p => p.Specifications)
            .Where(p => p.ComponentType == componentType)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.ImageUrl,
                p.StoreName,
                Specifications = p.Specifications.Select(s => new { s.Key, s.Value })
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("compatible/{productId}/{targetType}")]
    public async Task<IActionResult> GetCompatible(int productId, ComponentType targetType)
    {
        var compatible = await _compatibilityService.GetCompatibleProducts(productId, targetType);

        var result = compatible.Select(p => new
        {
            p.Id,
            p.Name,
            p.Price,
            p.ImageUrl,
            p.StoreName,
            Specifications = p.Specifications.Select(s => new { s.Key, s.Value })
        });

        return Ok(result);
    }

    /// <summary>
    /// Сохранение сборки (только для авторизованных)
    /// </summary>
    [Authorize]
    [HttpPost("builds")]
    public async Task<IActionResult> CreateBuild([FromBody] CreateBuildRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var build = new PcBuild
        {
            Name = request.Name,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        decimal totalPrice = 0;

        foreach (var componentId in request.ComponentIds)
        {
            var product = await _context.Products.FindAsync(componentId);
            if (product == null)
                return BadRequest($"Продукт с ID {componentId} не найден");

            build.Components.Add(new PcBuildComponent
            {
                ProductId = componentId
            });

            totalPrice += product.Price;
        }

        build.TotalPrice = totalPrice;

        // Проверка совместимости
        var compatibilityWarnings = await CheckBuildCompatibility(build);

        _context.PcBuilds.Add(build);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            build.Id,
            build.Name,
            build.TotalPrice,
            Components = build.Components.Select(c => new { c.ProductId }),
            CompatibilityWarnings = compatibilityWarnings
        });
    }

    /// <summary>
    /// Получение сборки по ID
    /// </summary>
    [Authorize]
    [HttpGet("builds/{id}")]
    public async Task<IActionResult> GetBuild(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var build = await _context.PcBuilds
            .Include(b => b.Components)
                .ThenInclude(c => c.Product)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (build == null)
            return NotFound();

        return Ok(new
        {
            build.Id,
            build.Name,
            build.TotalPrice,
            build.CreatedAt,
            Components = build.Components.Select(c => new
            {
                c.ProductId,
                Product = new
                {
                    c.Product.Id,
                    c.Product.Name,
                    c.Product.ComponentType,
                    c.Product.Price
                }
            })
        });
    }

    /// <summary>
    /// Получение сборок текущего пользователя
    /// </summary>
    [Authorize]
    [HttpGet("builds")]
    public async Task<IActionResult> GetMyBuilds()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var builds = await _context.PcBuilds
            .Include(b => b.Components)
            .ThenInclude(c => c.Product)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.TotalPrice,
                b.CreatedAt,
                Components = b.Components.Select(c => new
                {
                    c.ProductId,
                    c.Product.Name,
                    c.Product.ComponentType,
                    c.Product.Price
                })
            })
            .ToListAsync();

        return Ok(builds);
    }

    /// <summary>
    /// Проверка совместимости сборки
    /// </summary>
    [HttpPost("check-compatibility")]
    public async Task<IActionResult> CheckCompatibility([FromBody] List<int> componentIds)
    {
        var products = await _context.Products
            .Include(p => p.Specifications)
            .Where(p => componentIds.Contains(p.Id))
            .ToListAsync();

        var warnings = new List<string>();
        var errors = new List<string>();

        // Проверяем попарно все компоненты
        for (int i = 0; i < products.Count; i++)
        {
            for (int j = i + 1; j < products.Count; j++)
            {
                var source = products[i];
                var target = products[j];

                // Проверяем правила совместимости
                var rules = await _context.CompatibilityRules
                    .Where(r => r.SourceProductId == source.Id || r.SourceProductId == target.Id)
                    .ToListAsync();

                foreach (var rule in rules)
                {
                    var sourceSpec = source.Specifications
                        .FirstOrDefault(s => s.Key == rule.SourceSpecKey);
                    var targetSpec = target.Specifications
                        .FirstOrDefault(s => s.Key == rule.TargetSpecKey);

                    if (sourceSpec == null || targetSpec == null)
                        continue;

                    var isCompatible = CheckRule(rule, sourceSpec.Value, targetSpec.Value);

                    if (!isCompatible)
                    {
                        if (rule.RuleType == "Requires")
                        {
                            errors.Add($"{source.Name} несовместим с {target.Name}: " +
                                      $"{rule.SourceSpecKey} ({sourceSpec.Value}) не подходит к " +
                                      $"{rule.TargetSpecKey} ({targetSpec.Value})");
                        }
                        else if (rule.RuleType == "Excludes")
                        {
                            warnings.Add($"{source.Name} и {target.Name} не рекомендуются вместе");
                        }
                    }
                }
            }
        }

        return Ok(new { IsCompatible = !errors.Any(), Errors = errors, Warnings = warnings });
    }

    private bool CheckRule(CompatibilityRule rule, string sourceValue, string targetValue)
    {
        return rule.Condition switch
        {
            "Equals" => targetValue.Equals(sourceValue, StringComparison.OrdinalIgnoreCase),
            "Contains" => targetValue.Contains(sourceValue, StringComparison.OrdinalIgnoreCase),
            "GreaterThan" => decimal.TryParse(targetValue, out var tVal) &&
                            decimal.TryParse(sourceValue, out var sVal) &&
                            tVal > sVal,
            _ => true
        };
    }

    private async Task<List<string>> CheckBuildCompatibility(PcBuild build)
    {
        var warnings = new List<string>();
        var products = build.Components
            .Select(c => c.Product)
            .Where(p => p != null)
            .ToList();

        for (int i = 0; i < products.Count; i++)
        {
            for (int j = i + 1; j < products.Count; j++)
            {
                var rules = await _context.CompatibilityRules
                    .Where(r => r.SourceProductId == products[i].Id)
                    .ToListAsync();

                foreach (var rule in rules)
                {
                    var sourceSpec = products[i].Specifications
                        .FirstOrDefault(s => s.Key == rule.SourceSpecKey);
                    var targetSpec = products[j].Specifications
                        .FirstOrDefault(s => s.Key == rule.TargetSpecKey);

                    if (sourceSpec != null && targetSpec != null)
                    {
                        if (!CheckRule(rule, sourceSpec.Value, targetSpec.Value))
                        {
                            warnings.Add($"{products[i].Name} и {products[j].Name} могут быть несовместимы");
                        }
                    }
                }
            }
        }

        return warnings;
    }
}

public class CreateBuildRequest
{
    public string Name { get; set; } = string.Empty;
    public List<int> ComponentIds { get; set; } = new();
}