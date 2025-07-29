namespace OrderService.Application.Events;

public record OrderCreatedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal TotalAmount { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

public record OrderItemDto
{
    public int NoodleId { get; init; }
    public string NoodleName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Subtotal { get; init; }
}