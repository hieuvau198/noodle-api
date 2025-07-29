using Microsoft.AspNetCore.Mvc;
using UserService2.Application.Dtos;
using UserService2.Application.Services;

namespace UserService2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUsersService _usersService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUsersService usersService, ILogger<AuthController> logger)
    {
        _usersService = usersService;
        _logger = logger;
    }

    /// <summary>
    /// Đăng nhập bằng email và password
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _usersService.LoginAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email: {Email}", request.Email);
            return StatusCode(500, new { message = "Login failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Đăng ký tài khoản mới
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _usersService.RegisterAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email: {Email}", request.Email);
            return StatusCode(500, new { message = "Registration failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Đăng nhập bằng Google ID Token
    /// </summary>
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthRequest request)
    {
        try
        {
            var result = await _usersService.GoogleLoginAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google login failed");
            return StatusCode(500, new { message = "Google login failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Quên mật khẩu - gửi email reset
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var result = await _usersService.ForgotPasswordAsync(request.Email);
            
            // Always return success for security reasons
            return Ok(new { message = "If the email exists, a temporary password has been sent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Forgot password failed for email: {Email}", request.Email);
            return StatusCode(500, new { message = "Forgot password failed", error = ex.Message });
        }
    }
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = null!;
} 