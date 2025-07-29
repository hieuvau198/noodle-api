namespace OrderService.Application.Events;

public record OrderStatusChangedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public string OldStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime ChangedAt { get; init; }
}