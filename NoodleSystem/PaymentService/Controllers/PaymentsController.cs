using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;
using PaymentService.Application.Services;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<PaymentsController> _logger;
    private readonly IOrderGrpcClient _orderGrpcClient;

    public PaymentsController(
        PaymentDbContext context, 
        ILogger<PaymentsController> logger,
        IOrderGrpcClient orderGrpcClient)
    {
        _context = context;
        _logger = logger;
        _orderGrpcClient = orderGrpcClient;
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var paymentCount = await _context.Payments.CountAsync();
            return Ok(new { 
                message = "Payment database connection successful!", 
                paymentCount = paymentCount,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment database connection test failed");
            return StatusCode(500, new { 
                message = "Payment database connection failed", 
                error = ex.Message,
                timestamp = DateTime.UtcNow 
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
    {
        try
        {
            var payments = await _context.Payments
                .Select(p => new
                {
                    p.PaymentId,
                    p.OrderId,
                    p.Amount,
                    p.Status,
                    p.PaymentMethod,
                    p.TransactionId,
                    p.PaidAt,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments");
            return StatusCode(500, new { message = "Error retrieving payments", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Payment>> GetPayment(int id)
    {
        try
        {
            var payment = await _context.Payments
                .Where(p => p.PaymentId == id)
                .Select(p => new
                {
                    p.PaymentId,
                    p.OrderId,
                    p.Amount,
                    p.Status,
                    p.PaymentMethod,
                    p.TransactionId,
                    p.PaidAt,
                    p.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            return Ok(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment {PaymentId}", id);
            return StatusCode(500, new { message = "Error retrieving payment", error = ex.Message });
        }
    }

    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<IEnumerable<Payment>>> GetPaymentsByOrder(int orderId)
    {
        try
        {
            var payments = await _context.Payments
                .Where(p => p.OrderId == orderId)
                .Select(p => new
                {
                    p.PaymentId,
                    p.OrderId,
                    p.Amount,
                    p.Status,
                    p.PaymentMethod,
                    p.TransactionId,
                    p.PaidAt,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for order {OrderId}", orderId);
            return StatusCode(500, new { message = "Error retrieving payments for order", error = ex.Message });
        }
    }

    [HttpPost("confirm")]
    public async Task<ActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
    {
        try
        {
            var payment = await _context.Payments.FindAsync(request.PaymentId);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            if (request.Status != "NotPaid" && request.Status != "Completed")
            {
                return BadRequest(new { message = "Invalid status. Must be 'NotPaid' or 'Completed'" });
            }

            payment.Status = request.Status;
            if (request.Status == "Completed" && !payment.PaidAt.HasValue)
            {
                payment.PaidAt = DateTime.UtcNow;
            }
            else if (request.Status == "NotPaid")
            {
                payment.PaidAt = null;
            }

            await _context.SaveChangesAsync();

            return Ok(new {
                message = "Payment confirmation updated successfully",
                paymentId = payment.PaymentId,
                newStatus = payment.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment {PaymentId}", request.PaymentId);
            return StatusCode(500, new { message = "Error confirming payment", error = ex.Message });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusRequest request)
    {
        try
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            payment.Status = request.Status;
            if (request.Status == "Completed" && !payment.PaidAt.HasValue)
            {
                payment.PaidAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Payment status updated successfully",
                paymentId = payment.PaymentId,
                newStatus = payment.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status {PaymentId}", id);
            return StatusCode(500, new { message = "Error updating payment status", error = ex.Message });
        }
    }
}

public class ConfirmPaymentRequest
{
    public int PaymentId { get; set; }
    public string Status { get; set; } = string.Empty; 
}

public class UpdatePaymentStatusRequest
{
    public string Status { get; set; } = string.Empty;
} 