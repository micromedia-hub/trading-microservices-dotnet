using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingMicroservices.Services.PortfolioService.Data.Entities
{
    public class LastPrice
    {
        public int StockId { get; set; }
        public decimal Price { get; set; }
        public DateTimeOffset UpdateDate { get; set; }
        public Stock? Stock { get; set; }
    }
}
