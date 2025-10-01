using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Services.PortfolioService.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "portfolio");

            migrationBuilder.CreateTable(
                name: "stocks",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "last_prices",
                schema: "portfolio",
                columns: table => new
                {
                    StockId = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UpdateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_last_prices", x => x.StockId);
                    table.ForeignKey(
                        name: "FK_last_prices_stocks_StockId",
                        column: x => x.StockId,
                        principalSchema: "portfolio",
                        principalTable: "stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StockId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    AvgPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RealizedPnl = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UpdateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_positions_stocks_StockId",
                        column: x => x.StockId,
                        principalSchema: "portfolio",
                        principalTable: "stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trades",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderRefId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StockId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_trades_stocks_StockId",
                        column: x => x.StockId,
                        principalSchema: "portfolio",
                        principalTable: "stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "portfolio",
                table: "stocks",
                columns: new[] { "Id", "Name", "Symbol" },
                values: new object[,]
                {
                    { 1, "Apple Inc.", "AAPL" },
                    { 2, "Tesla, Inc.", "TSLA" },
                    { 3, "NVIDIA Corporation", "NVDA" },
                    { 4, "Microsoft Corporation", "MSFT" },
                    { 5, "Amazon.com, Inc.", "AMZN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_positions_StockId",
                schema: "portfolio",
                table: "positions",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_positions_UserRef_StockId",
                schema: "portfolio",
                table: "positions",
                columns: new[] { "UserRef", "StockId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stocks_Symbol",
                schema: "portfolio",
                table: "stocks",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trades_OrderRefId",
                schema: "portfolio",
                table: "trades",
                column: "OrderRefId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trades_StockId",
                schema: "portfolio",
                table: "trades",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_trades_UserRef_StockId_Date",
                schema: "portfolio",
                table: "trades",
                columns: new[] { "UserRef", "StockId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "last_prices",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "positions",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "trades",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "stocks",
                schema: "portfolio");
        }
    }
}
