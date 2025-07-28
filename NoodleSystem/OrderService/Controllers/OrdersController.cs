using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _context;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrderDbContext context, ILogger<OrdersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var orderCount = await _context.Orders.CountAsync();
            var noodleCount = await _context.SpicyNoodles.CountAsync();
            return Ok(new { 
                message = "Order database connection successful!", 
                orderCount = orderCount,
                noodleCount = noodleCount,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order database connection test failed");
            return StatusCode(500, new { 
                message = "Order database connection failed", 
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
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Select(o => new
                {
                    o.OrderId,
                    o.UserId,
                    o.Status,
                    o.TotalAmount,
                    o.CreatedAt,
                    o.UpdatedAt,
                    ItemCount = o.OrderItems.Count
                })
                .ToListAsync();

            return Ok(orders);
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
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Noodle)
                .Where(o => o.OrderId == id)
                .Select(o => new
                {
                    o.OrderId,
                    o.UserId,
                    o.Status,
                    o.TotalAmount,
                    o.CreatedAt,
                    o.UpdatedAt,
                    OrderItems = o.OrderItems.Select(oi => new
                    {
                        oi.OrderItemId,
                        oi.NoodleId,
                        oi.Quantity,
                        oi.Subtotal,
                        oi.CreatedAt,
                        Noodle = new
                        {
                            oi.Noodle.NoodleId,
                            oi.Noodle.Name,
                            oi.Noodle.BasePrice,
                            oi.Noodle.Description
                        }
                    })
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            return Ok(order);
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
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .Select(o => new
                {
                    o.OrderId,
                    o.UserId,
                    o.Status,
                    o.TotalAmount,
                    o.CreatedAt,
                    o.UpdatedAt,
                    ItemCount = o.OrderItems.Count
                })
                .ToListAsync();

            return Ok(orders);
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
            var noodles = await _context.SpicyNoodles
                .Where(n => n.IsActive)
                .Select(n => new
                {
                    n.NoodleId,
                    n.Name,
                    n.BasePrice,
                    n.ImageUrl,
                    n.Description,
                    n.CreatedAt
                })
                .ToListAsync();

            return Ok(noodles);
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
            var order = new Order
            {
                UserId = request.UserId,
                Status = "Pending",
                TotalAmount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            if (request.Items != null && request.Items.Any())
            {
                foreach (var item in request.Items)
                {
                    var noodle = await _context.SpicyNoodles.FindAsync(item.NoodleId);
                    if (noodle == null)
                    {
                        return BadRequest(new { message = $"Noodle with ID {item.NoodleId} not found" });
                    }

                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        NoodleId = item.NoodleId,
                        Quantity = item.Quantity,
                        Subtotal = noodle.BasePrice * item.Quantity,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.OrderItems.Add(orderItem);
                }

                await _context.SaveChangesAsync();

                order.TotalAmount = await _context.OrderItems
                    .Where(oi => oi.OrderId == order.OrderId)
                    .SumAsync(oi => oi.Subtotal);
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, new
            {
                order.OrderId,
                order.UserId,
                order.Status,
                order.TotalAmount,
                order.CreatedAt,
                order.UpdatedAt
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
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Order status updated successfully",
                orderId = order.OrderId,
                newStatus = order.Status
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
