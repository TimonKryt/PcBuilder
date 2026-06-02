namespace PcBuilder.Core.Entities;

public class PcBuild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public decimal TotalPrice { get; set; }
    public bool IsPublic { get; set; } = false;

    // Навигационные свойства
    public ApplicationUser User { get; set; } = null!;
    public ICollection<PcBuildComponent> Components { get; set; } = new List<PcBuildComponent>();
}

public class PcBuildComponent
{
    public int Id { get; set; }
    public int PcBuildId { get; set; }
    public int ProductId { get; set; }

    public PcBuild PcBuild { get; set; } = null!;
    public Product Product { get; set; } = null!;
}