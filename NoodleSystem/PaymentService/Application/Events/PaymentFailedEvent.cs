namespace PaymentService.Application.Events;

public record PaymentFailedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal Amount { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string ErrorCode { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; }
}