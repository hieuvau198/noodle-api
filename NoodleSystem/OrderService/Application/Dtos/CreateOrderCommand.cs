namespace OrderService.Application.Dtos;

public record CreateOrderCommand
{
    public int UserId { get; init; }
    public List<CreateOrderItemCommand> Items { get; init; } = new();
}

public record CreateOrderItemCommand
{
    public int NoodleId { get; init; }
    public int Quantity { get; init; }
}

public record OrderResult
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<OrderItemResult> Items { get; init; } = new();
    
    // Payment information
    public string? PaymentId { get; init; }
    public string? PaymentUrl { get; init; }
    public DateTime? PaymentExpiresAt { get; init; }
    public string? PaymentErrorMessage { get; init; }
}

public record OrderItemResult
{
    public int OrderItemId { get; init; }
    public int NoodleId { get; init; }
    public string NoodleName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Subtotal { get; init; }
}