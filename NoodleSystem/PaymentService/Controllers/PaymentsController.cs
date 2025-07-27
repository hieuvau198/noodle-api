using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(PaymentDbContext context, ILogger<PaymentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/payments/test
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

    // GET: api/payments
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

    // GET: api/payments/{id}
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

    // GET: api/payments/order/{orderId}
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

    // POST: api/payments
    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        try
        {
            var payment = new Payment
            {
                OrderId = request.OrderId,
                Amount = request.Amount,
                Status = request.Status ?? "Pending",
                PaymentMethod = request.PaymentMethod,
                TransactionId = request.TransactionId,
                PaidAt = request.PaidAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPayment), new { id = payment.PaymentId }, new
            {
                payment.PaymentId,
                payment.OrderId,
                payment.Amount,
                payment.Status,
                payment.PaymentMethod,
                payment.TransactionId,
                payment.PaidAt,
                payment.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            return StatusCode(500, new { message = "Error creating payment", error = ex.Message });
        }
    }

    // PUT: api/payments/{id}/status
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

public class CreatePaymentRequest
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string? Status { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class UpdatePaymentStatusRequest
{
    public string Status { get; set; } = string.Empty;
} 