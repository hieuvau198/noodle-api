using Microsoft.EntityFrameworkCore;
using OrderService.Domain;
using OrderService.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<OrderDbContext>("spicyNoodleDbOrder");

builder.Services.AddControllers();
builder.Services.AddGrpc();
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
app.MapGrpcService<OrderGrpcService>();
app.MapDefaultEndpoints();

app.Run();
