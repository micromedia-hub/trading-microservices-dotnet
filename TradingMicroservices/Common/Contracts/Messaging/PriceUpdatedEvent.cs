using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingMicroservices.Common.Contracts.Messaging
{
    public class PriceUpdatedEvent
    {
        public string StockSymbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string TraceId { get; set; } = string.Empty;
    }
}
