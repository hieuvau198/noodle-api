namespace OrderService.Domain.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<IEnumerable<Order>> GetByUserIdAsync(int userId);
    Task<Order> CreateAsync(Order order);
    Task<Order> UpdateAsync(Order order);
    Task<bool> DeleteAsync(int id);
}
