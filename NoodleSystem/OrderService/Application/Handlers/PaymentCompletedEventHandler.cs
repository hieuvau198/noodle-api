using MassTransit;
using OrderService.Application.Events;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Handlers;

public class PaymentCompletedEventHandler : IConsumer<PaymentCompletedEvent>
{
    private readonly ILogger<PaymentCompletedEventHandler> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public PaymentCompletedEventHandler(
        ILogger<PaymentCompletedEventHandler> logger,
        IOrderRepository orderRepository,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _orderRepository = orderRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var paymentEvent = context.Message;
        
        _logger.LogInformation(
            "Processing PaymentCompleted event for Order {OrderId}, User {UserId}, Amount {Amount}",
            paymentEvent.OrderId,
            paymentEvent.UserId,
            paymentEvent.AmountPaid);

        try
        {
            await UpdateOrderStatusToInPreparationAsync(paymentEvent);

            await TrackPaymentAnalyticsAsync(paymentEvent);

            await CreatePaymentAuditTrailAsync(paymentEvent);

            _logger.LogInformation("Successfully processed PaymentCompleted event for Order {OrderId}", paymentEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentCompleted event for Order {OrderId}", paymentEvent.OrderId);
            
            await HandlePaymentProcessingFailureAsync(paymentEvent, ex.Message);

            throw;
        }
    }

    private async Task UpdateOrderStatusToInPreparationAsync(PaymentCompletedEvent paymentEvent)
    {
        _logger.LogInformation("Updating order status to InPreparation for Order {OrderId}", paymentEvent.OrderId);

        try
        {
            var order = await _orderRepository.GetByIdAsync(paymentEvent.OrderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order {paymentEvent.OrderId} not found");
            }

            order.Status = "InPreparation";
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("Order status updated to InPreparation for Order {OrderId}", paymentEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order status to InPreparation for Order {OrderId}", paymentEvent.OrderId);
            throw;
        }
    }

    private async Task TrackPaymentAnalyticsAsync(PaymentCompletedEvent paymentEvent)
    {
        _logger.LogInformation("Tracking payment analytics for Order {OrderId}", paymentEvent.OrderId);

        try
        {
            _logger.LogInformation(
                "Payment Analytics - OrderId: {OrderId}, UserId: {UserId}, AmountPaid: {AmountPaid}, Currency: {Currency}, PaymentMethod: {PaymentMethod}, TransactionId: {TransactionId}, PaidAt: {PaidAt}",
                paymentEvent.OrderId,
                paymentEvent.UserId,
                paymentEvent.AmountPaid,
                paymentEvent.Currency,
                paymentEvent.PaymentMethod,
                paymentEvent.TransactionId,
                paymentEvent.PaidAt);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track payment analytics for Order {OrderId}", paymentEvent.OrderId);
        }
    }

    private async Task CreatePaymentAuditTrailAsync(PaymentCompletedEvent paymentEvent)
    {
        _logger.LogInformation("Creating payment audit trail for Order {OrderId}", paymentEvent.OrderId);

        try
        {
            _logger.LogInformation(
                "AUDIT: Payment completed for Order {OrderId} by User {UserId} at {PaidAt} - Amount: {AmountPaid} {Currency}, Method: {PaymentMethod}, Transaction: {TransactionId}",
                paymentEvent.OrderId,
                paymentEvent.UserId,
                paymentEvent.PaidAt,
                paymentEvent.AmountPaid,
                paymentEvent.Currency,
                paymentEvent.PaymentMethod,
                paymentEvent.TransactionId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payment audit trail for Order {OrderId}", paymentEvent.OrderId);
        }
    }

    private async Task HandlePaymentProcessingFailureAsync(PaymentCompletedEvent paymentEvent, string errorMessage)
    {
        try
        {
            _logger.LogWarning("Payment processing failed for Order {OrderId}, initiating failure handling", paymentEvent.OrderId);

            var order = await _orderRepository.GetByIdAsync(paymentEvent.OrderId);
            if (order != null)
            {
                order.Status = "PaymentProcessingError";
                order.UpdatedAt = DateTime.UtcNow;
                await _orderRepository.UpdateAsync(order);
            }

            _logger.LogInformation("Payment processing failure handled for Order {OrderId}", paymentEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle payment processing failure for Order {OrderId}", paymentEvent.OrderId);
        }
    }
}