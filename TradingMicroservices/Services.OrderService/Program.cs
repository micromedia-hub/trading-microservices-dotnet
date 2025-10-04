using System.Security.Claims;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using TradingMicroservices.Common.Contracts.Http;
using TradingMicroservices.Common.Contracts.Messaging;
using TradingMicroservices.Services.OrderService.Data.Repositories;
using TradingMicroservices.Services.OrderService.Data;
using TradingMicroservices.Services.OrderService.Infrastructure;
using TradingMicroservices.Services.OrderService.Messaging;
using TradingMicroservices.Services.OrderService.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// EF Core (PostgreSQL)
builder.Services.AddDbContext<OrderDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("OrderDb")));

// Repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IUnitOfWork, OrderUnitOfWork>();

// App services
builder.Services.AddScoped<IOrderProcessingService, OrderProcessingService>();
builder.Services.AddScoped<IOrderEventPublisher, OrderEventPublisher>();

// In-memory price cache
builder.Services.AddSingleton<IPriceCache, PriceCache>();

// MassTransit, RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PriceUpdatedConsumer>();
    x.UsingRabbitMq((context, configure) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var vhost = builder.Configuration["RabbitMQ:VirtualHost"] ?? "/";
        configure.Host(host, vhost, h =>
        {
            h.Username(builder.Configuration["RabbitMQ:User"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Pass"] ?? "guest");
        });
        configure.Message<TradingMicroservices.Common.Contracts.Messaging.OrderExecutedEvent>(m =>
            m.SetEntityName(TradingMicroservices.Common.Constants.Messaging.Exchanges.OrderExecuted));
        configure.Message<PriceUpdatedEvent>(m =>
            m.SetEntityName(TradingMicroservices.Common.Constants.Messaging.Exchanges.PriceUpdated));
        configure.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("order", false));
    });
});

// JWT
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("ApiUser", p => p.RequireAuthenticatedUser());
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OrderService", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT}"
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var app = builder.Build();

// Apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to migrate OrderDb.");
        throw;
    }
}

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderService v1");
    });
}

app.MapPost("/api/order/add", async (
    PlaceOrderRequest request,
    IOrderProcessingService orderService,
    IOrderEventPublisher eventPublisher,
    ClaimsPrincipal user,
    HttpContext httpContext,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    var logger = loggerFactory.CreateLogger("OrderService");
    logger.LogInformation("Order requested: {StockSymbol} {Quantity} {Side}", request.StockSymbol, request.Quantity, request.Side);
    var userRef = user.FindFirst("sub")?.Value
    ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? httpContext.Request.Headers[TradingMicroservices.Common.Constants.Messaging.Headers.UserRef].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(userRef))
    {
        return Results.Unauthorized();
    }
    try
    {
        var result = await orderService.PlaceOrderAsync(request, userRef, ct);
        await eventPublisher.PublishOrderExecutedAsync(result, ct);
        logger.LogInformation("Order executed: {OrderId} {UserRef} {Symbol} qty={Qty} @ {Price}",
            result.OrderId, result.UserRef, result.StockSymbol, result.FilledQuantity, result.FillPrice);
        return Results.Created($"/api/order/{result.OrderId}", new { id = result.OrderId });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
})
.WithName("PlaceOrder")
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.RequireAuthorization("ApiUser");

app.Run();
