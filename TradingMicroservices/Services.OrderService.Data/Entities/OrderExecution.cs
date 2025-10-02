using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingMicroservices.Services.OrderService.Data.Entities
{
    public class OrderExecution
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public decimal FillPrice { get; set; }
        public int FilledQuantity { get; set; }
        public DateTimeOffset Date { get; set; }
        public Order? Order { get; set; }
    }
}
