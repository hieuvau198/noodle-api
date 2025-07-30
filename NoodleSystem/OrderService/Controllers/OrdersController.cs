using Microsoft.AspNetCore.Mvc;
using OrderService.Domain;
using OrderService.Application.Services;
using OrderService.Application.Dtos;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            // Test service connection by getting counts via service methods
            var orders = await _orderService.GetAllOrdersAsync();
            var noodles = await _orderService.GetAvailableNoodlesAsync();
            
            var orderCount = orders.Count();
            var noodleCount = noodles.Count();
            
            return Ok(new { 
                message = "Order service connection successful!", 
                orderCount = orderCount,
                noodleCount = noodleCount,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order service connection test failed");
            return StatusCode(500, new { 
                message = "Order service connection failed", 
                error = ex.Message,
                timestamp = DateTime.UtcNow 
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        try
        {
            var results = await _orderService.GetAllOrdersAsync();

            // Map OrderResult to response format (maintaining API contract)
            var response = results.Select(result => new
            {
                result.OrderId,
                result.UserId,
                result.Status,
                result.TotalAmount,
                result.CreatedAt,
                UpdatedAt = result.CreatedAt, // OrderResult doesn't have UpdatedAt
                ItemCount = result.Items.Count
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            return StatusCode(500, new { message = "Error retrieving orders", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        try
        {
            var result = await _orderService.GetOrderAsync(id);
            
            if (result == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            // Map OrderResult to response format (maintaining API contract)
            return Ok(new
            {
                result.OrderId,
                result.UserId,
                result.Status,
                result.TotalAmount,
                result.CreatedAt,
                UpdatedAt = result.CreatedAt, // OrderResult doesn't have UpdatedAt, using CreatedAt
                OrderItems = result.Items.Select(item => new
                {
                    OrderItemId = item.OrderItemId,
                    NoodleId = item.NoodleId,
                    Quantity = item.Quantity,
                    Subtotal = item.Subtotal,
                    CreatedAt = result.CreatedAt,
                    // Note: Noodle details not available in OrderResult.Items
                    // This is a limitation of the current service structure
                    Noodle = new
                    {
                        NoodleId = item.NoodleId,
                        Name = "N/A", // Not available in service result
                        BasePrice = 0m, // Not available in service result  
                        Description = "N/A" // Not available in service result
                    }
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, new { message = "Error retrieving order", error = ex.Message });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByUser(int userId)
    {
        try
        {
            var results = await _orderService.GetOrdersByUserAsync(userId);

            // Map OrderResult to response format (maintaining API contract)
            var response = results.Select(result => new
            {
                result.OrderId,
                result.UserId,
                result.Status,
                result.TotalAmount,
                result.CreatedAt,
                UpdatedAt = result.CreatedAt, // OrderResult doesn't have UpdatedAt
                ItemCount = result.Items.Count
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for user {UserId}", userId);
            return StatusCode(500, new { message = "Error retrieving orders for user", error = ex.Message });
        }
    }

    [HttpGet("noodles")]
    public async Task<ActionResult<IEnumerable<SpicyNoodle>>> GetNoodles()
    {
        try
        {
            var noodles = await _orderService.GetAvailableNoodlesAsync();

            // Map SpicyNoodle to response format (maintaining API contract)
            var response = noodles.Select(n => new
            {
                n.NoodleId,
                n.Name,
                n.BasePrice,
                n.ImageUrl,
                n.Description,
                n.CreatedAt
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving noodles");
            return StatusCode(500, new { message = "Error retrieving noodles", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            // Map request to command
            var command = new CreateOrderCommand
            {
                UserId = request.UserId,
                Items = request.Items?.Select(item => new CreateOrderItemCommand
                {
                    NoodleId = item.NoodleId,
                    Quantity = item.Quantity
                }).ToList() ?? new()
            };

            // Use OrderService for business logic and event-driven payment creation
            var result = await _orderService.CreateOrderAsync(command);

            // Return response maintaining API contract
            return CreatedAtAction(nameof(GetOrder), new { id = result.OrderId }, new
            {
                result.OrderId,
                result.UserId,
                result.Status,
                result.TotalAmount,
                result.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new { message = "Error creating order", error = ex.Message });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            var success = await _orderService.UpdateOrderStatusAsync(id, request.Status);
            
            if (!success)
            {
                return NotFound(new { message = "Order not found" });
            }

            return Ok(new { 
                message = "Order status updated successfully",
                orderId = id,
                newStatus = request.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status {OrderId}", id);
            return StatusCode(500, new { message = "Error updating order status", error = ex.Message });
        }
    }
}

public class CreateOrderRequest
{
    public int UserId { get; set; }
    public List<CreateOrderItemRequest>? Items { get; set; }
}

public class CreateOrderItemRequest
{
    public int NoodleId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
