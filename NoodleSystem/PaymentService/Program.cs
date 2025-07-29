using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;
using PaymentService.Application.Services;
using PaymentService.Application.Handlers;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<PaymentDbContext>("spicyNoodleDbPayment");

// Add RabbitMQ with Aspire
builder.AddRabbitMQClient("rabbitmq");

// Configure MassTransit
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<PaymentRequestedEventHandler>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("rabbitmq");
        cfg.Host(connectionString);
        
        cfg.ConfigureEndpoints(context);
    });
});

// Add HttpClient for OrderService
builder.Services.AddHttpClient<IOrderGrpcClient, OrderGrpcClient>(client =>
{
    client.BaseAddress = new Uri("https://order-service");
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    context.Database.EnsureCreated();
    
    // Add seed data if table is empty
    if (!context.Payments.Any())
    {
        context.Payments.AddRange(
            new Payment { OrderId = 1, Amount = 15.49m, Status = "Completed", PaymentMethod = "Credit Card", TransactionId = "txn_1234567890" },
            new Payment { OrderId = 2, Amount = 22.98m, Status = "Completed", PaymentMethod = "PayPal", TransactionId = "pp_0987654321" },
            new Payment { OrderId = 3, Amount = 18.99m, Status = "Pending", PaymentMethod = "Credit Card" }
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
app.MapDefaultEndpoints();

app.Run();
