using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add SQL Server DbContext using Aspire
builder.AddSqlServerDbContext<PaymentDbContext>("spicyNoodleDbPayment");

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Add Aspire default endpoints
app.MapDefaultEndpoints();

app.Run();
