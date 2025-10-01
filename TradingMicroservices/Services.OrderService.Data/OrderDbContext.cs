using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Services.OrderService.Data.Entities;

namespace Services.OrderService.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

        public DbSet<Stock> Stocks => Set<Stock>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderExecution> Executions => Set<OrderExecution>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.HasDefaultSchema("order");

            b.Entity<Stock>(e =>
            {
                e.ToTable("stocks");
                e.HasKey(x => x.Id);
                e.Property(x => x.Symbol).IsRequired().HasMaxLength(16);
                e.Property(x => x.Name).IsRequired().HasMaxLength(256);
                e.HasIndex(x => x.Symbol).IsUnique();

                e.HasData(
                    new Stock { Id = 1, Symbol = "AAPL", Name = "Apple Inc." },
                    new Stock { Id = 2, Symbol = "TSLA", Name = "Tesla, Inc." },
                    new Stock { Id = 3, Symbol = "NVDA", Name = "NVIDIA Corporation" },
                    new Stock { Id = 4, Symbol = "MSFT", Name = "Microsoft Corporation" },
                    new Stock { Id = 5, Symbol = "AMZN", Name = "Amazon.com, Inc." }
                );
            });

            b.Entity<Order>(e =>
            {
                e.ToTable("orders");
                e.HasKey(x => x.Id);
                e.Property(x => x.UserRef).IsRequired().HasMaxLength(256);
                e.Property(x => x.Quantity).IsRequired();
                e.Property(x => x.Side).IsRequired();
                e.Property(x => x.Date).IsRequired();

                e.HasIndex(x => x.UserRef);
                e.HasOne(x => x.Stock).WithMany().HasForeignKey(x => x.StockId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Execution)
                 .WithOne(x => x.Order!)
                 .HasForeignKey<OrderExecution>(x => x.OrderId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<OrderExecution>(e =>
            {
                e.ToTable("executions");
                e.HasKey(x => x.Id);
                e.Property(x => x.FillPrice).HasColumnType("numeric(18,4)");
                e.Property(x => x.Date).IsRequired();
                e.HasIndex(x => x.OrderId).IsUnique();
            });
        }
    }
}
