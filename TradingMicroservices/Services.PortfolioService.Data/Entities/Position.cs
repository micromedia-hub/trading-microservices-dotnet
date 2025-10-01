using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.PortfolioService.Data.Entities
{
    public class Position
    {
        public long Id { get; set; }
        public string UserRef { get; set; }
        public int StockId { get; set; }
        public int Quantity { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal RealizedPnl { get; set; }
        public DateTimeOffset UpdateDate { get; set; }
        public Stock? Stock { get; set; }
    }
}
