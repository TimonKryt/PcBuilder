using Microsoft.EntityFrameworkCore;
using PcBuilder.Api.Services;
using PcBuilder.Core.Data;
using PcBuilder.Core.Entities;
using PcBuilder.Core.Enums;

namespace PcBuilder.Tests;

public class CompatibilityServiceTests
{
    /// <summary>
    /// Создаёт новый контекст с уникальной in-memory базой данных для каждого теста
    /// </summary>
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Тест: Процессор Intel и материнская плата с одинаковым сокетом должны быть совместимы
    /// </summary>
    [Fact]
    public async Task GetCompatibleProducts_WithMatchingSocket_ReturnsCompatibleProducts()
    {
        // Arrange
        var context = CreateContext();
        var service = new CompatibilityService(context);

        var cpu = new Product
        {
            Name = "Intel Core i5-12400F",
            ComponentType = ComponentType.CPU,
            CategoryId = 1,
            Price = 15000
        };
        context.Products.Add(cpu);
        await context.SaveChangesAsync();

        context.ProductSpecifications.Add(new ProductSpecification
        {
            ProductId = cpu.Id,
            Key = "Socket",
            Value = "LGA1700"
        });

        var motherboard = new Product
        {
            Name = "MSI PRO B660M-A",
            ComponentType = ComponentType.Motherboard,
            CategoryId = 3,
            Price = 12000
        };
        context.Products.Add(motherboard);
        await context.SaveChangesAsync();

        context.ProductSpecifications.Add(new ProductSpecification
        {
            ProductId = motherboard.Id,
            Key = "Socket",
            Value = "LGA1700"
        });

        context.CompatibilityRules.Add(new CompatibilityRule
        {
            SourceProductId = cpu.Id,
            RuleType = "Requires",
            SourceSpecKey = "Socket",
            TargetSpecKey = "Socket",
            Condition = "Equals"
        });
        await context.SaveChangesAsync();

        // Act
        var compatible = await service.GetCompatibleProducts(cpu.Id, ComponentType.Motherboard);

        // Assert
        Assert.NotEmpty(compatible);
        Assert.Contains(compatible, m => m.Id == motherboard.Id);
    }

    /// <summary>
    /// Тест: Процессор Intel и материнская плата AMD не должны быть совместимы
    /// </summary>
    [Fact]
    public async Task GetCompatibleProducts_WithDifferentSocket_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateContext();
        var service = new CompatibilityService(context);

        var cpu = new Product
        {
            Name = "Intel Core i5-12400F",
            ComponentType = ComponentType.CPU,
            CategoryId = 1,
            Price = 15000
        };
        context.Products.Add(cpu);
        await context.SaveChangesAsync();

        context.ProductSpecifications.Add(new ProductSpecification
        {
            ProductId = cpu.Id,
            Key = "Socket",
            Value = "LGA1700"
        });

        var motherboard = new Product
        {
            Name = "ASUS ROG B650-A",
            ComponentType = ComponentType.Motherboard,
            CategoryId = 3,
            Price = 25000
        };
        context.Products.Add(motherboard);
        await context.SaveChangesAsync();

        context.ProductSpecifications.Add(new ProductSpecification
        {
            ProductId = motherboard.Id,
            Key = "Socket",
            Value = "AM5"
        });

        context.CompatibilityRules.Add(new CompatibilityRule
        {
            SourceProductId = cpu.Id,
            RuleType = "Requires",
            SourceSpecKey = "Socket",
            TargetSpecKey = "Socket",
            Condition = "Equals"
        });
        await context.SaveChangesAsync();

        // Act
        var compatible = await service.GetCompatibleProducts(cpu.Id, ComponentType.Motherboard);

