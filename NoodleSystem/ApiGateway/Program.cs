var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient("OrderService", client =>
{
    client.BaseAddress = new Uri("http://order-service/"); // Use the correct service name or URL
});
builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri("http://user-service/"); // Use the correct service name or URL
});

// Add controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
