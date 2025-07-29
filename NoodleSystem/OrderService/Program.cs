using Microsoft.EntityFrameworkCore;
using OrderService.Domain;
using OrderService.Domain.Repositories;
using OrderService.Application.Services;
using OrderService.Application.Handlers;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<OrderDbContext>("spicyNoodleDbOrder");

// Add RabbitMQ with Aspire
builder.AddRabbitMQClient("rabbitmq");

// Configure MassTransit
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<OrderCreatedEventHandler>();
    x.AddConsumer<PaymentRequestedEventHandler>();
    x.AddConsumer<PaymentCompletedEventHandler>();
    x.AddConsumer<PaymentFailedEventHandler>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("rabbitmq");
        cfg.Host(connectionString);
        
        cfg.ConfigureEndpoints(context);
    });
});

// Register repositories and services
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService.Application.Services.OrderService>();

// Register new application services
builder.Services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7204");
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    context.Database.EnsureCreated();
    
    // Add seed data if tables are empty
    if (!context.SpicyNoodles.Any())
    {
        context.SpicyNoodles.AddRange(
            new SpicyNoodle { Name = "Classic Spicy Ramen", BasePrice = 8.99m, Description = "Traditional spicy ramen with rich broth and tender noodles" },
            new SpicyNoodle { Name = "Korean Fire Noodles", BasePrice = 9.99m, Description = "Extremely spicy Korean-style instant noodles" },
            new SpicyNoodle { Name = "Thai Tom Yum Noodles", BasePrice = 10.49m, Description = "Aromatic Thai soup noodles with lemongrass and chili" }
        );
        context.SaveChanges();
    }
    
    if (!context.Orders.Any())
    {
        context.Orders.AddRange(
            new Order { UserId = 1, Status = "Completed", TotalAmount = 15.49m },
            new Order { UserId = 2, Status = "Completed", TotalAmount = 22.98m },
            new Order { UserId = 3, Status = "Pending", TotalAmount = 18.99m }
        );
        context.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapGrpcService<OrderGrpcService>();
app.MapDefaultEndpoints();

app.Run();
