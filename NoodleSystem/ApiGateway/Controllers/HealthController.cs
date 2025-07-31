using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ApiGateway.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { 
            Status = "Healthy", 
            Service = "API Gateway", 
            Timestamp = DateTime.UtcNow,
            Message = "API Gateway is running and ready to route requests to microservices"
        });
    }

    [HttpGet("validate-token")]
    [Authorize]
    public IActionResult ValidateToken()
    {
        var user = User;
        return Ok(new {
            Status = "Token Valid",
            UserId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            Email = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            Name = user.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
            Role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value,
            IsGoogleUser = user.FindFirst("IsGoogleUser")?.Value,
            Timestamp = DateTime.UtcNow
        });
    }
} 