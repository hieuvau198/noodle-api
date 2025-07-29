namespace PaymentService.Application.Events;

public record PaymentCompletedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal Amount { get; init; }
    public string PaymentId { get; init; } = string.Empty;
    public string TransactionId { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; }
}