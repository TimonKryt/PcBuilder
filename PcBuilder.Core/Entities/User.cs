using Microsoft.AspNetCore.Identity;

namespace PcBuilder.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Связь со сборками
    public ICollection<PcBuild> PcBuilds { get; set; } = new List<PcBuild>();
}