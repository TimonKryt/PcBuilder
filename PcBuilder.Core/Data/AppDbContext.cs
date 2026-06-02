using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PcBuilder.Core.Entities;
using PcBuilder.Core.Enums;

namespace PcBuilder.Core.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductSpecification> ProductSpecifications { get; set; }
    public DbSet<CompatibilityRule> CompatibilityRules { get; set; }
    public DbSet<PcBuild> PcBuilds { get; set; }
    public DbSet<PcBuildComponent> PcBuildComponents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Важно для Identity таблиц

        // Связь User -> PcBuilds
        modelBuilder.Entity<PcBuild>(entity =>
        {
            entity.HasOne(b => b.User)
                .WithMany(u => u.PcBuilds)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(b => b.TotalPrice)
                .HasPrecision(18, 2);
        });

        // Остальные конфигурации...
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => p.ComponentType);
            entity.HasIndex(p => p.Price);
        });

        modelBuilder.Entity<ProductSpecification>(entity =>
        {
            entity.HasOne(s => s.Product)
                .WithMany(p => p.Specifications)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => new { s.Key, s.Value });
        });

        modelBuilder.Entity<CompatibilityRule>(entity =>
        {
            entity.HasOne(r => r.SourceProduct)
                .WithMany(p => p.SourceRules)
                .HasForeignKey(r => r.SourceProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.TargetProduct)
                .WithMany(p => p.TargetRules)
                .HasForeignKey(r => r.TargetProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Категории (Id с 1)
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Процессоры", Description = "Центральные процессоры" },
            new Category { Id = 2, Name = "Видеокарты", Description = "Графические процессоры" },
            new Category { Id = 3, Name = "Материнские платы", Description = "Системные платы" },
            new Category { Id = 4, Name = "Оперативная память", Description = "Модули RAM" },
            new Category { Id = 5, Name = "Блоки питания", Description = "Источники питания" },
            new Category { Id = 6, Name = "Корпуса", Description = "Компьютерные корпуса" },
            new Category { Id = 7, Name = "Системы охлаждения", Description = "Кулеры и СЖО" }
        );

        // Товары (Id с 1 по 17)
        modelBuilder.Entity<Product>().HasData(
            // CPU
            new Product { Id = 1, Name = "Intel Core i5-12400F", Description = "6 ядер, 12 потоков", Price = 14990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.CPU, CategoryId = 1 },
            new Product { Id = 2, Name = "Intel Core i7-13700K", Description = "16 ядер, 24 потока", Price = 38990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.CPU, CategoryId = 1 },
            new Product { Id = 3, Name = "AMD Ryzen 5 7600X", Description = "6 ядер, 12 потоков", Price = 21990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.CPU, CategoryId = 1 },
            // GPU
            new Product { Id = 4, Name = "NVIDIA GeForce RTX 4060", Description = "8 ГБ GDDR6", Price = 32990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.GPU, CategoryId = 2 },
            new Product { Id = 5, Name = "NVIDIA GeForce RTX 4070 Ti", Description = "12 ГБ GDDR6X", Price = 74990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.GPU, CategoryId = 2 },
            new Product { Id = 6, Name = "AMD Radeon RX 7800 XT", Description = "16 ГБ GDDR6", Price = 62990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.GPU, CategoryId = 2 },
            // Motherboard
            new Product { Id = 7, Name = "MSI PRO B660M-A WiFi", Description = "LGA1700, mATX", Price = 12990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.Motherboard, CategoryId = 3 },
            new Product { Id = 8, Name = "ASUS ROG STRIX Z790-F", Description = "LGA1700, ATX", Price = 38990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.Motherboard, CategoryId = 3 },
            new Product { Id = 9, Name = "Gigabyte B650 AORUS ELITE", Description = "AM5, ATX", Price = 22990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.Motherboard, CategoryId = 3 },
            // RAM
            new Product { Id = 10, Name = "Kingston Fury Beast 32GB DDR4", Description = "3200 МГц", Price = 8990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.RAM, CategoryId = 4 },
            new Product { Id = 11, Name = "Corsair Vengeance 32GB DDR5", Description = "5600 МГц", Price = 11990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.RAM, CategoryId = 4 },
            // PSU
            new Product { Id = 12, Name = "Corsair RM850e 850W", Description = "80+ Gold", Price = 10990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.PowerSupply, CategoryId = 5 },
            new Product { Id = 13, Name = "be quiet! Pure Power 12 M 1000W", Description = "80+ Gold, ATX 3.0", Price = 15990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.PowerSupply, CategoryId = 5 },
            // Case
            new Product { Id = 14, Name = "NZXT H5 Flow", Description = "Mid-Tower", Price = 7990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.Case, CategoryId = 6 },
            new Product { Id = 15, Name = "Lian Li LANCOOL III", Description = "Mid-Tower, 4 вентилятора", Price = 11990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.Case, CategoryId = 6 },
            // Cooler
            new Product { Id = 16, Name = "DeepCool AK400", Description = "Башенный, 120 мм", Price = 2990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.Cooler, CategoryId = 7 },
            new Product { Id = 17, Name = "Arctic Liquid Freezer II 360", Description = "СЖО, 360 мм", Price = 8990, ImageUrl = "", StoreUrl = "", StoreName = "DNS", ComponentType = ComponentType.Cooler, CategoryId = 7 }
        );

        // Характеристики
        modelBuilder.Entity<ProductSpecification>().HasData(
            new ProductSpecification { Id = 1, ProductId = 1, Key = "Socket", Value = "LGA1700" },
            new ProductSpecification { Id = 2, ProductId = 1, Key = "TDP", Value = "65" },
            new ProductSpecification { Id = 3, ProductId = 2, Key = "Socket", Value = "LGA1700" },
            new ProductSpecification { Id = 4, ProductId = 2, Key = "TDP", Value = "125" },
            new ProductSpecification { Id = 5, ProductId = 3, Key = "Socket", Value = "AM5" },
            new ProductSpecification { Id = 6, ProductId = 3, Key = "TDP", Value = "105" },
            new ProductSpecification { Id = 7, ProductId = 7, Key = "Socket", Value = "LGA1700" },
            new ProductSpecification { Id = 8, ProductId = 7, Key = "FormFactor", Value = "mATX" },
            new ProductSpecification { Id = 9, ProductId = 8, Key = "Socket", Value = "LGA1700" },
            new ProductSpecification { Id = 10, ProductId = 8, Key = "FormFactor", Value = "ATX" },
            new ProductSpecification { Id = 11, ProductId = 9, Key = "Socket", Value = "AM5" },
            new ProductSpecification { Id = 12, ProductId = 9, Key = "FormFactor", Value = "ATX" },
            new ProductSpecification { Id = 13, ProductId = 12, Key = "Wattage", Value = "850" },
            new ProductSpecification { Id = 14, ProductId = 13, Key = "Wattage", Value = "1000" },
            new ProductSpecification { Id = 15, ProductId = 14, Key = "SupportedFormFactors", Value = "ATX,mATX,ITX" },
            new ProductSpecification { Id = 16, ProductId = 15, Key = "SupportedFormFactors", Value = "E-ATX,ATX,mATX,ITX" }
        );

        // Правила совместимости
        modelBuilder.Entity<CompatibilityRule>().HasData(
            new CompatibilityRule { Id = 1, RuleType = "Requires", SourceProductId = 1, SourceSpecKey = "Socket", TargetSpecKey = "Socket", Condition = "Equals" },
            new CompatibilityRule { Id = 2, RuleType = "Requires", SourceProductId = 2, SourceSpecKey = "Socket", TargetSpecKey = "Socket", Condition = "Equals" },
            new CompatibilityRule { Id = 3, RuleType = "Requires", SourceProductId = 3, SourceSpecKey = "Socket", TargetSpecKey = "Socket", Condition = "Equals" },
            new CompatibilityRule { Id = 4, RuleType = "Requires", SourceProductId = 2, SourceSpecKey = "TDP", TargetSpecKey = "Wattage", Condition = "GreaterThan" }
        );
    }
}