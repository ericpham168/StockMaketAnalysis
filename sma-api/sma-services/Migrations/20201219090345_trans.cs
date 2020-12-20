using Microsoft.EntityFrameworkCore.Migrations;

namespace sma_services.Migrations
{
    public partial class trans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TickerTranSactions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ticker = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TickerTranSactions", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    TID = table.Column<int>(type: "int", nullable: false),
                    ItemSet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<double>(type: "float", nullable: false),
                    TickerID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.TID);
                    table.ForeignKey(
                        name: "FK_Transactions_TickerTranSactions_TickerID",
                        column: x => x.TickerID,
                        principalTable: "TickerTranSactions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TickerTranSactions_Ticker",
                table: "TickerTranSactions",
                column: "Ticker",
                unique: true,
                filter: "[Ticker] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TickerID",
                table: "Transactions",
                column: "TickerID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "TickerTranSactions");
        }
    }
}
