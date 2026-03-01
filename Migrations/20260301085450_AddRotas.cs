using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rota2.Migrations
{
    /// <inheritdoc />
    public partial class AddRotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rotas", x => x.Id);
                });

            // Users table already exists in prior migrations/schema; do not recreate here.

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Start = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    End = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    RotaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shifts_Rotas_RotaId",
                        column: x => x.RotaId,
                        principalTable: "Rotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RotaAdmins",
                columns: table => new
                {
                    RotaId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotaAdmins", x => new { x.RotaId, x.UserId });
                    table.ForeignKey(
                        name: "FK_RotaAdmins_Rotas_RotaId",
                        column: x => x.RotaId,
                        principalTable: "Rotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RotaAdmins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RotaDoctors",
                columns: table => new
                {
                    RotaId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotaDoctors", x => new { x.RotaId, x.UserId });
                    table.ForeignKey(
                        name: "FK_RotaDoctors_Rotas_RotaId",
                        column: x => x.RotaId,
                        principalTable: "Rotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RotaDoctors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RotaAdmins_UserId",
                table: "RotaAdmins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RotaDoctors_UserId",
                table: "RotaDoctors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_RotaId",
                table: "Shifts",
                column: "RotaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RotaAdmins");

            migrationBuilder.DropTable(
                name: "RotaDoctors");

            migrationBuilder.DropTable(
                name: "Shifts");

            // Do not drop Users table here; only drop rota-related tables added in this migration.
            migrationBuilder.DropTable(
                name: "Rotas");
        }
    }
}
