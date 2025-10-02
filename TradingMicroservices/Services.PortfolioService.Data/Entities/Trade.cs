using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingMicroservices.Services.PortfolioService.Data.Entities
{
    public class Trade
    {
        public Guid Id { get; set; }
        public Guid OrderRefId { get; set; }
        public string UserRef { get; set; }
        public int StockId { get; set; }
        /// BUY => +Quantity; SELL => -qty
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTimeOffset Date { get; set; }
        public Stock? Stock { get; set; }
    }
}
