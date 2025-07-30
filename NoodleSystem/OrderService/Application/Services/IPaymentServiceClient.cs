using OrderService.Application.Events;
using Grpc.Net.Client;

namespace OrderService.Application.Services;

public interface IPaymentServiceClient
{
    Task<PaymentRequestResult> RequestPaymentAsync(PaymentRequestDto request);
    Task<PaymentStatus> GetPaymentStatusAsync(int orderId);
    Task<bool> CancelPaymentAsync(int orderId, string reason);
    Task<bool> RefundPaymentAsync(int orderId, decimal amount, string reason);
}

public record PaymentRequestDto
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "VND";
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
}

public record PaymentRequestResult
{
    public bool Success { get; init; }
    public string? PaymentId { get; init; }
    public string? PaymentUrl { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? ErrorMessage { get; init; }
}

public record PaymentStatus
{
    public string Status { get; init; } = "";
    public string? TransactionId { get; init; }
    public decimal? AmountPaid { get; init; }
    public DateTime? PaidAt { get; init; }
    public string? FailureReason { get; init; }
}

public class PaymentServiceClient : IPaymentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly PaymentService.Grpc.PaymentService.PaymentServiceClient _grpcClient;
    private readonly ILogger<PaymentServiceClient> _logger;

    public PaymentServiceClient(HttpClient httpClient, ILogger<PaymentServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        var channel = GrpcChannel.ForAddress(_httpClient.BaseAddress!, new GrpcChannelOptions
        {
            HttpClient = _httpClient
        });
        
        _grpcClient = new PaymentService.Grpc.PaymentService.PaymentServiceClient(channel);
        
        _logger.LogInformation("PaymentServiceClient initialized with base address: {BaseAddress}", _httpClient.BaseAddress);
    }

    public async Task<PaymentRequestResult> RequestPaymentAsync(PaymentRequestDto request)
{
    _logger.LogInformation("Requesting payment for order {OrderId}, amount {Amount} via gRPC", 
        request.OrderId, request.Amount);

    try
    {
        var grpcRequest = new PaymentService.Grpc.PaymentRequestDto
        {
            OrderId = request.OrderId,
            UserId = request.UserId,
            Amount = (double)request.Amount, // Convert decimal to double for gRPC
            Currency = request.Currency,
            RequestedAt = request.RequestedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") // ISO 8601 format with milliseconds
        };

        var grpcResponse = await _grpcClient.RequestPaymentAsync(grpcRequest);

        return new PaymentRequestResult
        {
            Success = grpcResponse.Success,
            PaymentId = grpcResponse.PaymentId,
            PaymentUrl = grpcResponse.PaymentUrl,
            ExpiresAt = string.IsNullOrEmpty(grpcResponse.ExpiresAt) ? null : 
                DateTime.TryParse(grpcResponse.ExpiresAt, out var expiresAt) ? expiresAt : null,
            ErrorMessage = grpcResponse.ErrorMessage
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error requesting payment for order {OrderId} via gRPC", request.OrderId);
        return new PaymentRequestResult
        {
            Success = false,
            ErrorMessage = ex.Message
        };
    }
}

    public async Task<PaymentStatus> GetPaymentStatusAsync(int orderId)
{
    _logger.LogInformation("Getting payment status for order {OrderId} via gRPC", orderId);

    try
    {
        var grpcRequest = new PaymentService.Grpc.GetPaymentStatusRequest
        {
            OrderId = orderId
        };

        var grpcResponse = await _grpcClient.GetPaymentStatusAsync(grpcRequest);

        var result = new PaymentStatus
        {
            Status = grpcResponse.Status
        };

        if (!string.IsNullOrEmpty(grpcResponse.TransactionId))
        {
            result = result with 
            { 
                TransactionId = grpcResponse.TransactionId,
                AmountPaid = (decimal)grpcResponse.AmountPaid, // Convert double to decimal
                PaidAt = string.IsNullOrEmpty(grpcResponse.PaidAt) ? null : 
                    DateTime.TryParse(grpcResponse.PaidAt, out var paidAt) ? paidAt : null
            };
        }

        if (!string.IsNullOrEmpty(grpcResponse.FailureReason))
        {
            result = result with { FailureReason = grpcResponse.FailureReason };
        }

        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting payment status for order {OrderId} via gRPC", orderId);
        return new PaymentStatus
        {
            Status = "Error",
            FailureReason = ex.Message
        };
    }
}

    public async Task<bool> CancelPaymentAsync(int orderId, string reason)
    {
        _logger.LogInformation("Cancelling payment for order {OrderId}, reason: {Reason} via gRPC", orderId, reason);

        try
        {
            var grpcRequest = new PaymentService.Grpc.CancelPaymentRequest
            {
                OrderId = orderId,
                Reason = reason
            };

            var grpcResponse = await _grpcClient.CancelPaymentAsync(grpcRequest);
            
            if (!grpcResponse.Success)
            {
                _logger.LogWarning("Failed to cancel payment for order {OrderId}: {Message}", orderId, grpcResponse.Message);
            }

            return grpcResponse.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment for order {OrderId} via gRPC", orderId);
            return false;
        }
    }

    public async Task<bool> RefundPaymentAsync(int orderId, decimal amount, string reason)
    {
        _logger.LogInformation("Requesting refund for order {OrderId}, amount {Amount}, reason: {Reason} via gRPC", 
            orderId, amount, reason);

        try
        {
            var grpcRequest = new PaymentService.Grpc.RefundPaymentRequest
            {
                OrderId = orderId,
                Amount = (double)amount,
                Reason = reason
            };

            var grpcResponse = await _grpcClient.RefundPaymentAsync(grpcRequest);
            
            if (!grpcResponse.Success)
            {
                _logger.LogWarning("Failed to refund payment for order {OrderId}: {Message}", orderId, grpcResponse.Message);
            }
            
            if (!string.IsNullOrEmpty(grpcResponse.RefundId))
            {
                _logger.LogInformation("Refund processed successfully for order {OrderId}, refund ID: {RefundId}", 
                    orderId, grpcResponse.RefundId);
            }

            return grpcResponse.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for order {OrderId} via gRPC", orderId);
            return false;
        }
    }
}