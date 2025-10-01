using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.OrderService.Data
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }

    public class OrderUnitOfWork : IUnitOfWork
    {
        private readonly OrderDbContext DbContext;
        public OrderUnitOfWork(OrderDbContext db)
        {
            DbContext = db;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return DbContext.SaveChangesAsync(ct);
        }
    }
}
