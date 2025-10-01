using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Services.OrderService.Data.Entities;

namespace Services.OrderService.Data.Repositories
{
    public interface IOrderRepository
    {
        Task AddAsync(Order order, CancellationToken ct);
        Task<Order?> FindAsync(Guid id, CancellationToken ct);
    }

    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext DbContext;
        public OrderRepository(OrderDbContext db)
        {
            DbContext = db;
        }

        public Task AddAsync(Order order, CancellationToken ct)
        {
            return DbContext.Orders.AddAsync(order, ct).AsTask();
        }

        public Task<Order?> FindAsync(Guid id, CancellationToken ct)
        {
            return DbContext.Orders.Include(o => o.Execution).Include(o => o.Stock).FirstOrDefaultAsync(o => o.Id == id, ct);
        }
    }
}
