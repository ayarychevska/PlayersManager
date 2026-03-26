using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PlayersManager.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Batches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nickname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BatchRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    Nickname = table.Column<string>(type: "text", nullable: false),
                    Power = table.Column<string>(type: "text", nullable: false),
                    TownHallLevel = table.Column<string>(type: "text", nullable: false),
                    MatchStatus = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchRecords_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoricalPlayerRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    Nickname = table.Column<string>(type: "text", nullable: false),
                    Power = table.Column<string>(type: "text", nullable: false),
                    TownHallLevel = table.Column<string>(type: "text", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalPlayerRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricalPlayerRecords_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistoricalPlayerRecords_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BatchRecords_BatchId",
                table: "BatchRecords",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalPlayerRecords_BatchId",
                table: "HistoricalPlayerRecords",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalPlayerRecords_PlayerId",
                table: "HistoricalPlayerRecords",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Nickname",
                table: "Players",
                column: "Nickname",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatchRecords");

            migrationBuilder.DropTable(
                name: "HistoricalPlayerRecords");

            migrationBuilder.DropTable(
                name: "Batches");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
