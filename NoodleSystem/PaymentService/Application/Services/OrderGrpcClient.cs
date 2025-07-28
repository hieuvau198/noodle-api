using Grpc.Net.Client;
using OrderService.Grpc;

namespace PaymentService.Application.Services;

public interface IOrderGrpcClient
{
    Task<bool> ValidateOrderExistsAsync(int orderId);
    Task<OrderDetails?> GetOrderDetailsAsync(int orderId);
}

public class OrderGrpcClient : IOrderGrpcClient
{
    private readonly GrpcChannel _channel;
    private readonly OrderService.Grpc.OrderService.OrderServiceClient _client;
    private readonly ILogger<OrderGrpcClient> _logger;

    public OrderGrpcClient(ILogger<OrderGrpcClient> logger)
    {
        var orderServiceUrl = Environment.GetEnvironmentVariable("ORDER_SERVICE_URL") ?? "https://localhost:7001";
        
        _channel = GrpcChannel.ForAddress(orderServiceUrl);
        _client = new OrderService.Grpc.OrderService.OrderServiceClient(_channel);
        _logger = logger;
    }

    public async Task<bool> ValidateOrderExistsAsync(int orderId)
    {
        try
        {
            _logger.LogInformation("Validating order {OrderId} via gRPC", orderId);

            var request = new ValidateOrderRequest { OrderId = orderId };
            var response = await _client.ValidateOrderAsync(request);

            _logger.LogInformation("Order validation result: {Exists} - {Message}", response.Exists, response.Message);

            return response.Exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating order {OrderId} via gRPC", orderId);
            throw new InvalidOperationException($"Failed to validate order {orderId}: {ex.Message}", ex);
        }
    }

    public async Task<OrderDetails?> GetOrderDetailsAsync(int orderId)
    {
        try
        {
            _logger.LogInformation("Getting order details for {OrderId} via gRPC", orderId);

            var request = new ValidateOrderRequest { OrderId = orderId };
            var response = await _client.ValidateOrderAsync(request);

            if (!response.Exists)
            {
                _logger.LogWarning("Order {OrderId} not found", orderId);
                return null;
            }

            _logger.LogInformation("Order details retrieved successfully for {OrderId}", orderId);
            return response.Order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order details for {OrderId} via gRPC", orderId);
            throw new InvalidOperationException($"Failed to get order details for {orderId}: {ex.Message}", ex);
        }
    }
} 