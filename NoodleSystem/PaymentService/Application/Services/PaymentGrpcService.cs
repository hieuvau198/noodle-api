using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;
using PaymentService.Grpc;

namespace PaymentService.Application.Services;

public class PaymentGrpcService : Grpc.PaymentService.PaymentServiceBase
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<PaymentGrpcService> _logger;

    public PaymentGrpcService(PaymentDbContext context, ILogger<PaymentGrpcService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task<PaymentRequestResult> RequestPayment(PaymentRequestDto request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Processing payment request for order {OrderId}, amount {Amount} via gRPC", 
                request.OrderId, request.Amount);

            Console.WriteLine("=== PAYMENT REQUEST (gRPC) ===");
            Console.WriteLine($"Order ID: {request.OrderId}");
            Console.WriteLine($"User ID: {request.UserId}");
            Console.WriteLine($"Amount: {request.Amount:C} {request.Currency}");
            Console.WriteLine($"Requested At: {request.RequestedAt}");
            Console.WriteLine("===============================");
            
            await Task.Delay(100);
            var paymentId = $"pay_{Guid.NewGuid():N}";
            var paymentUrl = $"https://payment.example.com/pay/{request.OrderId}";
            
            // Create payment record in database
            var payment = new Payment
            {
                OrderId = request.OrderId,
                Amount = (decimal)request.Amount,
                Status = "Pending",
                PaymentMethod = "Credit Card",
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"Payment request created: {paymentId}");
            Console.WriteLine($"Payment URL: {paymentUrl}");
            
            return new PaymentRequestResult
            {
                Success = true,
                PaymentId = paymentId,
                PaymentUrl = paymentUrl,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15).ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment request for order {OrderId} via gRPC", request.OrderId);
            return new PaymentRequestResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<PaymentStatusResponse> GetPaymentStatus(GetPaymentStatusRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Getting payment status for order {OrderId} via gRPC", request.OrderId);

            var payment = await _context.Payments
                .Where(p => p.OrderId == request.OrderId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                return new PaymentStatusResponse
                {
                    Status = "NotFound",
                    FailureReason = "Payment not found for this order"
                };
            }

            Console.WriteLine("=== PAYMENT STATUS CHECK (gRPC) ===");
            Console.WriteLine($"Order ID: {request.OrderId}");
            Console.WriteLine($"Payment Status: {payment.Status}");
            Console.WriteLine("====================================");

            var response = new PaymentStatusResponse
            {
                Status = payment.Status
            };

            if (payment.Status == "Completed" && payment.PaidAt.HasValue)
            {
                response.TransactionId = payment.TransactionId ?? "";
                response.AmountPaid = (double)payment.Amount;
                response.PaidAt = payment.PaidAt.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
            else if (payment.Status == "Failed")
            {
                response.FailureReason = "Payment processing failed";
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for order {OrderId} via gRPC", request.OrderId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<CancelPaymentResponse> CancelPayment(CancelPaymentRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Cancelling payment for order {OrderId}, reason: {Reason} via gRPC", 
                request.OrderId, request.Reason);

            var payment = await _context.Payments
                .Where(p => p.OrderId == request.OrderId && p.Status == "Pending")
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                return new CancelPaymentResponse
                {
                    Success = false,
                    Message = "Payment not found or cannot be cancelled"
                };
            }

            payment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            Console.WriteLine("=== PAYMENT CANCELLATION (gRPC) ===");
            Console.WriteLine($"Order ID: {request.OrderId}");
            Console.WriteLine($"Cancellation Reason: {request.Reason}");
            Console.WriteLine($"Cancelled At: {DateTime.UtcNow}");
            Console.WriteLine("====================================");

            return new CancelPaymentResponse
            {
                Success = true,
                Message = "Payment cancelled successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment for order {OrderId} via gRPC", request.OrderId);
            return new CancelPaymentResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public override async Task<RefundPaymentResponse> RefundPayment(RefundPaymentRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Processing refund for order {OrderId}, amount {Amount}, reason: {Reason} via gRPC", 
                request.OrderId, request.Amount, request.Reason);

            var payment = await _context.Payments
                .Where(p => p.OrderId == request.OrderId && p.Status == "Completed")
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                return new RefundPaymentResponse
                {
                    Success = false,
                    Message = "Payment not found or cannot be refunded"
                };
            }

            if ((decimal)request.Amount > payment.Amount)
            {
                return new RefundPaymentResponse
                {
                    Success = false,
                    Message = "Refund amount cannot exceed payment amount"
                };
            }

            var refundId = $"rfnd_{Guid.NewGuid():N}";
            payment.Status = "Refunded";
            await _context.SaveChangesAsync();

            Console.WriteLine("=== PAYMENT REFUND (gRPC) ===");
            Console.WriteLine($"Order ID: {request.OrderId}");
            Console.WriteLine($"Refund Amount: {request.Amount:C}");
            Console.WriteLine($"Refund Reason: {request.Reason}");
            Console.WriteLine($"Processed At: {DateTime.UtcNow}");
            Console.WriteLine($"Refund ID: {refundId}");
            Console.WriteLine("==============================");

            return new RefundPaymentResponse
            {
                Success = true,
                RefundId = refundId,
                Message = "Refund processed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for order {OrderId} via gRPC", request.OrderId);
            return new RefundPaymentResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}