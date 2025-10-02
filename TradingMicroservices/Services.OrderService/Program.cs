using System.Security.Claims;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using TradingMicroservices.Common.Contracts.Http;
using TradingMicroservices.Common.Contracts.Messaging;
using TradingMicroservices.Services.OrderService.Data.Repositories;
using TradingMicroservices.Services.OrderService.Data;
using TradingMicroservices.Services.OrderService.Infrastructure;
using TradingMicroservices.Services.OrderService.Messaging;
using TradingMicroservices.Common.Enums;
using TradingMicroservices.Services.OrderService.Data.Entities;
using TradingMicroservices.Services.OrderService.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

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
    // Consumer
    x.AddConsumer<PriceUpdatedConsumer>();
    // Publisher
    x.UsingRabbitMq((context, configure) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var vhost = builder.Configuration["RabbitMQ:VirtualHost"] ?? "/";
        configure.Host(host, vhost, h =>
        {
            h.Username(builder.Configuration["RabbitMQ:User"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Pass"] ?? "guest");
        });
        configure.Message<PriceUpdatedEvent>(x =>
        {
            x.SetEntityName(TradingMicroservices.Common.Constants.Messaging.Exchanges.PriceUpdated);
        });
        configure.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("order", false));
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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
    var logger = loggerFactory.CreateLogger("OrderPlace");
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
.Produces(StatusCodes.Status401Unauthorized);

app.Run();
