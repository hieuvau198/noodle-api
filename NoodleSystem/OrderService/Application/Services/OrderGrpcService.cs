using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain;
using OrderService.Grpc;

namespace OrderService.Application.Services;

public class OrderGrpcService : OrderService.Grpc.OrderService.OrderServiceBase
{
    private readonly OrderDbContext _context;
    private readonly ILogger<OrderGrpcService> _logger;

    public OrderGrpcService(OrderDbContext context, ILogger<OrderGrpcService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task<ValidateOrderResponse> ValidateOrder(ValidateOrderRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Validating order {OrderId} via gRPC", request.OrderId);

            var order = await _context.Orders
                .Where(o => o.OrderId == request.OrderId)
                .Select(o => new
                {
                    o.OrderId,
                    o.UserId,
                    o.Status,
                    o.TotalAmount,
                    o.CreatedAt,
                    o.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return new ValidateOrderResponse
                {
                    Exists = false,
                    Message = $"Order with ID {request.OrderId} not found"
                };
            }

            return new ValidateOrderResponse
            {
                Exists = true,
                Message = "Order found successfully",
                Order = new OrderDetails
                {
                    OrderId = order.OrderId,
                    UserId = order.UserId,
                    Status = order.Status,
                    TotalAmount = (double)order.TotalAmount,
                    CreatedAt = order.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    UpdatedAt = order.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating order {OrderId} via gRPC", request.OrderId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
} 