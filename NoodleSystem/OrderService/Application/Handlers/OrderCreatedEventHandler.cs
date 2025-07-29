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

            await TrackOrderAnalyticsAsync(orderEvent);

            await CreateAuditTrailAsync(orderEvent);

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

    private async Task TrackOrderAnalyticsAsync(OrderCreatedEvent orderEvent)
    {
        _logger.LogInformation("Tracking analytics for Order {OrderId}", orderEvent.OrderId);

        try
        {

            _logger.LogInformation(
                "Order Analytics - OrderId: {OrderId}, UserId: {UserId}, ItemCount: {ItemCount}, TotalAmount: {TotalAmount}, CreatedAt: {CreatedAt}",
                orderEvent.OrderId,
                orderEvent.UserId,
                orderEvent.Items.Count,
                orderEvent.TotalAmount,
                orderEvent.CreatedAt);

            foreach (var item in orderEvent.Items)
            {
                _logger.LogInformation(
                    "Item Analytics - NoodleId: {NoodleId}, Name: {Name}, Quantity: {Quantity}, Revenue: {Revenue}",
                    item.NoodleId,
                    item.NoodleName,
                    item.Quantity,
                    item.Subtotal);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track analytics for Order {OrderId}", orderEvent.OrderId);
        }    
    }

    private async Task CreateAuditTrailAsync(OrderCreatedEvent orderEvent)
    {
        _logger.LogInformation("Creating audit trail for Order {OrderId}", orderEvent.OrderId);

        try
        {
            Console.WriteLine("=== AUDIT TRAIL ===");
            Console.WriteLine($"Event: OrderCreated");
            Console.WriteLine($"Order ID: {orderEvent.OrderId}");
            Console.WriteLine($"User ID: {orderEvent.UserId}");
            Console.WriteLine($"Created At: {orderEvent.CreatedAt}");
            Console.WriteLine($"Total Amount: {orderEvent.TotalAmount:C}");
            Console.WriteLine($"Item Count: {orderEvent.Items.Count}");
            Console.WriteLine("Items:");
            foreach (var item in orderEvent.Items)
            {
                Console.WriteLine($"  - {item.NoodleName} x{item.Quantity} = {item.Subtotal:C}");
            }
            Console.WriteLine($"Audit logged at: {DateTime.UtcNow}");
            Console.WriteLine("===================");

            _logger.LogInformation(
                "AUDIT: Order {OrderId} created by User {UserId} at {CreatedAt} with {ItemCount} items totaling {TotalAmount}",
                orderEvent.OrderId,
                orderEvent.UserId,
                orderEvent.CreatedAt,
                orderEvent.Items.Count,
                orderEvent.TotalAmount);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit trail for Order {OrderId}", orderEvent.OrderId);
        }
    }
}