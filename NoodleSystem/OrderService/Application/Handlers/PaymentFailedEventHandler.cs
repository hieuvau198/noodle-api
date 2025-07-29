using MassTransit;
using OrderService.Application.Events;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Handlers;

public class PaymentFailedEventHandler : IConsumer<PaymentFailedEvent>
{
    private readonly ILogger<PaymentFailedEventHandler> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public PaymentFailedEventHandler(
        ILogger<PaymentFailedEventHandler> logger,
        IOrderRepository orderRepository,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _orderRepository = orderRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var paymentEvent = context.Message;
        
        _logger.LogInformation(
            "Processing PaymentFailed event for Order {OrderId}, User {UserId}, Reason: {FailureReason}",
            paymentEvent.OrderId,
            paymentEvent.UserId,
            paymentEvent.FailureReason);

        try
        {
            await UpdateOrderStatusToFailedAsync(paymentEvent);
            await HandleRetryLogicAsync(paymentEvent);
            await TrackPaymentFailureAnalyticsAsync(paymentEvent);
            await CreatePaymentFailureAuditTrailAsync(paymentEvent);

            _logger.LogInformation("Successfully processed PaymentFailed event for Order {OrderId}", paymentEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentFailed event for Order {OrderId}", paymentEvent.OrderId);
            throw;
        }
    }

    private async Task UpdateOrderStatusToFailedAsync(PaymentFailedEvent paymentEvent)
    {
        _logger.LogInformation("Updating order status to PaymentFailed for Order {OrderId}", paymentEvent.OrderId);

        try
        {
            var order = await _orderRepository.GetByIdAsync(paymentEvent.OrderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order {paymentEvent.OrderId} not found");
            }

            var newStatus = paymentEvent.IsRetryable ? "PaymentFailedRetryable" : "Cancelled";
            
            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("Order status updated to {Status} for Order {OrderId}", newStatus, paymentEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order status for Order {OrderId}", paymentEvent.OrderId);
            throw;
        }
    }


    private async Task HandleRetryLogicAsync(PaymentFailedEvent paymentEvent)
    {
        if (!paymentEvent.IsRetryable)
        {
            _logger.LogInformation("Payment failure is not retryable for Order {OrderId}", paymentEvent.OrderId);
            return;
        }

        _logger.LogInformation("Handling retry logic for Order {OrderId}", paymentEvent.OrderId);

        try
        {

            _logger.LogInformation("Payment failure is retryable for Order {OrderId} - customer can attempt payment again", 
                paymentEvent.OrderId);


            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle retry logic for Order {OrderId}", paymentEvent.OrderId);
        }
    }

    private async Task TrackPaymentFailureAnalyticsAsync(PaymentFailedEvent paymentEvent)
    {
        _logger.LogInformation("Tracking payment failure analytics for Order {OrderId}", paymentEvent.OrderId);

        try
        {
            _logger.LogInformation(
                "Payment Failure Analytics - OrderId: {OrderId}, UserId: {UserId}, AttemptedAmount: {AttemptedAmount}, Currency: {Currency}, PaymentMethod: {PaymentMethod}, FailureReason: {FailureReason}, ErrorCode: {ErrorCode}, IsRetryable: {IsRetryable}, FailedAt: {FailedAt}",
                paymentEvent.OrderId,
                paymentEvent.UserId,
                paymentEvent.AttemptedAmount,
                paymentEvent.Currency,
                paymentEvent.PaymentMethod,
                paymentEvent.FailureReason,
                paymentEvent.ErrorCode,
                paymentEvent.IsRetryable,
                paymentEvent.FailedAt);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track payment failure analytics for Order {OrderId}", paymentEvent.OrderId);
        }
    }

    private async Task CreatePaymentFailureAuditTrailAsync(PaymentFailedEvent paymentEvent)
    {
        _logger.LogInformation("Creating payment failure audit trail for Order {OrderId}", paymentEvent.OrderId);

        try
        {
            _logger.LogInformation(
                "AUDIT: Payment failed for Order {OrderId} by User {UserId} at {FailedAt} - Amount: {AttemptedAmount} {Currency}, Method: {PaymentMethod}, Reason: {FailureReason}, Code: {ErrorCode}, Retryable: {IsRetryable}",
                paymentEvent.OrderId,
                paymentEvent.UserId,
                paymentEvent.FailedAt,
                paymentEvent.AttemptedAmount,
                paymentEvent.Currency,
                paymentEvent.PaymentMethod,
                paymentEvent.FailureReason,
                paymentEvent.ErrorCode,
                paymentEvent.IsRetryable);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payment failure audit trail for Order {OrderId}", paymentEvent.OrderId);
        }
    }

}