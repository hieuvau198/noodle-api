using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IHttpClientFactory clientFactory, ILogger<UsersController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        try
        {
            var client = _clientFactory.CreateClient("UserService");
            var response = await client.GetAsync($"api/users/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, 
                    new { message = "Error retrieving user from user service" });
            }

            var user = await response.Content.ReadFromJsonAsync<object>();
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gateway while retrieving user {UserId}", id);
            return StatusCode(500, new { message = "Gateway error", error = ex.Message });
        }
    }
}