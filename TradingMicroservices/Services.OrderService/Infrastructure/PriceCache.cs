using System.Collections.Concurrent;

namespace TradingMicroservices.Services.OrderService.Infrastructure
{
    public interface IPriceCache
    {
        bool TryGet(string symbol, out decimal price);
        void Set(string symbol, decimal price);
    }

    /// <summary>
    /// Thread-safe in-memory last price cache keyed by stock symbol.
    /// </summary>
    public class PriceCache : IPriceCache
    {
        private readonly ConcurrentDictionary<string, decimal> Cache = new(StringComparer.OrdinalIgnoreCase);

        public bool TryGet(string symbol, out decimal price)
        {
            return Cache.TryGetValue(symbol, out price);
        }

        public void Set(string symbol, decimal price)
        {
            Cache[symbol] = price;
        }
    }
}
