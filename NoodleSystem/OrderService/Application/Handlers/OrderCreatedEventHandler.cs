using MassTransit;
using OrderService.Application.Events;
using OrderService.Domain.Repositories;
using OrderService.Domain;

namespace OrderService.Application.Handlers;

public class OrderCreatedEventHandler : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedEventHandler> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderCreatedEventHandler(
        ILogger<OrderCreatedEventHandler> logger,
        IOrderRepository orderRepository,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _orderRepository = orderRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var orderEvent = context.Message;
        
        _logger.LogInformation(
            "Processing OrderCreated event for Order {OrderId}, User {UserId}, Amount {TotalAmount}",
            orderEvent.OrderId,
            orderEvent.UserId,
            orderEvent.TotalAmount);

        try
        {
            await UpdateOrderStatusAsync(orderEvent);

            // Trigger payment request after order is confirmed to exist
            await RequestPaymentAsync(orderEvent);

            _logger.LogInformation("Successfully processed OrderCreated event for Order {OrderId}", orderEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCreated event for Order {OrderId}", orderEvent.OrderId);
            throw; 
        }
    }

    private async Task UpdateOrderStatusAsync(OrderCreatedEvent orderEvent)
    {
        _logger.LogInformation("Updating order status for Order {OrderId}", orderEvent.OrderId);

        var order = await _orderRepository.GetByIdAsync(orderEvent.OrderId);
        if (order != null)
        {
            order.Status = "Processing";
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order);
            _logger.LogInformation("Order {OrderId} status updated to Processing", orderEvent.OrderId);
        }
        else
        {
            _logger.LogWarning("Order {OrderId} not found for status update", orderEvent.OrderId);
        }
    }

    private async Task RequestPaymentAsync(OrderCreatedEvent orderEvent)
    {
        _logger.LogInformation("Requesting payment for Order {OrderId}", orderEvent.OrderId);

        var paymentRequestedEvent = new PaymentRequestedEvent
        {
            OrderId = orderEvent.OrderId,
            UserId = orderEvent.UserId,
            Amount = orderEvent.TotalAmount,
            Currency = "VND",
            RequestedAt = DateTime.UtcNow
        };

        await _publishEndpoint.Publish(paymentRequestedEvent);
        _logger.LogInformation("Payment requested for order {OrderId} via OrderCreated event", orderEvent.OrderId);
    }
}