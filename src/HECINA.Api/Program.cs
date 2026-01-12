using HECINA.Api.Authentication;
using HECINA.Api.Extensions;
using HECINA.Api.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add authentication: register BOTH Basic and Bearer schemes
var authScheme = builder.Configuration["Authentication:Scheme"] ?? "Basic";
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = authScheme.Equals("JWT", StringComparison.OrdinalIgnoreCase) ? "Bearer" : "Basic";
})
.AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);

builder.Services.AddJwtAuthentication(builder.Configuration); // Extension method must call AddJwtBearer("Bearer", ...)

builder.Services.AddAuthorization();

// Add application services
builder.Services.AddApplicationServices(builder.Configuration);

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HECINA API",
        Version = "v1",
        Description = "Medical Expenses Management API"
    });

    // Add security definition for Basic Auth
    options.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        In = ParameterLocation.Header,
        Description = "Basic Authentication header"
    });

    // Add security definition for JWT Bearer
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme"
    });

    // Add security requirement for both schemes
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Basic"
                }
            },
            new string[] {}
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCorrelationId();

app.UseAuthentication();
app.UseAuthorization(); 

app.MapControllers();

app.MapGet("/", () => Results.Ok("HECINA API is running"));

await app.RunAsync();