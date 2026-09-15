using System.Diagnostics.CodeAnalysis;
using CarRepairShop.API.Authentication;
using CarRepairShop.API.Middleware;
using CarRepairShop.API.Services;
using CarRepairShop.Application;
using CarRepairShop.Domain.Interfaces.Services;
using CarRepairShop.Repository;
using CarRepairShop.Repository.Context;
using CarRepairShop.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

// Add services to the container.
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Car Repair Shop API",
        Version = "v1",
        Description = "REST API for Car Repair Shop management with DDD, CQRS and JWT Authentication"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token.\r\n\r\nExample: 'Bearer eyJhbGci...'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// DDD layers
builder.Services.AddRepository(builder.Configuration);
builder.Services.AddServices(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

// Current user service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Exception response mappers (ordered: most-specific first)
builder.Services.AddSingleton<CarRepairShop.API.Middleware.IExceptionResponseMapper, CarRepairShop.API.Middleware.ExceptionMappers.ValidationExceptionMapper>();
builder.Services.AddSingleton<CarRepairShop.API.Middleware.IExceptionResponseMapper, CarRepairShop.API.Middleware.ExceptionMappers.NotFoundExceptionMapper>();
builder.Services.AddSingleton<CarRepairShop.API.Middleware.IExceptionResponseMapper, CarRepairShop.API.Middleware.ExceptionMappers.BusinessExceptionMapper>();
builder.Services.AddSingleton<CarRepairShop.API.Middleware.IExceptionResponseMapper, CarRepairShop.API.Middleware.ExceptionMappers.InvalidOperationExceptionMapper>();

builder.Services.AddSingleton<IJwtSigningKeyProvider, ConfigurationJwtSigningKeyProvider>();
builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer();

builder.Services.AddAuthorization();

var app = builder.Build();

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<CarRepairShopDbContext>();

    logger.LogInformation("Applying pending EF Core migrations.");
    db.Database.Migrate();
    logger.LogInformation("EF Core migrations finished.");

    return;
}

if (builder.Configuration.GetValue("Database:RunMigrationsOnStartup", false))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<CarRepairShopDbContext>();

    logger.LogWarning("Database:RunMigrationsOnStartup is enabled. This should be used only for local development.");
    db.Database.Migrate();
}

if (args.Contains("--validate-config", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    _ = scope.ServiceProvider.GetRequiredService<CarRepairShopDbContext>();
    var signingKeyProvider = scope.ServiceProvider.GetRequiredService<IJwtSigningKeyProvider>();
    await signingKeyProvider.GetSigningKeyAsync();
    return;
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

namespace CarRepairShop.API
{
    [ExcludeFromCodeCoverage]
    public partial class Program { }
}
