using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UserService2.Application.Services;
using UserService2.Domain.Context;
using UserService2.Domain.Repositories;

var builder = WebApplication.CreateBuilder(args);


builder.AddSqlServerDbContext<SpicyNoodleDbContext>("spicyNoodleDbUser");

builder.Services.AddControllers();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUsersService, UsersService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]))
    };
})
.AddGoogle(options =>
{
    var clientIdBase64 = builder.Configuration["Authentication:Google:ClientId"];
    var clientSecretBase64 = builder.Configuration["Authentication:Google:ClientSecret"];
    
    options.ClientId = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(clientIdBase64));
    options.ClientSecret = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(clientSecretBase64));
    options.CallbackPath = "/auth/google-callback";
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Serve static files from wwwroot
app.UseCors("AllowAll"); // Enable CORS
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Set default page to index.html
app.MapFallbackToFile("index.html");

app.Run();
