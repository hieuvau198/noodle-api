using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Context;
using UserService.Domain.Entities;
using UserService.Application.Services;
using UserService.Application.Dtos;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly SpicyNoodleDbContext _context;
    private readonly ILogger<UsersController> _logger;
    private readonly IGoogleAuthService _googleAuthService;

    public UsersController(SpicyNoodleDbContext context, ILogger<UsersController> logger, IGoogleAuthService googleAuthService)
    {
        _context = context;
        _logger = logger;
        _googleAuthService = googleAuthService;
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var userCount = await _context.Users.CountAsync();
            return Ok(new { 
                message = "Database connection successful!", 
                userCount = userCount,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return StatusCode(500, new { 
                message = "Database connection failed", 
                error = ex.Message,
                timestamp = DateTime.UtcNow 
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        try
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.IsGoogleUser,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, new { message = "Error retrieving users", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        try
        {
            var user = await _context.Users
                .Where(u => u.UserId == id && u.IsActive)
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.IsGoogleUser,
                    u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, new { message = "Error retrieving user", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Password = request.Password,
                GoogleId = request.GoogleId,
                Role = request.Role,
                IsGoogleUser = !string.IsNullOrEmpty(request.GoogleId),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, new
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.Role,
                user.IsGoogleUser,
                user.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "Error creating user", error = ex.Message });
        }
    }

    [HttpPost("google-login")]
    public async Task<ActionResult<AuthResult>> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            _logger.LogInformation("Google login request received");
            
            if (request == null)
            {
                _logger.LogWarning("Request body is null");
                return BadRequest(new { message = "Request body is required" });
            }

            if (string.IsNullOrEmpty(request.IdToken))
            {
                _logger.LogWarning("ID token is empty or null");
                return BadRequest(new { message = "ID token is required" });
            }

            _logger.LogInformation($"Processing Google login with token length: {request.IdToken.Length}");
            var result = await _googleAuthService.AuthenticateGoogleUserAsync(request.IdToken);
            
            if (result.Success)
            {
                _logger.LogInformation("Google login successful");
                return Ok(result);
            }
            else
            {
                _logger.LogWarning($"Google login failed: {result.Message}");
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google login");
            return StatusCode(500, new { message = "Error during Google login", error = ex.Message });
        }
    }
}

public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? GoogleId { get; set; }
    public int Role { get; set; } = 2;
}

public class GoogleLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
}
