using OrderService.Application.Dtos;
using OrderService.Application.Events;
using OrderService.Domain;
using OrderService.Domain.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly OrderDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            OrderDbContext context,
            IPublishEndpoint publishEndpoint,
            ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _context = context;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<OrderResult> CreateOrderAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                var order = new Order
                {
                    UserId = command.UserId,
                    Status = "Pending",
                    TotalAmount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdOrder = await _orderRepository.CreateAsync(order);

                decimal totalAmount = 0;
                var orderItems = new List<OrderItemResult>();

                if (command.Items != null && command.Items.Any())
                {
                    foreach (var item in command.Items)
                    {
                        var noodle = await _context.SpicyNoodles.FindAsync(item.NoodleId);
                        if (noodle == null)
                        {
                            throw new ArgumentException($"Noodle with ID {item.NoodleId} not found");
                        }

                        var orderItem = new OrderItem
                        {
                            OrderId = createdOrder.OrderId,
                            NoodleId = item.NoodleId,
                            Quantity = item.Quantity,
                            Subtotal = noodle.BasePrice * item.Quantity,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.OrderItems.Add(orderItem);
                        totalAmount += orderItem.Subtotal;

                        orderItems.Add(new OrderItemResult
                        {
                            OrderItemId = orderItem.OrderItemId,
                            NoodleId = orderItem.NoodleId,
                            Quantity = orderItem.Quantity,
                            Subtotal = orderItem.Subtotal
                        });
                    }

                    await _context.SaveChangesAsync();

                    // Update order total
                    createdOrder.TotalAmount = totalAmount;
                    createdOrder.UpdatedAt = DateTime.UtcNow;
                    await _orderRepository.UpdateAsync(createdOrder);
                }

                // Immediately request payment for the order via event
                var paymentRequestedEvent = new PaymentRequestedEvent
                {
                    OrderId = createdOrder.OrderId,
                    UserId = createdOrder.UserId,
                    Amount = createdOrder.TotalAmount,
                    Currency = "VND",
                    RequestedAt = DateTime.UtcNow
                };

                await _publishEndpoint.Publish(paymentRequestedEvent);
                _logger.LogInformation("Payment requested for order {OrderId} via event", createdOrder.OrderId);

                // Publish OrderCreated event
                var orderCreatedEvent = new OrderCreatedEvent
                {
                    OrderId = createdOrder.OrderId,
                    UserId = createdOrder.UserId,
                    TotalAmount = createdOrder.TotalAmount,
                    CreatedAt = createdOrder.CreatedAt,
                    Items = orderItems.Select(oi => new OrderItemDto
                    {
                        NoodleId = oi.NoodleId,
                        Quantity = oi.Quantity,
                        Subtotal = oi.Subtotal
                    }).ToList()
                };

                await _publishEndpoint.Publish(orderCreatedEvent);
                _logger.LogInformation("Order {OrderId} created and event published", createdOrder.OrderId);

                return new OrderResult
                {
                    OrderId = createdOrder.OrderId,
                    UserId = createdOrder.UserId,
                    Status = createdOrder.Status,
                    TotalAmount = createdOrder.TotalAmount,
                    CreatedAt = createdOrder.CreatedAt,
                    Items = orderItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for user {UserId}", command.UserId);
                throw;
            }
        }

        public async Task<OrderResult?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null) return null;

                return new OrderResult
                {
                    OrderId = order.OrderId,
                    UserId = order.UserId,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,
                    CreatedAt = order.CreatedAt,
                    Items = order.OrderItems.Select(oi => new OrderItemResult
                    {
                        OrderItemId = oi.OrderItemId,
                        NoodleId = oi.NoodleId,
                        Quantity = oi.Quantity,
                        Subtotal = oi.Subtotal
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<IEnumerable<OrderResult>> GetOrdersByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var orders = await _orderRepository.GetByUserIdAsync(userId);
                return orders.Select(order => new OrderResult
                {
                    OrderId = order.OrderId,
                    UserId = order.UserId,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,
                    CreatedAt = order.CreatedAt,
                    Items = order.OrderItems.Select(oi => new OrderItemResult
                    {
                        OrderItemId = oi.OrderItemId,
                        NoodleId = oi.NoodleId,
                        Quantity = oi.Quantity,
                        Subtotal = oi.Subtotal
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<OrderResult>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ToListAsync(cancellationToken);

                return orders.Select(order => new OrderResult
                {
                    OrderId = order.OrderId,
                    UserId = order.UserId,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,
                    CreatedAt = order.CreatedAt,
                    Items = order.OrderItems.Select(oi => new OrderItemResult
                    {
                        OrderItemId = oi.OrderItemId,
                        NoodleId = oi.NoodleId,
                        Quantity = oi.Quantity,
                        Subtotal = oi.Subtotal
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders");
                throw;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null) return false;

                var oldStatus = order.Status;
                order.Status = newStatus;
                order.UpdatedAt = DateTime.UtcNow;

                await _orderRepository.UpdateAsync(order);

                // Publish OrderStatusChanged event
                var statusChangedEvent = new OrderStatusChangedEvent
                {
                    OrderId = orderId,
                    UserId = order.UserId,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    TotalAmount = order.TotalAmount,
                    ChangedAt = order.UpdatedAt
                };

                await _publishEndpoint.Publish(statusChangedEvent);
                _logger.LogInformation("Order {OrderId} status changed from {OldStatus} to {NewStatus}", orderId, oldStatus, newStatus);

                // If order is confirmed, request payment
                if (newStatus.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
                {
                    var paymentRequestedEvent = new PaymentRequestedEvent
                    {
                        OrderId = orderId,
                        UserId = order.UserId,
                        Amount = order.TotalAmount,
                        RequestedAt = DateTime.UtcNow
                    };

                    await _publishEndpoint.Publish(paymentRequestedEvent);
                    _logger.LogInformation("Payment requested for order {OrderId}", orderId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId} status to {Status}", orderId, newStatus);
                throw;
            }
        }

        public async Task<IEnumerable<SpicyNoodle>> GetAvailableNoodlesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SpicyNoodles
                    .Where(n => n.IsActive)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available noodles");
                throw;
            }
        }

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            try
            {
                return await _orderRepository.DeleteAsync(orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", orderId);
                throw;
            }
        }
    }
}