        // Assert
        Assert.Empty(compatible);
    }

    /// <summary>
    /// Тест: Блок питания с достаточной мощностью совместим с процессором
    /// </summary>
    [Fact]
    public async Task GetCompatibleProducts_PowerSupplyWattageGreaterThanCPUTDP_ReturnsCompatible()
    {
        // Arrange
        var context = CreateContext();
        var service = new CompatibilityService(context);

        var cpu = new Product
        {
            Name = "Intel Core i9-13900K",
            ComponentType = ComponentType.CPU,
            CategoryId = 1,
            Price = 45000
        };
        context.Products.Add(cpu);
        await context.SaveChangesAsync();

        context.ProductSpecifications.Add(new ProductSpecification
        {
            ProductId = cpu.Id,
            Key = "TDP",
            Value = "125"
        });

        var psu = new Product
        {
            Name = "Corsair RM850x",
            ComponentType = ComponentType.PowerSupply,
            CategoryId = 5,
            Price = 12000
        };
        context.Products.Add(psu);
        await context.SaveChangesAsync();

        context.ProductSpecifications.Add(new ProductSpecification
        {
            ProductId = psu.Id,
            Key = "Wattage",
            Value = "850"
        });

        context.CompatibilityRules.Add(new CompatibilityRule
        {
            SourceProductId = cpu.Id,
            RuleType = "Requires",
            SourceSpecKey = "TDP",
            TargetSpecKey = "Wattage",
            Condition = "GreaterThan"
        });
        await context.SaveChangesAsync();

        // Act
        var compatible = await service.GetCompatibleProducts(cpu.Id, ComponentType.PowerSupply);

        // Assert
        Assert.NotEmpty(compatible);
        Assert.Contains(compatible, p => p.Id == psu.Id);
    }

    /// <summary>
    /// Тест: Блок питания с недостаточной мощностью не совместим
    /// </summary>
    [Fact]
    public async Task GetCompatibleProducts_PowerSupplyWattageLessThanCPUTDP_ReturnsEmpty()
    {
        // Arrange
        var context = CreateContext();
        var service = new CompatibilityService(context);

        var cpu = new Product
        {
            Name = "Intel Core i9-13900K",
            ComponentType = ComponentType.CPU,
            CategoryId = 1,
            Price = 45000
        };
        context.Products.Add(cpu);
        await context.SaveChangesAsync();

        context.ProductSpecifications.Add(new ProductSpecification
        {
            ProductId = cpu.Id,
            Key = "TDP",
            Value = "125"
        });

        var psu = new Product
        {
            Name = "NoName 300W",
            ComponentType = ComponentType.PowerSupply,
            CategoryId = 5,
            Price = 1500
        };
        context.Products.Add(psu);
        await context.SaveChangesAsync();

        context.ProductSpecifications.Add(new ProductSpecification
        {
            ProductId = psu.Id,
            Key = "Wattage",
            Value = "300"
        });

        context.CompatibilityRules.Add(new CompatibilityRule
        {
            SourceProductId = cpu.Id,
            RuleType = "Requires",
            SourceSpecKey = "TDP",
            TargetSpecKey = "Wattage",
            Condition = "GreaterThan"
        });
        await context.SaveChangesAsync();

        // Act
        var compatible = await service.GetCompatibleProducts(cpu.Id, ComponentType.PowerSupply);

        // Assert
        Assert.DoesNotContain(compatible, p => p.Name.Contains("300W"));
    }

    /// <summary>
    /// Тест: Корпус поддерживает форм-фактор материнской платы
    /// </summary>
    [Fact]
    public async Task GetCompatibleProducts_CaseSupportsMotherboardFormFactor_ReturnsCompatible()
    {
        // Arrange
        var context = CreateContext();
        var service = new CompatibilityService(context);

        var motherboard = new Product
        {
            Name = "ASUS ROG MAXIMUS",
            ComponentType = ComponentType.Motherboard,
            CategoryId = 3,
            Price = 35000
        };
        context.Products.Add(motherboard);
        await context.SaveChangesAsync();

        context.ProductSpecifications.Add(new ProductSpecification
        {
            ProductId = motherboard.Id,
            Key = "FormFactor",
            Value = "ATX"
        });

        var pcCase = new Product
        {
            Name = "NZXT H510",
            ComponentType = ComponentType.Case,
            CategoryId = 6,
            Price = 7000
        };
        context.Products.Add(pcCase);
        await context.SaveChangesAsync();

        context.ProductSpecifications.Add(new ProductSpecification
        {
            ProductId = pcCase.Id,
            Key = "SupportedFormFactors",
            Value = "ATX,mATX,ITX"
        });

        context.CompatibilityRules.Add(new CompatibilityRule
        {
            SourceProductId = motherboard.Id,
            RuleType = "Requires",
            SourceSpecKey = "FormFactor",
            TargetSpecKey = "SupportedFormFactors",
            Condition = "Contains"
        });
        await context.SaveChangesAsync();

        // Act
        var compatible = await service.GetCompatibleProducts(motherboard.Id, ComponentType.Case);

        // Assert
        Assert.NotEmpty(compatible);
        Assert.Contains(compatible, c => c.Id == pcCase.Id);
    }

    /// <summary>
    /// Тест: Правило исключения - определённый чипсет исключается
    /// </summary>
    [Fact]
    public async Task GetCompatibleProducts_WithExclusionRule_ExcludesProduct()
    {
        // Arrange
        var context = CreateContext();
        var service = new CompatibilityService(context);

        var cpu = new Product
        {
            Name = "AMD Ryzen 7 7800X3D",
            ComponentType = ComponentType.CPU,
            CategoryId = 1,
            Price = 35000
        };
        context.Products.Add(cpu);
        await context.SaveChangesAsync();

        context.ProductSpecifications.Add(new ProductSpecification
        {
            ProductId = cpu.Id,
            Key = "Socket",
            Value = "AM5"
        });

        var goodMotherboard = new Product
        {
            Name = "ASUS B650",
            ComponentType = ComponentType.Motherboard,
            CategoryId = 3,
            Price = 18000
        };
        context.Products.Add(goodMotherboard);
        await context.SaveChangesAsync();

        context.ProductSpecifications.Add(new ProductSpecification
        {
            ProductId = goodMotherboard.Id,
            Key = "Chipset",
            Value = "B650"
        });

        var badMotherboard = new Product
        {
            Name = "Gigabyte A620",
            ComponentType = ComponentType.Motherboard,
            CategoryId = 3,
            Price = 9000
        };
        context.Products.Add(badMotherboard);
        await context.SaveChangesAsync();

        context.ProductSpecifications.Add(new ProductSpecification
        {
            ProductId = badMotherboard.Id,
            Key = "Chipset",
            Value = "A620"
        });

        // Правило: исключаем чипсет A620
        context.CompatibilityRules.Add(new CompatibilityRule
        {
            SourceProductId = cpu.Id,
            RuleType = "Excludes",
            SourceSpecKey = "Socket",
            SourceSpecValue = "AM5",
            TargetSpecKey = "Chipset",
            TargetSpecValue = "A620",
            Condition = "Equals"
        });
        await context.SaveChangesAsync();

        // Act
        var compatible = await service.GetCompatibleProducts(cpu.Id, ComponentType.Motherboard);

        // Assert
        Assert.Contains(compatible, m => m.Id == goodMotherboard.Id);
        Assert.DoesNotContain(compatible, m => m.Id == badMotherboard.Id);
    }

    /// <summary>
    /// Тест: Несуществующий ID продукта возвращает пустой список
    /// </summary>
    [Fact]
    public async Task GetCompatibleProducts_WithInvalidProductId_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateContext();
        var service = new CompatibilityService(context);

        // Act
        var compatible = await service.GetCompatibleProducts(999, ComponentType.Motherboard);

        // Assert
        Assert.Empty(compatible);
    }

    /// <summary>
    /// Тест: Если нет правил совместимости, возвращаются все продукты целевого типа
    /// </summary>
    [Fact]
    public async Task GetCompatibleProducts_WithoutRules_ReturnsAllProductsOfTargetType()
    {
        // Arrange
        var context = CreateContext();
        var service = new CompatibilityService(context);

        var cpu = new Product
        {
            Name = "Intel Core i5-12400F",
            ComponentType = ComponentType.CPU,
            CategoryId = 1,
            Price = 15000
        };
        context.Products.Add(cpu);

        var motherboard1 = new Product
        {
            Name = "MSI B660",
            ComponentType = ComponentType.Motherboard,
            CategoryId = 3,
            Price = 12000
        };
        context.Products.Add(motherboard1);

        var motherboard2 = new Product
        {
            Name = "ASUS Z690",
            ComponentType = ComponentType.Motherboard,
            CategoryId = 3,
            Price = 25000
        };
        context.Products.Add(motherboard2);
        await context.SaveChangesAsync();

        // Act
        var compatible = await service.GetCompatibleProducts(cpu.Id, ComponentType.Motherboard);

        // Assert
        Assert.Equal(2, compatible.Count);
    }
}