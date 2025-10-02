using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingMicroservices.Common.Enums;

namespace TradingMicroservices.Common.Contracts.Http
{
    public class PlaceOrderRequest
    {
        // символ на акцията (AAPL, TSLA…)
        public string StockSymbol { get; set; } = string.Empty;

        // брой акции
        public int Quantity { get; set; }

        // Buy / Sell
        public OrderSide Side { get; set; }
    }
}
