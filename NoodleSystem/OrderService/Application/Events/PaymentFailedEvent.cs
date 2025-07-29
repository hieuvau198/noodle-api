namespace OrderService.Application.Events;

public record PaymentFailedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal AttemptedAmount { get; init; }
    public string Currency { get; init; } = "VND";
    public string PaymentMethod { get; init; } = string.Empty;
    public string FailureReason { get; init; } = string.Empty;
    public string ErrorCode { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; }
    public bool IsRetryable { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}