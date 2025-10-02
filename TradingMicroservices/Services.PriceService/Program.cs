using TradingMicroservices.Common.Contracts.Messaging;
using MassTransit;
using TradingMicroservices.Services.PriceService.Application;

var builder = WebApplication.CreateBuilder(args);

// MassTransit, RabbitMQ
builder.Services.AddMassTransit(x =>
{
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
    });
});

builder.Services.AddHostedService<PriceGenerator>();

var app = builder.Build();

app.Run();
