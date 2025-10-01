using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Enums;

namespace Services.OrderService.Data.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public string UserRef { get; set; }
        public int StockId { get; set; }
        public int Quantity { get; set; }
        public OrderSide Side { get; set; }
        public DateTimeOffset Date { get; set; }
        public Stock? Stock { get; set; }
        public OrderExecution? Execution { get; set; }
    }
}
