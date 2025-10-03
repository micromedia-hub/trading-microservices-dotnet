using MassTransit;
using Microsoft.Extensions.Options;
using TradingMicroservices.Common.Contracts.Messaging;

namespace TradingMicroservices.Services.PortfolioService.Messaging
{
    public sealed class PriceUpdatedConsumerDefinition : ConsumerDefinition<PriceUpdatedConsumer>
    {
        private readonly PriceUpdatedProcessingOptions Options;
        public PriceUpdatedConsumerDefinition(IOptions<PriceUpdatedProcessingOptions> options)
        {
            Options = options.Value;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpoint, IConsumerConfigurator<PriceUpdatedConsumer> consumer)
        {
            if (Options.RetryCount > 0)
            {
                endpoint.UseMessageRetry(configure => configure.Interval(Options.RetryCount, TimeSpan.FromMilliseconds(Options.RetryIntervalMilliseconds)));
            }
            if (string.Equals(Options.ConcurrencyMode, "Serialized", StringComparison.OrdinalIgnoreCase))
            {
                endpoint.ConcurrentMessageLimit = 1;
            }
            else if (string.Equals(Options.ConcurrencyMode, "Partitioned", StringComparison.OrdinalIgnoreCase))
            {
                var partitions = Math.Max(1, Options.Partitions);
                var partitioner = endpoint.CreatePartitioner(partitions);
                consumer.Message<PriceUpdatedEvent>(x =>
                {
                    x.UsePartitioner(partitioner, m => m.Message.StockSymbol);
                });
            }
            // else: "Parallel"
        }
    }
}
