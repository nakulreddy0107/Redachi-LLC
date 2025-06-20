using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedachiApi.Data;
using RedachiApi.Models;
using RedachiApi.Services;
using System.Security.Cryptography;
using System.Text;

namespace RedachiApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;

    public AuthController(ApplicationDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegister dto)
    {
        if (_context.Users.Any(u => u.Email == dto.Email))
        {
            return BadRequest(new { message = "Email already exists" });
        }

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Password_Hash = HashPassword(dto.Password),
            Role = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new { message = "User Registered Successfully" });
    }

    [HttpPost("login")]
    public IActionResult Login(UserLogin dto)
    {
        var user = _context.Users.SingleOrDefault(u => u.Email == dto.Email);
        if (user == null || user.Password_Hash != HashPassword(dto.Password))
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }
        var token = _authService.GenerateToken(user.Email, user.Role);
        return Ok(new { token, role = user.Role });
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

