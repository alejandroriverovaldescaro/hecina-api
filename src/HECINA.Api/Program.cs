using HECINA.Api.Authentication;
using HECINA.Api.Extensions;
using HECINA.Api.Middleware;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add authentication
var authScheme = builder.Configuration["Authentication:Scheme"] ?? "Basic";
if (authScheme.Equals("JWT", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddJwtAuthentication(builder.Configuration);
}
else
{
    builder.Services.AddAuthentication("Basic")
        .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);
}

builder.Services.AddAuthorization();

// Add application services
builder.Services.AddApplicationServices(builder.Configuration);

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "HECINA API",
        Version = "v1",
        Description = "Medical Expenses Management API"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add middleware
app.UseCorrelationId();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
