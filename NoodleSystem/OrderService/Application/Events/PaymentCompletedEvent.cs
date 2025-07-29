namespace OrderService.Application.Events;

public record PaymentCompletedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal AmountPaid { get; init; }
    public string Currency { get; init; } = "VND";
    public string PaymentMethod { get; init; } = string.Empty;
    public string TransactionId { get; init; } = string.Empty;
    public DateTime PaidAt { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}