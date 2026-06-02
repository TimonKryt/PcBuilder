using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PcBuilder.Core.Data;
using PcBuilder.Core.Entities;
using PcBuilder.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Настройка БД
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var dbPath = Path.Combine(AppContext.BaseDirectory, "pcbuilder.db");
    options.UseSqlite($"Data Source={dbPath}");
});

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SuperSecretKey12345!ChangeInProduction";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// Сервисы
builder.Services.AddScoped<CompatibilityService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddSingleton<ParsingService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Создание БД и таблиц Identity
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Удаляем старую БД если есть (только для разработки!)
    // File.Delete(Path.Combine(AppContext.BaseDirectory, "pcbuilder.db"));

    // Создаём БД и все таблицы
    await dbContext.Database.EnsureCreatedAsync();

    // Явно создаём таблицы Identity, если их нет
    await CreateIdentityTablesAsync(dbContext);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

// Функция создания таблиц Identity вручную
async Task CreateIdentityTablesAsync(AppDbContext context)
{
    try
    {
        // Проверяем, есть ли таблица AspNetUsers
        var tableExists = await context.Database.ExecuteSqlRawAsync(
            "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='AspNetUsers';");

        if (tableExists == 0)
        {
            Console.WriteLine("[DB] Создание таблиц Identity...");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS AspNetRoles (
                    Id TEXT NOT NULL PRIMARY KEY,
                    Name TEXT NULL,
                    NormalizedName TEXT NULL,
                    ConcurrencyStamp TEXT NULL
                );
                
                CREATE TABLE IF NOT EXISTS AspNetUsers (
                    Id TEXT NOT NULL PRIMARY KEY,
                    UserName TEXT NULL,
                    NormalizedUserName TEXT NULL,
                    Email TEXT NULL,
                    NormalizedEmail TEXT NULL,
                    EmailConfirmed INTEGER NOT NULL DEFAULT 0,
                    PasswordHash TEXT NULL,
                    SecurityStamp TEXT NULL,
                    ConcurrencyStamp TEXT NULL,
                    PhoneNumber TEXT NULL,
                    PhoneNumberConfirmed INTEGER NOT NULL DEFAULT 0,
                    TwoFactorEnabled INTEGER NOT NULL DEFAULT 0,
                    LockoutEnd TEXT NULL,
                    LockoutEnabled INTEGER NOT NULL DEFAULT 1,
                    AccessFailedCount INTEGER NOT NULL DEFAULT 0,
                    RefreshToken TEXT NULL,
                    RefreshTokenExpiryTime TEXT NOT NULL DEFAULT '0001-01-01 00:00:00',
                    CreatedAt TEXT NOT NULL DEFAULT '0001-01-01 00:00:00'
                );
                
                CREATE TABLE IF NOT EXISTS AspNetRoleClaims (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    RoleId TEXT NOT NULL,
                    ClaimType TEXT NULL,
                    ClaimValue TEXT NULL,
                    FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE
                );
                
                CREATE TABLE IF NOT EXISTS AspNetUserClaims (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    UserId TEXT NOT NULL,
                    ClaimType TEXT NULL,
                    ClaimValue TEXT NULL,
                    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
                );
                
                CREATE TABLE IF NOT EXISTS AspNetUserLogins (
                    LoginProvider TEXT NOT NULL,
                    ProviderKey TEXT NOT NULL,
                    ProviderDisplayName TEXT NULL,
                    UserId TEXT NOT NULL,
                    PRIMARY KEY (LoginProvider, ProviderKey),
                    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
                );
                
                CREATE TABLE IF NOT EXISTS AspNetUserRoles (
                    UserId TEXT NOT NULL,
                    RoleId TEXT NOT NULL,
                    PRIMARY KEY (UserId, RoleId),
                    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
                    FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE
                );
                
                CREATE TABLE IF NOT EXISTS AspNetUserTokens (
                    UserId TEXT NOT NULL,
                    LoginProvider TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Value TEXT NULL,
                    PRIMARY KEY (UserId, LoginProvider, Name),
                    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
                );
                
                CREATE INDEX IF NOT EXISTS IX_AspNetRoles_NormalizedName ON AspNetRoles (NormalizedName);
                CREATE INDEX IF NOT EXISTS IX_AspNetUsers_NormalizedUserName ON AspNetUsers (NormalizedUserName);
                CREATE INDEX IF NOT EXISTS IX_AspNetUsers_NormalizedEmail ON AspNetUsers (NormalizedEmail);
                CREATE INDEX IF NOT EXISTS IX_AspNetRoleClaims_RoleId ON AspNetRoleClaims (RoleId);
                CREATE INDEX IF NOT EXISTS IX_AspNetUserClaims_UserId ON AspNetUserClaims (UserId);
                CREATE INDEX IF NOT EXISTS IX_AspNetUserLogins_UserId ON AspNetUserLogins (UserId);
            ");

            Console.WriteLine("[DB] Таблицы Identity созданы успешно");
        }
        else
        {
            Console.WriteLine("[DB] Таблицы Identity уже существуют");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB] Ошибка создания таблиц Identity: {ex.Message}");
    }
}