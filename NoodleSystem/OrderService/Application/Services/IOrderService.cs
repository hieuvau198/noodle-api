using OrderService.Application.Dtos;
using OrderService.Domain;

namespace OrderService.Application.Services;

public interface IOrderService
{
    Task<OrderResult> CreateOrderAsync(CreateOrderCommand command, CancellationToken cancellationToken = default);
    Task<OrderResult?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderResult>> GetOrdersByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderResult>> GetAllOrdersAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, CancellationToken cancellationToken = default);
    Task<IEnumerable<SpicyNoodle>> GetAvailableNoodlesAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteOrderAsync(int orderId);
}
