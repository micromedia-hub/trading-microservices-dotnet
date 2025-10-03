using System.Security.Claims;
using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TradingMicroservices.Common.Contracts.Http;
using TradingMicroservices.Services.PortfolioService.Application;
using TradingMicroservices.Services.PortfolioService.Data;
using TradingMicroservices.Services.PortfolioService.Data.Repositories;
using TradingMicroservices.Services.PortfolioService.Messaging;

var builder = WebApplication.CreateBuilder(args);

// EF Core (PostgreSQL)
builder.Services.AddDbContext<PortfolioDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("PortfolioDb")));

// Repositories
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IUnitOfWork, PortfolioUnitOfWork>();

// App services
builder.Services.AddScoped<IPortfolioDomainService, PortfolioDomainService>();

// MassTransit, RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderExecutedConsumer>();
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
        configure.Message<TradingMicroservices.Common.Contracts.Messaging.PriceUpdatedEvent>(m =>
            m.SetEntityName(TradingMicroservices.Common.Constants.Messaging.Exchanges.PriceUpdated));
        configure.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("portfolio", false));
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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PortfolieService", Version = "v1" });
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
        var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to migrate PortfolioDb.");
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
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "PortfolioService v1");
    });
}

app.MapGet("/api/portfolio", async (
    IPortfolioRepository portfolioRepository,
    ClaimsPrincipal user,
    HttpContext httpContext,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    var logger = loggerFactory.CreateLogger("PortfolioService");
    var userRef = user.FindFirst("sub")?.Value
               ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? httpContext.Request.Headers[TradingMicroservices.Common.Constants.Messaging.Headers.UserRef].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(userRef))
    {
        return Results.Unauthorized();
    }
    var positions = await portfolioRepository.GetPositionsForUserAsync(userRef, ct);
    var lastPrices = await portfolioRepository.GetLastPricesAsync(ct);
    decimal totalMarketValue = 0m;
    decimal totalUnrealized = 0m;
    var items = new List<PortfolioPositionModel>();
    foreach (var position in positions)
    {
        var lastPrice = lastPrices.TryGetValue(position.StockId, out var lp) ? lp : 0m;
        var marketValue = Math.Round((decimal)(lastPrice * position.Quantity), 4);
        var unrealized = Math.Round((decimal)((lastPrice - position.AvgPrice) * position.Quantity), 4);
        totalMarketValue += marketValue;
        totalUnrealized += unrealized;
        items.Add(new PortfolioPositionModel
        {
            StockSymbol = position.Stock?.Symbol ?? "(unknown)",
            Quantity = position.Quantity,
            AvgPrice = position.AvgPrice,
            LastPrice = lastPrice,
            MarketValue = marketValue,
            UnrealizedPnl = unrealized
        });
    }
    var response = new PortfolioResponse
    {
        UserRef = userRef,
        TotalMarketValue = Math.Round(totalMarketValue, 4),
        UnrealizedPnl = Math.Round(totalUnrealized, 4),
        Positions = items,
        Date = DateTimeOffset.UtcNow
    };
    return Results.Ok(response);
})
.WithName("GetPortfolio")
.Produces<PortfolioResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.RequireAuthorization("ApiUser");

app.Run();
