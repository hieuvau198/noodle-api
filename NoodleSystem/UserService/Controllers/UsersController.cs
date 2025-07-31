using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
    private readonly IConfiguration _configuration;

    public UsersController(SpicyNoodleDbContext context, ILogger<UsersController> logger, IGoogleAuthService googleAuthService, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _googleAuthService = googleAuthService;
        _configuration = configuration;
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

    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login request received for email: {Email}", request.Email);

            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }

            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found for email {Email}", request.Email);
                return BadRequest(new { message = "Invalid email or password" });
            }

            // For Google users, they should use Google login
            if (user.IsGoogleUser)
            {
                return BadRequest(new { message = "Please use Google login for this account" });
            }

            // Simple password verification (in production, use proper hashing)
            if (user.Password != request.Password)
            {
                _logger.LogWarning("Login failed: Invalid password for email {Email}", request.Email);
                return BadRequest(new { message = "Invalid email or password" });
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);

            _logger.LogInformation("Login successful for user {UserId}", user.UserId);

            return Ok(new AuthResult
            {
                Success = true,
                Token = token,
                Message = "Login successful",
                User = new UserDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    IsGoogleUser = user.IsGoogleUser
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "Error during login", error = ex.Message });
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

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Secret"] ?? "SpicyNoodleSecretKey12345678901234567890";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("IsGoogleUser", user.IsGoogleUser.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"] ?? "SpicyNoodleAPI",
            audience: jwtSettings["Audience"] ?? "SpicyNoodleClient",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(Convert.ToInt32(jwtSettings["ExpirationInDays"] ?? "7")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
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

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class GoogleLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
}
