using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PcBuilder.Core.DTOs;
using PcBuilder.Core.Entities;
using PcBuilder.Api.Services;

namespace PcBuilder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtService _jwtService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtService jwtService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        // Логируем входящие данные
        Console.WriteLine($"[AUTH] Попытка регистрации: Email={dto.Email}, Username={dto.Username}");

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            Console.WriteLine($"[AUTH] Ошибки валидации: {string.Join(", ", errors)}");
            return BadRequest(new { errors });
        }

        var user = new ApplicationUser
        {
            UserName = dto.Username,
            Email = dto.Email,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            Console.WriteLine($"[AUTH] Ошибки Identity: {string.Join(", ", errors)}");
            return BadRequest(new { errors });
        }

        Console.WriteLine($"[AUTH] Пользователь создан: {user.UserName}");

        var token = await _jwtService.GenerateTokenAsync(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            Username = user.UserName,
            Email = user.Email,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        Console.WriteLine($"[AUTH] Попытка входа: Email={dto.Email}");

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            Console.WriteLine($"[AUTH] Ошибки валидации: {string.Join(", ", errors)}");
            return BadRequest(new { errors });
        }

        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user == null)
        {
            Console.WriteLine($"[AUTH] Пользователь не найден: {dto.Email}");
            return Unauthorized(new { message = "Неверный email или пароль" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);

        if (!result.Succeeded)
        {
            Console.WriteLine($"[AUTH] Неверный пароль для: {dto.Email}");
            return Unauthorized(new { message = "Неверный email или пароль" });
        }

        Console.WriteLine($"[AUTH] Успешный вход: {user.UserName}");

        var token = await _jwtService.GenerateTokenAsync(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            Username = user.UserName ?? "",
            Email = user.Email ?? "",
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        });
    }

    /// <summary>
    /// Получение информации о текущем пользователе
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.CreatedAt
        });
    }
}