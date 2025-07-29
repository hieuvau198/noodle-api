using MassTransit;
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
            var orderExists = await _orderGrpcClient.ValidateOrderExistsAsync(paymentRequest.OrderId);
            if (!orderExists)
            {
                await PublishPaymentFailedAsync(paymentRequest, "Order not found", "ORDER_NOT_FOUND");
                return;
            }

            var orderDetails = await _orderGrpcClient.GetOrderDetailsAsync(paymentRequest.OrderId);
            if (orderDetails == null)
            {
                await PublishPaymentFailedAsync(paymentRequest, "Could not retrieve order details", "ORDER_DETAILS_ERROR");
                return;
            }

            if (Math.Abs((decimal)orderDetails.TotalAmount - paymentRequest.Amount) > 0.01m)
            {
                await PublishPaymentFailedAsync(paymentRequest, "Amount mismatch", "AMOUNT_MISMATCH");
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

                await PublishPaymentFailedAsync(paymentRequest, "Payment gateway declined", "PAYMENT_DECLINED");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment request for order {OrderId}", paymentRequest.OrderId);
            await PublishPaymentFailedAsync(paymentRequest, ex.Message, "PROCESSING_ERROR");
        }
    }

    private async Task PublishPaymentFailedAsync(PaymentRequestedEvent request, string reason, string errorCode)
    {
        await _publishEndpoint.Publish(new PaymentFailedEvent
        {
            OrderId = request.OrderId,
            UserId = request.UserId,
            Amount = request.Amount,
            Reason = reason,
            ErrorCode = errorCode,
            FailedAt = DateTime.UtcNow
        });

        _logger.LogWarning("Payment failed for order {OrderId}: {Reason}", request.OrderId, reason);
    }
}