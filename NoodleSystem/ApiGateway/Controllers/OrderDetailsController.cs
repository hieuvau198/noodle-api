using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderDetailsController : ControllerBase
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<OrderDetailsController> _logger;

    public OrderDetailsController(IHttpClientFactory clientFactory, ILogger<OrderDetailsController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrderWithUserDetails(int orderId)
    {
        try
        {
            // Get order details
            var orderClient = _clientFactory.CreateClient("OrderService");
            var orderResponse = await orderClient.GetAsync($"api/orders/{orderId}");

            if (!orderResponse.IsSuccessStatusCode)
                return StatusCode((int)orderResponse.StatusCode);

            var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();
            if (order == null)
                return NotFound(new { message = "Order not found" });

            // Get user details
            var userClient = _clientFactory.CreateClient("UserService");
            var userResponse = await userClient.GetAsync($"api/users/{order.UserId}");

            if (!userResponse.IsSuccessStatusCode)
                return StatusCode((int)userResponse.StatusCode);

            var user = await userResponse.Content.ReadFromJsonAsync<UserDto>();
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Combine the results
            return Ok(new
            {
                OrderDetails = order,
                UserDetails = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating order details");
            return StatusCode(500, new { message = "Gateway error", error = ex.Message });
        }
    }
}

public class OrderDto
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    // Add other properties as needed
}

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    // Add other properties as needed
}