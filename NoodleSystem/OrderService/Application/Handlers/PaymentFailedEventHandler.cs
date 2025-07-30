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



}