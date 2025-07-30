using MassTransit;
using OrderService.Application.Events;
using OrderService.Domain.Repositories;
using OrderService.Domain;

namespace OrderService.Application.Handlers;

public class OrderCreatedEventHandler : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedEventHandler> _logger;
    private readonly IOrderRepository _orderRepository;

    public OrderCreatedEventHandler(
        ILogger<OrderCreatedEventHandler> logger,
        IOrderRepository orderRepository)
    {
        _logger = logger;
        _orderRepository = orderRepository;
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


}