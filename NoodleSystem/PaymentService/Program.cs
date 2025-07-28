using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;
using PaymentService.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<PaymentDbContext>("spicyNoodleDbPayment");

builder.Services.AddControllers();
builder.Services.AddSingleton<IOrderGrpcClient, OrderGrpcClient>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
