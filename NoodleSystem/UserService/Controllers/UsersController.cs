using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Context;
using UserService.Domain.Entities;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly SpicyNoodleDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(SpicyNoodleDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/users/test
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

    // GET: api/users
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

    // GET: api/users/{id}
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

    // POST: api/users
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
                Password = request.Password, // In production, this should be hashed
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
}

public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? GoogleId { get; set; }
    public int Role { get; set; } = 2; // Default to Customer
}
