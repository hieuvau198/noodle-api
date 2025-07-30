using MassTransit;
using OrderService.Grpc;
using PaymentService.Application.Events;
using PaymentService.Application.Services;
using PaymentService.Domain;

namespace PaymentService.Application.Handlers;

public class PaymentRequestedEventHandler : IConsumer<PaymentRequestedEvent>
{
    private readonly ILogger<PaymentRequestedEventHandler> _logger;
    private readonly PaymentDbContext _context;
    private readonly IOrderGrpcClient _orderGrpcClient;
    private readonly IPublishEndpoint _publishEndpoint;

    public PaymentRequestedEventHandler(
        ILogger<PaymentRequestedEventHandler> logger,
        PaymentDbContext context,
        IOrderGrpcClient orderGrpcClient,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _context = context;
        _orderGrpcClient = orderGrpcClient;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<PaymentRequestedEvent> context)
    {
        var paymentRequest = context.Message;

        _logger.LogInformation(
            "Processing payment request for Order {OrderId}, User {UserId}, Amount {Amount}",
            paymentRequest.OrderId,
            paymentRequest.UserId,
            paymentRequest.Amount);

        try
        {
            bool orderExists = false;
            OrderDetails? orderDetails = null;
            int retryCount = 0;
            const int maxRetries = 3;
            const int retryDelayMs = 200;

            while (retryCount < maxRetries && !orderExists)
            {
                try
                {
                    orderExists = await _orderGrpcClient.ValidateOrderExistsAsync(paymentRequest.OrderId);
                    if (orderExists)
                    {
                        orderDetails = await _orderGrpcClient.GetOrderDetailsAsync(paymentRequest.OrderId);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Attempt {RetryCount} failed to validate order {OrderId}", retryCount + 1, paymentRequest.OrderId);
                }

                retryCount++;
                if (retryCount < maxRetries)
                {
                    await Task.Delay(retryDelayMs * retryCount); 
                }
            }

            if (!orderExists || orderDetails == null || Math.Abs((decimal)orderDetails.TotalAmount - paymentRequest.Amount) > 0.01m)
            {
                // Payment failed, just log and return (no event publishing)
                _logger.LogWarning("Payment failed for order {OrderId}: validation failed or amount mismatch", paymentRequest.OrderId);
                return;
            }

            var payment = new Payment
            {
                OrderId = paymentRequest.OrderId,
                Amount = paymentRequest.Amount,
                Status = "Processing",
                PaymentMethod = "Mock Payment",
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            await Task.Delay(1000);

            var random = new Random();
            var isSuccess = random.NextDouble() > 0.1;

            if (isSuccess)
            {
                payment.Status = "Completed";
                payment.TransactionId = $"txn_{Guid.NewGuid():N}";
                payment.PaidAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _publishEndpoint.Publish(new PaymentCompletedEvent
                {
                    OrderId = paymentRequest.OrderId,
                    UserId = paymentRequest.UserId,
                    Amount = paymentRequest.Amount,
                    PaymentId = payment.PaymentId.ToString(),
                    TransactionId = payment.TransactionId,
                    CompletedAt = payment.PaidAt.Value
                });

                _logger.LogInformation("Payment completed successfully for order {OrderId}", paymentRequest.OrderId);
            }
            else
            {
                payment.Status = "Failed";
                await _context.SaveChangesAsync();
                _logger.LogWarning("Payment failed for order {OrderId}: Payment gateway declined", paymentRequest.OrderId);
                // No PaymentFailedEvent published
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment request for order {OrderId}", paymentRequest.OrderId);
            // No PaymentFailedEvent published
        }
    }
}