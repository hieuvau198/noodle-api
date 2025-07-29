namespace PaymentService.Application.Events;

public record PaymentRequestedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "VND";
    public Dictionary<string, object> Metadata { get; init; } = new();
    public DateTime RequestedAt { get; init; }
}