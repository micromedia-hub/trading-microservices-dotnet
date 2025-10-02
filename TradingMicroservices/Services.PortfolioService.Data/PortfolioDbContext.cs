using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TradingMicroservices.Services.PortfolioService.Data.Entities;

namespace TradingMicroservices.Services.PortfolioService.Data
{
    public class PortfolioDbContext : DbContext
    {
        public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options) { }

        public DbSet<Stock> Stocks => Set<Stock>();
        public DbSet<Position> Positions => Set<Position>();
        public DbSet<LastPrice> LastPrices => Set<LastPrice>();
        public DbSet<Trade> Trades => Set<Trade>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.HasDefaultSchema("portfolio");

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

            b.Entity<Position>(e =>
            {
                e.ToTable("positions");
                e.HasKey(x => x.Id);
                e.Property(x => x.UserRef).IsRequired().HasMaxLength(256);
                e.Property(x => x.AvgPrice).HasColumnType("numeric(18,4)");
                e.Property(x => x.RealizedPnl).HasColumnType("numeric(18,4)");
                e.Property(x => x.UpdateDate).IsRequired();

                e.HasOne(x => x.Stock).WithMany().HasForeignKey(x => x.StockId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => new { x.UserRef, x.StockId }).IsUnique();
            });

            b.Entity<LastPrice>(e =>
            {
                e.ToTable("last_prices");
                e.HasKey(x => x.StockId);
                e.Property(x => x.Price).HasColumnType("numeric(18,4)");
                e.Property(x => x.UpdateDate).IsRequired();

                e.HasOne(x => x.Stock).WithMany().HasForeignKey(x => x.StockId).OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<Trade>(e =>
            {
                e.ToTable("trades");
                e.HasKey(x => x.Id);
                e.Property(x => x.Price).HasColumnType("numeric(18,4)");
                e.Property(x => x.UserRef).IsRequired().HasMaxLength(256);
                e.HasIndex(x => x.OrderRefId).IsUnique();
                e.HasIndex(x => new { x.UserRef, x.StockId, x.Date });

                e.HasOne(x => x.Stock).WithMany().HasForeignKey(x => x.StockId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
