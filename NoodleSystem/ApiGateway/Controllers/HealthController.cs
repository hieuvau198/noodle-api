using Microsoft.AspNetCore.Mvc;

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
} 