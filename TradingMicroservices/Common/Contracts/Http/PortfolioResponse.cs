using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingMicroservices.Common.Contracts.Http
{
    public class PortfolioResponse
    {
        public string UserRef { get; set; }
        public decimal TotalMarketValue { get; set; }
        public decimal UnrealizedPnl { get; set; }
        public List<PortfolioPositionModel> Positions { get; set; } = new();
        public DateTimeOffset Date { get; set; }
    }

    public class PortfolioPositionModel
    {
        public string StockSymbol { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal LastPrice { get; set; }
        public decimal MarketValue { get; set; }
        public decimal UnrealizedPnl { get; set; }
    }
}
