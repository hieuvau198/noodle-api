using MassTransit;
using OrderService.Application.Events;
using OrderService.Application.Services;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Handlers;

public class PaymentRequestedEventHandler : IConsumer<PaymentRequestedEvent>
{
    private readonly ILogger<PaymentRequestedEventHandler> _logger;
    private readonly IPaymentServiceClient _paymentServiceClient;
    private readonly IOrderRepository _orderRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public PaymentRequestedEventHandler(
        ILogger<PaymentRequestedEventHandler> logger,
        IPaymentServiceClient paymentServiceClient,
        IOrderRepository orderRepository,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _paymentServiceClient = paymentServiceClient;
        _orderRepository = orderRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<PaymentRequestedEvent> context)
    {
        var paymentEvent = context.Message;
        
        _logger.LogInformation(
            "Processing PaymentRequested event for Order {OrderId}, User {UserId}, Amount {Amount}",
            paymentEvent.OrderId,
            paymentEvent.UserId,
            paymentEvent.Amount);

        try
        {
            // 1. Update order status to "AwaitingPayment"
            await UpdateOrderStatusAsync(paymentEvent);

            // 2. Perform fraud detection checks
            await PerformFraudDetectionAsync(paymentEvent);

            // 3. Forward payment request to payment service
            await ForwardPaymentRequestAsync(paymentEvent);

            // 4. Set up payment timeout
            await SetupPaymentTimeoutAsync(paymentEvent);

            _logger.LogInformation("Successfully processed PaymentRequested event for Order {OrderId}", paymentEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentRequested event for Order {OrderId}", paymentEvent.OrderId);
            
            // Update order status to indicate payment processing failure
            await UpdateOrderStatusToFailedAsync(paymentEvent.OrderId, ex.Message);
            
            throw; // Re-throw to let MassTransit handle retry logic
        }
    }

    private async Task UpdateOrderStatusAsync(PaymentRequestedEvent paymentEvent)
    {
        _logger.LogInformation("Updating order status to AwaitingPayment for Order {OrderId}", paymentEvent.OrderId);

        try
        {
            var order = await _orderRepository.GetByIdAsync(paymentEvent.OrderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order {paymentEvent.OrderId} not found");
            }

            order.Status = "AwaitingPayment";
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("Order status updated to AwaitingPayment for Order {OrderId}", paymentEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order status for Order {OrderId}", paymentEvent.OrderId);
            throw;
        }
    }

    private async Task PerformFraudDetectionAsync(PaymentRequestedEvent paymentEvent)
    {
        _logger.LogInformation("Performing fraud detection for Order {OrderId}", paymentEvent.OrderId);

        try
        {
            // Basic fraud detection checks
            if (paymentEvent.Amount <= 0)
            {
                throw new InvalidOperationException($"Invalid payment amount: {paymentEvent.Amount}");
            }

            if (paymentEvent.Amount > 1000) // Example: flag orders over $1000 VND (very high)
            {
                _logger.LogWarning("High-value order detected: Order {OrderId}, Amount {Amount}", 
                    paymentEvent.OrderId, paymentEvent.Amount);
                
                // In a real system, you might:
                // - Require additional verification
                // - Send to manual review queue
                // - Apply stricter validation
            }

            // Check for suspicious patterns (mock implementation)
            if (paymentEvent.UserId <= 0)
            {
                throw new InvalidOperationException($"Invalid user ID: {paymentEvent.UserId}");
            }

            _logger.LogInformation("Fraud detection passed for Order {OrderId}", paymentEvent.OrderId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fraud detection failed for Order {OrderId}", paymentEvent.OrderId);
            throw;
        }
    }

    private async Task ForwardPaymentRequestAsync(PaymentRequestedEvent paymentEvent)
    {
        _logger.LogInformation("Forwarding payment request to payment service for Order {OrderId}", paymentEvent.OrderId);

        try
        {
            var paymentRequest = new PaymentRequestDto
            {
                OrderId = paymentEvent.OrderId,
                UserId = paymentEvent.UserId,
                Amount = paymentEvent.Amount,
                Currency = paymentEvent.Currency,
                RequestedAt = paymentEvent.RequestedAt
            };

            var result = await _paymentServiceClient.RequestPaymentAsync(paymentRequest);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Payment service request failed: {result.ErrorMessage}");
            }

            _logger.LogInformation("Payment request forwarded successfully for Order {OrderId}, PaymentId: {PaymentId}", 
                paymentEvent.OrderId, result.PaymentId);

            // Store payment details in metadata for future reference
            // In a real system, you might store this in a separate payment tracking table
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to forward payment request for Order {OrderId}", paymentEvent.OrderId);
            throw;
        }
    }

    private async Task SetupPaymentTimeoutAsync(PaymentRequestedEvent paymentEvent)
    {
        _logger.LogInformation("Setting up payment timeout for Order {OrderId}", paymentEvent.OrderId);

        try
        {
            // Academic implementation: Console logging instead of background job scheduling
            var timeoutDuration = TimeSpan.FromMinutes(15); // 15-minute payment timeout
            var timeoutAt = paymentEvent.RequestedAt.Add(timeoutDuration);

            Console.WriteLine("=== PAYMENT TIMEOUT SETUP ===");
            Console.WriteLine($"Order ID: {paymentEvent.OrderId}");
            Console.WriteLine($"Payment Requested At: {paymentEvent.RequestedAt}");
            Console.WriteLine($"Timeout Duration: {timeoutDuration.TotalMinutes} minutes");
            Console.WriteLine($"Timeout Scheduled At: {timeoutAt}");
            Console.WriteLine("Background job would be created here in production");
            Console.WriteLine("==============================");

            _logger.LogInformation("Payment timeout scheduled for Order {OrderId} at {TimeoutAt}", 
                paymentEvent.OrderId, timeoutAt);

            // Simulate background job setup completion
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup payment timeout for Order {OrderId}", paymentEvent.OrderId);
            // Don't throw - timeout setup failure shouldn't fail the entire process
        }
    }


    private async Task UpdateOrderStatusToFailedAsync(int orderId, string errorMessage)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order != null)
            {
                order.Status = "PaymentProcessingFailed";
                order.UpdatedAt = DateTime.UtcNow;
                await _orderRepository.UpdateAsync(order);

                _logger.LogInformation("Order status updated to PaymentProcessingFailed for Order {OrderId}", orderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order status to failed for Order {OrderId}", orderId);
        }
    }

    // This method would be called by a background job when payment times out
    public async Task HandlePaymentTimeout(int orderId)
    {
        _logger.LogInformation("Handling payment timeout for Order {OrderId}", orderId);

        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order != null && order.Status == "AwaitingPayment")
            {
                order.Status = "PaymentTimeout";
                order.UpdatedAt = DateTime.UtcNow;
                await _orderRepository.UpdateAsync(order);

                // Cancel payment request
                await _paymentServiceClient.CancelPaymentAsync(orderId, "Payment timeout");

                _logger.LogInformation("Payment timeout notification would be sent in production for Order {OrderId}", orderId);

                _logger.LogInformation("Payment timeout handled for Order {OrderId}", orderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment timeout for Order {OrderId}", orderId);
        }
    }
}