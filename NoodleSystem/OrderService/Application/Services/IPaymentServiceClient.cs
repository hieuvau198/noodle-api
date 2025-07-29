using OrderService.Application.Events;

namespace OrderService.Application.Services;

public interface IPaymentServiceClient
{
    Task<PaymentRequestResult> RequestPaymentAsync(PaymentRequestDto request);
    Task<PaymentStatus> GetPaymentStatusAsync(int orderId);
    Task<bool> CancelPaymentAsync(int orderId, string reason);
    Task<bool> RefundPaymentAsync(int orderId, decimal amount, string reason);
}

public record PaymentRequestDto
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "VND";
    public Dictionary<string, object> Metadata { get; init; } = new();
    public DateTime RequestedAt { get; init; }
}

public record PaymentRequestResult
{
    public bool Success { get; init; }
    public string? PaymentId { get; init; }
    public string? PaymentUrl { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime ExpiresAt { get; init; }
}

public record PaymentStatus
{
    public string Status { get; init; } = string.Empty; // Pending, Completed, Failed, Cancelled
    public string? TransactionId { get; init; }
    public decimal? AmountPaid { get; init; }
    public DateTime? PaidAt { get; init; }
    public string? FailureReason { get; init; }
}

public class PaymentServiceClient : IPaymentServiceClient
{
    private readonly ILogger<PaymentServiceClient> _logger;
    private readonly HttpClient _httpClient;

    public PaymentServiceClient(ILogger<PaymentServiceClient> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<PaymentRequestResult> RequestPaymentAsync(PaymentRequestDto request)
    {
        _logger.LogInformation("Requesting payment for order {OrderId}, amount {Amount}", 
            request.OrderId, request.Amount);

        try
        {
            Console.WriteLine("=== PAYMENT REQUEST ===");
            Console.WriteLine($"Order ID: {request.OrderId}");
            Console.WriteLine($"User ID: {request.UserId}");
            Console.WriteLine($"Amount: {request.Amount:C} {request.Currency}");
            Console.WriteLine($"Requested At: {request.RequestedAt}");
            Console.WriteLine("========================");
            
            await Task.Delay(100);
            var paymentId = $"pay_{Guid.NewGuid():N}";
            var paymentUrl = $"https://payment.example.com/pay/{request.OrderId}";
            
            Console.WriteLine($"Payment request created: {paymentId}");
            Console.WriteLine($"Payment URL: {paymentUrl}");
            
            return new PaymentRequestResult
            {
                Success = true,
                PaymentId = paymentId,
                PaymentUrl = paymentUrl,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting payment for order {OrderId}", request.OrderId);
            return new PaymentRequestResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaymentStatus> GetPaymentStatusAsync(int orderId)
    {
        _logger.LogInformation("Getting payment status for order {OrderId}", orderId);

        try
        {
            // Academic implementation: Console logging and mock status checking
            Console.WriteLine("=== PAYMENT STATUS CHECK ===");
            Console.WriteLine($"Order ID: {orderId}");
            
            await Task.Delay(50);
            
            // Simulate random payment status for demonstration
            var random = new Random();
            var statusOptions = new[] { "Pending", "Completed", "Failed" };
            var status = statusOptions[random.Next(statusOptions.Length)];
            
            Console.WriteLine($"Payment Status: {status}");
            
            var result = new PaymentStatus
            {
                Status = status
            };
            
            // Add additional details based on status
            switch (status)
            {
                case "Completed":
                    result = result with 
                    { 
                        TransactionId = $"txn_{Guid.NewGuid():N}",
                        AmountPaid = 150000m, // Mock amount
                        PaidAt = DateTime.UtcNow.AddMinutes(-5)
                    };
                    Console.WriteLine($"Transaction ID: {result.TransactionId}");
                    Console.WriteLine($"Amount Paid: {result.AmountPaid:C}");
                    Console.WriteLine($"Paid At: {result.PaidAt}");
                    break;
                case "Failed":
                    result = result with { FailureReason = "Insufficient funds" };
                    Console.WriteLine($"Failure Reason: {result.FailureReason}");
                    break;
                case "Pending":
                    Console.WriteLine("Payment is still being processed");
                    break;
            }
            
            Console.WriteLine("=============================");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for order {OrderId}", orderId);
            return new PaymentStatus
            {
                Status = "Error",
                FailureReason = ex.Message
            };
        }
    }

    public async Task<bool> CancelPaymentAsync(int orderId, string reason)
    {
        _logger.LogInformation("Cancelling payment for order {OrderId}, reason: {Reason}", orderId, reason);

        try
        {
            // Academic implementation: Console logging instead of HTTP call
            Console.WriteLine("=== PAYMENT CANCELLATION ===");
            Console.WriteLine($"Order ID: {orderId}");
            Console.WriteLine($"Cancellation Reason: {reason}");
            Console.WriteLine($"Cancelled At: {DateTime.UtcNow}");
            Console.WriteLine("Payment has been successfully cancelled.");
            Console.WriteLine("==============================");
            
            await Task.Delay(50);
            return true; // Always succeed in academic implementation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment for order {OrderId}", orderId);
            Console.WriteLine($"Failed to cancel payment for order {orderId}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RefundPaymentAsync(int orderId, decimal amount, string reason)
    {
        _logger.LogInformation("Requesting refund for order {OrderId}, amount {Amount}, reason: {Reason}", 
            orderId, amount, reason);

        try
        {
            // Academic implementation: Console logging instead of HTTP call
            Console.WriteLine("=== PAYMENT REFUND ===");
            Console.WriteLine($"Order ID: {orderId}");
            Console.WriteLine($"Refund Amount: {amount:C}");
            Console.WriteLine($"Refund Reason: {reason}");
            Console.WriteLine($"Processed At: {DateTime.UtcNow}");
            Console.WriteLine($"Refund ID: rfnd_{Guid.NewGuid():N}");
            Console.WriteLine("Refund has been successfully processed.");
            Console.WriteLine("=======================");
            
            await Task.Delay(100);
            return true; // Always succeed in academic implementation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for order {OrderId}", orderId);
            Console.WriteLine($"Failed to process refund for order {orderId}: {ex.Message}");
            return false;
        }
    }
}