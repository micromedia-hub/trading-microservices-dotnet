
using Microsoft.EntityFrameworkCore;
using TradingMicroservices.Services.PortfolioService.Data;
using TradingMicroservices.Services.PortfolioService.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<PortfolioDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("PortfolioDb")));

builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IUnitOfWork, PortfolioUnitOfWork>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
