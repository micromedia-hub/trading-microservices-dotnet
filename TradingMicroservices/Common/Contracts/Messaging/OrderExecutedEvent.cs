using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingMicroservices.Common.Contracts.Messaging
{
    public class OrderExecutedEvent
    {
        public Guid OrderId { get; set; }
        public string UserRef { get; set; }
        public string StockSymbol { get; set; } = string.Empty;
        /// <summary>
        /// BUY => +Quantity; SELL => -Quantity
        /// </summary>
        public int FilledQuantity { get; set; }
        public decimal FillPrice { get; set; }
        public DateTimeOffset Date { get; set; }
        public string TraceId { get; set; } = string.Empty;
    }
}
